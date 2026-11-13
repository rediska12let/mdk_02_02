using MedicalRecords.Api;
using Xunit;

namespace MedicalRecords.Tests;

public class MedicalRulesTests
{
    [Theory]
    [InlineData("Тестов", true)]
    [InlineData("A", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void NameValidation(string name, bool valid) =>
        Assert.Equal(valid, MedicalRules.IsValidName(name));

    [Fact]
    public void AccessDeniedToAnotherClinician() =>
        Assert.False(MedicalRules.MayAccess("doctor-b", false, "doctor-a"));

    [Fact]
    public void AssignedClinicianCanAccess() =>
        Assert.True(MedicalRules.MayAccess("doctor-a", false, "doctor-a"));

    [Fact]
    public void AdminCanAccess() =>
        Assert.True(MedicalRules.MayAccess("admin", true, "doctor-a"));

    [Theory]
    [InlineData("", false)]
    [InlineData("ok", true)]
    public void ClinicalNoteValidation(string note, bool valid) =>
        Assert.Equal(valid, MedicalRules.IsValidClinicalNote(note));

    [Fact]
    public void FutureBirthDateRejected() =>
        Assert.False(MedicalRules.IsValidBirthDate(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))));
}
