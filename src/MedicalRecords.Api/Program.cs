using System.Security.Claims;
using System.Text;
using MedicalRecords.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var signingKey = builder.Configuration["Auth:SigningKey"];
if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
    throw new InvalidOperationException("Set Auth__SigningKey to an unpredictable value of at least 32 bytes.");
var connection = builder.Configuration.GetConnectionString("MedicalDb");
if (string.IsNullOrWhiteSpace(connection))
    throw new InvalidOperationException("Set ConnectionStrings__MedicalDb before starting the API.");

builder.Services.AddDbContext<MedicalDb>(options => options.UseNpgsql(connection));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = "medical-records-local",
        ValidateAudience = true,
        ValidAudience = "medical-records-api",
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.NameIdentifier
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();
// EnsureCreated is intentionally DEVELOPMENT-only for the coursework prototype.
// Production deployments must use approved, reviewed EF Core migrations.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<MedicalDb>().Database.EnsureCreatedAsync();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

var api = app.MapGroup("/api/v1").RequireAuthorization();
api.MapGet("/patients", async (MedicalDb db, ClaimsPrincipal user, CancellationToken ct) =>
{
    var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var isAdmin = user.IsInRole("Admin");
    if (id is null) return Results.Unauthorized();
    var query = db.Patients.AsNoTracking().AsQueryable();
    if (!isAdmin) query = query.Where(p => p.AssignedClinicianId == id);
    var patients = await query.OrderBy(p => p.FamilyName)
        .Select(p => new PatientView(p.Id, p.FamilyName, p.GivenName, p.BirthDate))
        .ToListAsync(ct);
    return Results.Ok(patients);
}).RequireAuthorization(policy => policy.RequireRole("Clinician", "Admin"));

api.MapPost("/patients", async (CreatePatient request, MedicalDb db, ClaimsPrincipal user, CancellationToken ct) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null) return Results.Unauthorized();
    if (!MedicalRules.IsValidName(request.FamilyName) ||
        !MedicalRules.IsValidName(request.GivenName) ||
        !MedicalRules.IsValidBirthDate(request.BirthDate))
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["patient"] = ["Invalid patient name or birth date."]
        });
    var patient = new Patient
    {
        FamilyName = request.FamilyName.Trim(),
        GivenName = request.GivenName.Trim(),
        BirthDate = request.BirthDate,
        AssignedClinicianId = userId
    };
    db.Patients.Add(patient);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/api/v1/patients/{patient.Id}",
        new PatientView(patient.Id, patient.FamilyName, patient.GivenName, patient.BirthDate));
}).RequireAuthorization(policy => policy.RequireRole("Clinician", "Admin"));

api.MapGet("/patients/{id:guid}", async (Guid id, MedicalDb db, ClaimsPrincipal user, CancellationToken ct) =>
{
    var patient = await db.Patients.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, ct);
    if (patient is null) return Results.NotFound();
    if (!MedicalRules.MayAccess(user.FindFirstValue(ClaimTypes.NameIdentifier),
            user.IsInRole("Admin"), patient.AssignedClinicianId)) return Results.Forbid();
    return Results.Ok(new PatientView(patient.Id, patient.FamilyName,
        patient.GivenName, patient.BirthDate));
}).RequireAuthorization(policy => policy.RequireRole("Clinician", "Admin"));

api.MapGet("/patients/{id:guid}/encounters", async (Guid id, MedicalDb db, ClaimsPrincipal user, CancellationToken ct) =>
{
    var patient = await db.Patients.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, ct);
    if (patient is null) return Results.NotFound();
    if (!MedicalRules.MayAccess(user.FindFirstValue(ClaimTypes.NameIdentifier),
            user.IsInRole("Admin"), patient.AssignedClinicianId)) return Results.Forbid();
    var encounters = await db.Encounters.AsNoTracking()
        .Where(e => e.PatientId == id).OrderByDescending(e => e.RecordedAtUtc)
        .Select(e => new EncounterView(e.Id, e.RecordedAtUtc, e.ClinicalNote))
        .ToListAsync(ct);
    return Results.Ok(encounters);
}).RequireAuthorization(policy => policy.RequireRole("Clinician", "Admin"));

api.MapPost("/patients/{id:guid}/encounters", async (Guid id, CreateEncounter request,
    MedicalDb db, ClaimsPrincipal user, CancellationToken ct) =>
{
    var patient = await db.Patients.SingleOrDefaultAsync(p => p.Id == id, ct);
    if (patient is null) return Results.NotFound();
    var actor = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!MedicalRules.MayAccess(actor, user.IsInRole("Admin"), patient.AssignedClinicianId))
        return Results.Forbid();
    if (!MedicalRules.IsValidClinicalNote(request.ClinicalNote))
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["clinicalNote"] = ["Clinical note must have 2 to 4000 characters."]
        });
    var encounter = new Encounter
    {
        PatientId = patient.Id,
        ClinicalNote = request.ClinicalNote.Trim(),
        AuthorId = actor!,
        RecordedAtUtc = DateTime.UtcNow
    };
    db.Encounters.Add(encounter);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/api/v1/patients/{id}/encounters/{encounter.Id}",
        new EncounterView(encounter.Id, encounter.RecordedAtUtc, encounter.ClinicalNote));
}).RequireAuthorization(policy => policy.RequireRole("Clinician", "Admin"));

app.Run();

public record CreatePatient(string FamilyName, string GivenName, DateOnly BirthDate);
public record PatientView(Guid Id, string FamilyName, string GivenName, DateOnly BirthDate);
public record CreateEncounter(string ClinicalNote);
public record EncounterView(Guid Id, DateTime RecordedAtUtc, string ClinicalNote);
public partial class Program { }
