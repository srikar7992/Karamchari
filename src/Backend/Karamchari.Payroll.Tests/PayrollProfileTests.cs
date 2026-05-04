namespace Karamchari.Payroll.Tests;

using Karamchari.Payroll.Domain;
using Karamchari.Payroll.Domain.Statutory;

/// <summary>
/// Unit tests for <see cref="PayrollProfile"/> domain model.
/// </summary>
public class PayrollProfileTests
{
    [Fact]
    public void CreateDraft_WithLegalName_ShouldCacheEmployeeName()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        const string legalName = "Priya Sharma";

        // Act
        var profile = PayrollProfile.CreateDraft(employeeId, legalName);

        // Assert
        Assert.Equal(employeeId, profile.EmployeeId);
        Assert.Equal(legalName, profile.EmployeeName);
        Assert.Equal(PayType.Salaried, profile.PayType);
        Assert.False(profile.IsActive);
    }

    [Fact]
    public void CreateDraft_WithoutLegalName_ShouldNotThrow()
    {
        // Arrange
        var employeeId = Guid.NewGuid();

        // Act
        var profile = PayrollProfile.CreateDraft(employeeId);

        // Assert
        Assert.Equal(employeeId, profile.EmployeeId);
        Assert.Equal(string.Empty, profile.EmployeeName); // no name seeded — correct for tests/simulations
    }

    [Fact]
    public void CloneWithRegime_ShouldPreserveEmployeeName()
    {
        // Arrange
        var original = PayrollProfile.CreateDraft(Guid.NewGuid(), "Arun Reddy");
        original.Activate();

        // Act — switch from New to Old for tax simulation
        var cloned = original.CloneWithRegime(TaxRegime.Old);

        // Assert
        Assert.Equal("Arun Reddy", cloned.EmployeeName);
        Assert.Equal(TaxRegime.Old, cloned.TaxRegime);
        Assert.Equal(original.EmployeeId, cloned.EmployeeId);
        Assert.Equal(original.Id, cloned.Id); // shallow clone shares the same id
    }

    [Fact]
    public void CloneWithRegime_OriginalShouldBeUnmodified()
    {
        // Arrange
        var original = PayrollProfile.CreateDraft(Guid.NewGuid(), "Test Employee");

        // Act
        var cloned = original.CloneWithRegime(TaxRegime.Old);

        // Assert — original regime is still New
        Assert.Equal(TaxRegime.New, original.TaxRegime);
        Assert.Equal(TaxRegime.Old, cloned.TaxRegime);
        Assert.NotSame(original, cloned);
    }

    [Fact]
    public void SetSalaried_ShouldClearHourlyRate()
    {
        // Arrange
        var profile = PayrollProfile.CreateDraft(Guid.NewGuid());
        profile.SetHourly(250m);

        // Act
        profile.SetSalaried(60000m);

        // Assert
        Assert.Equal(PayType.Salaried, profile.PayType);
        Assert.Equal(60000m, profile.BaseSalary);
        Assert.Equal(0m, profile.HourlyRate);
    }

    [Fact]
    public void SetHourly_ShouldClearBaseSalary()
    {
        // Arrange
        var profile = PayrollProfile.CreateDraft(Guid.NewGuid());
        profile.SetSalaried(50000m);

        // Act
        profile.SetHourly(300m);

        // Assert
        Assert.Equal(PayType.Hourly, profile.PayType);
        Assert.Equal(300m, profile.HourlyRate);
        Assert.Equal(0m, profile.BaseSalary);
    }

    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        // Arrange
        var profile = PayrollProfile.CreateDraft(Guid.NewGuid());
        Assert.False(profile.IsActive);

        // Act
        profile.Activate();

        // Assert
        Assert.True(profile.IsActive);
    }
}
