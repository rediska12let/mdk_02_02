using Microsoft.EntityFrameworkCore;

namespace MedicalRecords.Api;

public sealed class Patient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FamilyName { get; set; } = "";
    public string GivenName { get; set; } = "";
    public DateOnly BirthDate { get; set; }
    public string AssignedClinicianId { get; set; } = "";
    public List<Encounter> Encounters { get; set; } = new();
}

public sealed class Encounter
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public string ClinicalNote { get; set; } = "";
    public string AuthorId { get; set; } = "";
}

public sealed class MedicalDb(DbContextOptions<MedicalDb> options) : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Encounter> Encounters => Set<Encounter>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Patient>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FamilyName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.GivenName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.AssignedClinicianId).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.AssignedClinicianId);
        });
        model.Entity<Encounter>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AuthorId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ClinicalNote).HasMaxLength(4000).IsRequired();
            entity.HasOne(x => x.Patient).WithMany(x => x.Encounters)
                .HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
