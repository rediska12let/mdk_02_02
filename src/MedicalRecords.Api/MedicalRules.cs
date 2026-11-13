namespace MedicalRecords.Api;

public static class MedicalRules
{
    public static bool IsValidName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && name.Trim().Length is >= 2 and <= 100;

    public static bool IsValidBirthDate(DateOnly date) =>
        date >= new DateOnly(1900, 1, 1) && date <= DateOnly.FromDateTime(DateTime.UtcNow);

    public static bool MayAccess(string? actorId, bool isAdmin, string assignedClinicianId) =>
        isAdmin || (!string.IsNullOrWhiteSpace(actorId) &&
                    string.Equals(actorId, assignedClinicianId, StringComparison.Ordinal));

    public static bool IsValidClinicalNote(string? text) =>
        !string.IsNullOrWhiteSpace(text) && text.Trim().Length is >= 2 and <= 4000;
}
