// -----------------------------------------------------------------------
// <copyright file="EsiCalculatorTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Payroll.Domain.DeductionRules;
using Karamchari.Payroll.Services.Deductions;

namespace Karamchari.Payroll.Tests.Phase4;

/// <summary>
/// ESI: 0.75% employee, 3.25% employer on gross ≤ ₹21,000.
/// Stops when IsEsicLocked = true (employee's gross exceeded ceiling mid-year).
/// </summary>
public class EsiCalculatorTests
{
    private readonly EsiCalculator _sut = new();

    private static DeductionContext Ctx(decimal gross, bool locked = false) =>
        new(Guid.NewGuid(), "T1", gross, gross * 0.4m, 0m,
            OptedVoluntaryPf: false, IsEsicLocked: locked, "MH", 6, 2025);

    // ──────────────────────────────────────────────────────────────────────
    // Employee contribution
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void ESI_BelowThreshold_0_75pct_OfGross()
    {
        // Gross 15,000 → 0.75% = 112.5 → Math.Round half-even = 112 (112 is even)
        var result = _sut.Calculate(Ctx(15_000));
        Assert.Equal(112m, result);
    }

    [Fact]
    public void ESI_At_21000_Exactly_Applies()
    {
        // Gross exactly 21,000 → eligible → 0.75% = 157.5 → 158
        var result = _sut.Calculate(Ctx(21_000));
        Assert.Equal(158m, result);
    }

    [Fact]
    public void ESI_Above_21000_Threshold_ZeroDeduction()
    {
        // Gross 21,001 → above ceiling → ESI = 0
        var result = _sut.Calculate(Ctx(21_001));
        Assert.Equal(0m, result);
    }

    [Fact]
    public void ESI_HighSalary_ZeroDeduction()
    {
        var result = _sut.Calculate(Ctx(100_000));
        Assert.Equal(0m, result);
    }

    [Fact]
    public void ESI_ZeroGross_ZeroDeduction()
    {
        var result = _sut.Calculate(Ctx(0));
        Assert.Equal(0m, result);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Lock detection: once locked, ESI stops for the contribution period
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void ESI_Locked_ZeroDeduction_EvenBelowThreshold()
    {
        // Locked employee at 10k gross — still 0 because contribution period closed
        var result = _sut.Calculate(Ctx(10_000, locked: true));
        Assert.Equal(0m, result);
    }

    [Fact]
    public void ESI_NotLocked_AppliesNormally()
    {
        var locked = _sut.Calculate(Ctx(15_000, locked: true));
        var notLocked = _sut.Calculate(Ctx(15_000, locked: false));
        Assert.Equal(0m, locked);
        Assert.True(notLocked > 0);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Employer contribution
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void ESI_EmployerContribution_3_25pct_BelowThreshold()
    {
        // Gross 15,000 → 3.25% = 487.5 → round = 488
        var result = _sut.EmployerContribution(Ctx(15_000));
        Assert.Equal(488m, result);
    }

    [Fact]
    public void ESI_EmployerContribution_Zero_AboveThreshold()
    {
        var result = _sut.EmployerContribution(Ctx(25_000));
        Assert.Equal(0m, result);
    }

    [Fact]
    public void ESI_EmployerContribution_Zero_WhenLocked()
    {
        var result = _sut.EmployerContribution(Ctx(15_000, locked: true));
        Assert.Equal(0m, result);
    }

    [Fact]
    public void ESI_EmployerContribution_At_21000_Exactly()
    {
        // 3.25% * 21000 = 682.5 → Math.Round half-even = 682 (682 is even)
        var result = _sut.EmployerContribution(Ctx(21_000));
        Assert.Equal(682m, result);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Combined employee + employer rate = 4%
    // ──────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(10_000)]
    [InlineData(15_000)]
    [InlineData(21_000)]
    public void ESI_EmployeePlusEmployer_Approximately_4pct(decimal gross)
    {
        var emp = _sut.Calculate(Ctx(gross));
        var employer = _sut.EmployerContribution(Ctx(gross));
        var total = emp + employer;
        var expected = Math.Round(gross * 0.04m, 0);
        // Allow ±1 rounding tolerance
        Assert.True(Math.Abs(total - expected) <= 1m,
            $"Combined ESI {total} should be ~4% of {gross} = {expected}");
    }
}
