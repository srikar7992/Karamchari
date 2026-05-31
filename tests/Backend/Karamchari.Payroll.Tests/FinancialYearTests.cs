namespace Karamchari.Payroll.Tests;

using Karamchari.Payroll.Domain;
using Karamchari.Payroll.Domain.SalaryStructures;
using Karamchari.Payroll.Services;
using Karamchari.Payroll.Services.Statutory;
using Karamchari.Payroll.Services.Statutory.Rules;
using NSubstitute;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public class FinancialYearTests
{
    [Fact]
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void RuleSetRegistryShouldReturnCorrectYear()
    {
        // Arrange
        var epfIds = new List<Guid> { Guid.NewGuid() };
        var ptProvider = Substitute.For<IProfessionalTaxProvider>();
        var projection = Substitute.For<IIncomeProjectionService>();
        var exemption = Substitute.For<IExemptionCalculator>();
        var slabs = Substitute.For<ITaxSlabProvider>();
        var repo = Substitute.For<IITDeclarationRepository>();

        var ruleSet = new FY20262027RuleSet(epfIds, ptProvider, projection, exemption, slabs, repo);

        // Assert
        Assert.Equal(2026, ruleSet.Year.StartYear);
        Assert.Equal(2027, ruleSet.Year.EndYear);
    }

    [Fact]
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void RuleSetShouldContainRequiredIndianRules()
    {
        // Arrange
        var ptProvider = Substitute.For<IProfessionalTaxProvider>();
        var projection = Substitute.For<IIncomeProjectionService>();
        var exemption = Substitute.For<IExemptionCalculator>();
        var slabs = Substitute.For<ITaxSlabProvider>();
        var repo = Substitute.For<IITDeclarationRepository>();

        var ruleSet = new FY20262027RuleSet(new List<Guid>(), ptProvider, projection, exemption, slabs, repo);

        // Assert
        Assert.Contains(ruleSet.Rules, r => r.Name == "EPF_Employee");
        Assert.Contains(ruleSet.Rules, r => r.Name == "ESIC_Employee");
        Assert.Contains(ruleSet.Rules, r => r.Name == "ProfessionalTax");
        Assert.Contains(ruleSet.Rules, r => r.Name == "TDS");
    }
}
