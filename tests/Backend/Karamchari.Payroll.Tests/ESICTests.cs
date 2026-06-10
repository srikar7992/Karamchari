// -----------------------------------------------------------------------
// <copyright file="ESICTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Tests;

using Karamchari.Payroll.Domain;
using Karamchari.Payroll.Domain.SalaryStructures;
using Karamchari.Payroll.Services;
using Karamchari.Payroll.Services.Statutory;
using Karamchari.Payroll.Services.Statutory.Rules;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public class ESICTests
{
    private static readonly FinancialYear FY2026 = new(2026, 2027);

    [Theory]
    [InlineData(20999, true)]  // Under threshold
    [InlineData(21000, true)]  // At threshold
    [InlineData(21001, false)] // Over threshold
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void ESICShouldDetermineApplicabilityBasedOnThreshold(decimal grossMonthly, bool expectedApplicable)
    {
        // Arrange
        var profile = PayrollProfile.CreateDraft(Guid.Empty);
        var breakdown = CreateBreakdown(grossMonthly);
        var context = new StatutoryContext(breakdown, profile, FY2026, 4);
        var rule = new EsicStatutoryRule();

        // Act
        var result = rule.Apply(context);

        // Assert
        Assert.Equal(expectedApplicable, result.IsApplicable);
    }

    [Fact]
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void ESICShouldCalculate075PercentAndCeilRounding()
    {
        // Arrange
        var profile = PayrollProfile.CreateDraft(Guid.Empty);
        var grossMonthly = 20001m; // 0.75% = 150.0075
        var breakdown = CreateBreakdown(grossMonthly);
        var context = new StatutoryContext(breakdown, profile, FY2026, 4);
        var rule = new EsicStatutoryRule();

        // Act
        var result = rule.Apply(context);

        // Assert
        Assert.Equal(151, result.Amount); // Ceil(150.0075)
    }

    [Fact]
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void ESICShouldApplyIfLockedEvenIfOverThreshold()
    {
        // Arrange
        var profile = PayrollProfile.CreateDraft(Guid.Empty);
        SetPrivateProperty(profile, nameof(PayrollProfile.IsEsicLocked), true);

        var grossMonthly = 25000m; // Over 21k
        var breakdown = CreateBreakdown(grossMonthly);
        var context = new StatutoryContext(breakdown, profile, FY2026, 4);
        var rule = new EsicStatutoryRule();

        // Act
        var result = rule.Apply(context);

        // Assert
        Assert.True(result.IsApplicable);
        Assert.Equal(188, result.Amount); // Ceil(25000 * 0.0075) = 187.5 -> 188
        Assert.Contains("Period Locked", result.Explanation);
    }

    private static CTCBreakdownResult CreateBreakdown(decimal grossMonthly)
    {
        return new CTCBreakdownResult(
            AnnualCTC: grossMonthly * 12,
            MonthlyGross: grossMonthly,
            MonthlyBreakdown: new Dictionary<Guid, decimal>(),
            AnnualBreakdown: new Dictionary<Guid, decimal>());
    }

    private static void SetPrivateProperty(object obj, string propertyName, object value)
    {
        var prop = obj.GetType().GetProperty(propertyName);
        prop?.SetValue(obj, value);
    }
}
