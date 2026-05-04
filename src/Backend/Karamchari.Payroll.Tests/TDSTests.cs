namespace Karamchari.Payroll.Tests;

using Karamchari.Payroll.Domain.Statutory;
using Karamchari.Payroll.Services.Statutory;
using Karamchari.Payroll.Services.Statutory.Rules;
using Karamchari.Payroll.Services;
using Karamchari.Payroll.Domain;
using NSubstitute;

public class TDSTests
{
    private static readonly FinancialYear FY2026 = new(2026, 2027);

    [Fact]
    public void TdsShouldCalculateMonthlyDeductionBasedOnProjection()
    {
        // Arrange
        var context = CreateContext(300000); // 3L Gross -> Above 50k Std Ded -> ~2.5L Taxable (New Regime Nil)
        
        var projection = Substitute.For<IIncomeProjectionService>();
        projection.ProjectAnnualIncomeAsync(Arg.Any<StatutoryContext>())
            .Returns(new AnnualProjection(0, 0, 1200000, 12)); // 1.2M Annual

        var exemption = Substitute.For<IExemptionCalculator>();
        var slabs = Substitute.For<ITaxSlabProvider>();
        slabs.CalculateAnnualTax(Arg.Any<decimal>(), Arg.Any<TaxRegime>()).Returns(120000m); // 1.2L Tax

        var repo = Substitute.For<IITDeclarationRepository>();

        var rule = new TdsStatutoryRule(projection, exemption, slabs, repo);

        // Act
        var result = rule.Apply(context);

        // Assert
        Assert.Equal(10000, result.Amount); // 120,000 / 12
    }

    [Fact]
    public void HraExemptionShouldApplyCorrectMath()
    {
        // Arrange
        var calc = new ExemptionCalculator();
        decimal basic = 500000;
        decimal hra = 200000;
        decimal rent = 240000;

        // Act
        // Case 1: Metro (50% of Basic = 2.5L, Rent - 10% Basic = 1.9L, HRA = 2L) -> Min is 1.9L
        var exemptionMetro = calc.CalculateHraExemption(basic, hra, rent, true);

        // Assert
        Assert.Equal(190000, exemptionMetro);
    }

    [Fact]
    public void NewRegimeSlabsShouldHandleRebate()
    {
        // Arrange
        var provider = new TaxSlabProvider();

        // Act
        var taxUnder7L = provider.CalculateAnnualTax(700000, TaxRegime.New);
        var taxOver7L = provider.CalculateAnnualTax(700001, TaxRegime.New);

        // Assert
        Assert.Equal(0, taxUnder7L);
        Assert.True(taxOver7L > 0);
    }

    private static StatutoryContext CreateContext(decimal gross)
    {
        var profile = PayrollProfile.CreateDraft(Guid.Empty);
        var breakdown = new CTCBreakdownResult(gross * 12, gross, new Dictionary<Guid, decimal>(), new Dictionary<Guid, decimal>());
        return new StatutoryContext(breakdown, profile, FY2026, 4);
    }
}
