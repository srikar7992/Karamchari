namespace Karamchari.Payroll.Tests;

using Karamchari.Payroll.Domain;
using Karamchari.Payroll.Domain.SalaryStructures;
using Karamchari.Payroll.Services;
using Karamchari.Payroll.Services.Statutory;
using Karamchari.Payroll.Services.Statutory.Rules;

public class EPFTests
{
    private static readonly Guid BasicId = Guid.NewGuid();
    private static readonly FinancialYear FY2026 = new(2026, 2027);

    [Theory]
    [InlineData(10000, 1200)] // Under cap
    [InlineData(15000, 1800)] // Exactly at cap
    [InlineData(50000, 1800)] // Over cap (capped at 15k)
    public void EPFShouldApplyStatutoryCapWhenNotVPF(decimal basicMonthly, decimal expectedEPF)
    {
        // Arrange
        var profile = PayrollProfile.CreateDraft(Guid.Empty); // Default: OptedForVoluntaryPF = false
        var breakdown = CreateBreakdown(basicMonthly);
        var context = new StatutoryContext(breakdown, profile, FY2026, 4);
        var rule = new EpfStatutoryRule(new List<Guid> { BasicId });

        // Act
        var result = rule.Apply(context);

        // Assert
        Assert.Equal(expectedEPF, result.Amount);
        Assert.True(result.IsApplicable);
        if (basicMonthly > 15000)
        {
            Assert.Contains("capped", result.Explanation);
        }
    }

    [Fact]
    public void EPFShouldBypassCapWhenVPFIsEnabled()
    {
        // Arrange
        var profile = PayrollProfile.CreateDraft(Guid.Empty);
        SetPrivateProperty(profile, nameof(PayrollProfile.OptedForVoluntaryPF), true);

        var basicMonthly = 50000m;
        var breakdown = CreateBreakdown(basicMonthly);
        var context = new StatutoryContext(breakdown, profile, FY2026, 4);
        var rule = new EpfStatutoryRule(new List<Guid> { BasicId });

        // Act
        var result = rule.Apply(context);

        // Assert
        Assert.Equal(6000, result.Amount); // 12% of 50,000
        Assert.Contains("12% of wage base", result.Explanation);
    }

    private static CTCBreakdownResult CreateBreakdown(decimal basicMonthly)
    {
        var monthlyBreakdown = new Dictionary<Guid, decimal> { { BasicId, basicMonthly } };
        return new CTCBreakdownResult(
            AnnualCTC: basicMonthly * 12,
            MonthlyGross: basicMonthly,
            MonthlyBreakdown: monthlyBreakdown,
            AnnualBreakdown: monthlyBreakdown.ToDictionary(k => k.Key, v => v.Value * 12));
    }

    private static void SetPrivateProperty(object obj, string propertyName, object value)
    {
        var prop = obj.GetType().GetProperty(propertyName);
        prop?.SetValue(obj, value);
    }
}
