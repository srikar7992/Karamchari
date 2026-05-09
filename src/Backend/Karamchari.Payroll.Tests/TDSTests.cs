namespace Karamchari.Payroll.Tests;

using Karamchari.Payroll.Domain;
using Karamchari.Payroll.Domain.Statutory;
using Karamchari.Payroll.Services;
using Karamchari.Payroll.Services.Statutory;
using Karamchari.Payroll.Services.Statutory.Rules;
using NSubstitute;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public class TDSTests
{
    private static readonly FinancialYear FY2026 = new(2026, 2027);

    [Fact]
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task TdsShouldCalculateMonthlyDeductionBasedOnProjection()
    {
        // Arrange
        var context = CreateContext(300000); // 3L Gross

        var projection = Substitute.For<IIncomeProjectionService>();
        projection.ProjectAnnualIncomeAsync(Arg.Any<StatutoryContext>())
            .Returns(Task.FromResult(new AnnualProjection(0, 0, 1200000, 12)));

        var exemption = Substitute.For<IExemptionCalculator>();
        exemption.CalculateHraExemption(Arg.Any<decimal>(), Arg.Any<decimal>(), Arg.Any<decimal>(), Arg.Any<bool>())
            .Returns(0m);

        var slabs = Substitute.For<ITaxSlabProvider>();
        slabs.CalculateAnnualTax(Arg.Any<decimal>(), Arg.Any<TaxRegime>())
            .Returns(120000m); // 1.2L annual tax â†’ 10k/month

        var repo = Substitute.For<IITDeclarationRepository>();
        repo.GetApprovedDeclarationsAsync(Arg.Any<Guid>(), Arg.Any<int>())
            .Returns(Task.FromResult<IReadOnlyList<ITDeclaration>>(Array.Empty<ITDeclaration>()));

        var rule = new TdsStatutoryRule(projection, exemption, slabs, repo);

        // Act
        var result = await rule.ApplyAsync(context);

        // Assert
        Assert.True(result.IsApplicable);
        Assert.Equal("TDS", result.RuleName);
        Assert.Equal(10000, result.Amount); // 120,000 / 12
    }

    [Fact]
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task TdsShouldReturnZeroWhenNoTaxLiability()
    {
        // Arrange â€” income below 7L â†’ New regime tax = 0 (rebate u/s 87A)
        var context = CreateContext(50000);

        var projection = Substitute.For<IIncomeProjectionService>();
        projection.ProjectAnnualIncomeAsync(Arg.Any<StatutoryContext>())
            .Returns(Task.FromResult(new AnnualProjection(0, 0, 600000, 12)));

        var exemption = Substitute.For<IExemptionCalculator>();
        exemption.CalculateHraExemption(Arg.Any<decimal>(), Arg.Any<decimal>(), Arg.Any<decimal>(), Arg.Any<bool>())
            .Returns(0m);

        var slabs = Substitute.For<ITaxSlabProvider>();
        slabs.CalculateAnnualTax(Arg.Any<decimal>(), Arg.Any<TaxRegime>())
            .Returns(0m); // Below 87A threshold

        var repo = Substitute.For<IITDeclarationRepository>();
        repo.GetApprovedDeclarationsAsync(Arg.Any<Guid>(), Arg.Any<int>())
            .Returns(Task.FromResult<IReadOnlyList<ITDeclaration>>(Array.Empty<ITDeclaration>()));

        var rule = new TdsStatutoryRule(projection, exemption, slabs, repo);

        // Act
        var result = await rule.ApplyAsync(context);

        // Assert
        Assert.Equal(0, result.Amount);
    }

    [Fact]
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void HraExemptionShouldApplyCorrectMath()
    {
        // Arrange
        var calc = new ExemptionCalculator();
        decimal basic = 500000;
        decimal hra = 200000;
        decimal rent = 240000;

        // Act
        // Metro: Min(50% Basic=250k, Rent-10%Basic=190k, HRA=200k) â†’ Min is 190k
        var exemptionMetro = calc.CalculateHraExemption(basic, hra, rent, isMetro: true);

        // Non-metro: Min(40% Basic=200k, Rent-10%Basic=190k, HRA=200k) â†’ Min is 190k
        var exemptionNonMetro = calc.CalculateHraExemption(basic, hra, rent, isMetro: false);

        // Assert
        Assert.Equal(190000, exemptionMetro);
        Assert.Equal(190000, exemptionNonMetro); // same here because rent factor is the binding constraint
    }

    [Fact]
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

    [Fact]
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void OldRegimeShouldApplySlabsAbove250k()
    {
        // Arrange
        var provider = new TaxSlabProvider();

        // Act â€” old regime basic exemption is 2.5L; income just above that
        var taxAt0 = provider.CalculateAnnualTax(250000, TaxRegime.Old);
        var taxAt500k = provider.CalculateAnnualTax(500000, TaxRegime.Old);
        var taxAt600k = provider.CalculateAnnualTax(600000, TaxRegime.Old);

        // Assert
        Assert.Equal(0, taxAt0);
        Assert.Equal(0, taxAt500k);
        Assert.True(taxAt600k > 0);
    }

    private static StatutoryContext CreateContext(decimal gross)
    {
        var profile = PayrollProfile.CreateDraft(Guid.Empty);
        var breakdown = new CTCBreakdownResult(gross * 12, gross, new Dictionary<Guid, decimal>(), new Dictionary<Guid, decimal>());
        return new StatutoryContext(breakdown, profile, FY2026, 4);
    }
}
