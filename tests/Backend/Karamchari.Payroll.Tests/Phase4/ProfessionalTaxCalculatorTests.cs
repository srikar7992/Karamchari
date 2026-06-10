// -----------------------------------------------------------------------
// <copyright file="ProfessionalTaxCalculatorTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Payroll.Domain.DeductionRules;
using Karamchari.Payroll.Services.Deductions;

namespace Karamchari.Payroll.Tests.Phase4;

/// <summary>
/// PT slabs verified per state schedule.
/// Tests both state code forms (short "KA" and long "KARNATAKA").
/// </summary>
public class ProfessionalTaxCalculatorTests
{
    private readonly ProfessionalTaxCalculator _sut = new();

    private static DeductionContext Ctx(decimal gross, string state, int month = 6) =>
        new(Guid.NewGuid(), "T1", gross, gross * 0.4m, 0m,
            OptedVoluntaryPf: false, IsEsicLocked: false, state, month, 2025);

    // ──────────────────────────────────────────────────────────────────────
    // Karnataka
    // ──────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(15_000, 0)]
    [InlineData(15_001, 150)]
    [InlineData(17_999, 150)]
    [InlineData(18_000, 200)]
    [InlineData(50_000, 200)]
    public void Karnataka_Slabs(decimal gross, decimal expected)
    {
        Assert.Equal(expected, _sut.Calculate(Ctx(gross, "KA")));
        Assert.Equal(expected, _sut.Calculate(Ctx(gross, "KARNATAKA")));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Maharashtra
    // ──────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(7_500, 0)]
    [InlineData(7_501, 175)]
    [InlineData(10_000, 175)]
    [InlineData(10_001, 200)]
    [InlineData(50_000, 200)]
    public void Maharashtra_NonFebruary_Slabs(decimal gross, decimal expected)
    {
        Assert.Equal(expected, _sut.Calculate(Ctx(gross, "MH", month: 6)));
    }

    [Fact]
    public void Maharashtra_February_AboveThreshold_Is_300()
    {
        // February is special: >10000 in Feb = 300
        Assert.Equal(300m, _sut.Calculate(Ctx(15_000, "MH", month: 2)));
    }

    [Fact]
    public void Maharashtra_February_BelowThreshold_NotAffected()
    {
        // Below 7500: still 0 in Feb
        Assert.Equal(0m, _sut.Calculate(Ctx(7_000, "MH", month: 2)));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Andhra Pradesh & Telangana (same slabs)
    // ──────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(15_000, 0)]
    [InlineData(15_001, 150)]
    [InlineData(20_000, 150)]
    [InlineData(20_001, 200)]
    [InlineData(50_000, 200)]
    public void AndhraPradesh_Slabs(decimal gross, decimal expected)
    {
        Assert.Equal(expected, _sut.Calculate(Ctx(gross, "AP")));
        Assert.Equal(expected, _sut.Calculate(Ctx(gross, "ANDHRA PRADESH")));
    }

    [Theory]
    [InlineData(15_000, 0)]
    [InlineData(15_001, 150)]
    [InlineData(20_001, 200)]
    public void Telangana_SameSlabsAsAP(decimal gross, decimal expected)
    {
        var ap = _sut.Calculate(Ctx(gross, "AP"));
        var ts = _sut.Calculate(Ctx(gross, "TS"));
        Assert.Equal(ap, ts);
        Assert.Equal(expected, ts);
    }

    // ──────────────────────────────────────────────────────────────────────
    // West Bengal (5 tiers)
    // ──────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(10_000, 0)]
    [InlineData(10_001, 110)]
    [InlineData(15_000, 110)]
    [InlineData(15_001, 130)]
    [InlineData(25_000, 130)]
    [InlineData(25_001, 150)]
    [InlineData(40_000, 150)]
    [InlineData(40_001, 200)]
    [InlineData(1_00_000, 200)]
    public void WestBengal_FiveTier_Slabs(decimal gross, decimal expected)
    {
        Assert.Equal(expected, _sut.Calculate(Ctx(gross, "WB")));
        Assert.Equal(expected, _sut.Calculate(Ctx(gross, "WEST BENGAL")));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Tamil Nadu
    // ──────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(21_000, 0)]
    [InlineData(21_001, 208)]
    [InlineData(50_000, 208)]
    public void TamilNadu_Slabs(decimal gross, decimal expected)
    {
        Assert.Equal(expected, _sut.Calculate(Ctx(gross, "TN")));
        Assert.Equal(expected, _sut.Calculate(Ctx(gross, "TAMIL NADU")));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Gujarat
    // ──────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(5_999, 0)]
    [InlineData(6_000, 80)]
    [InlineData(8_999, 80)]
    [InlineData(9_000, 150)]
    [InlineData(11_999, 150)]
    [InlineData(12_000, 200)]
    [InlineData(50_000, 200)]
    public void Gujarat_Slabs(decimal gross, decimal expected)
    {
        Assert.Equal(expected, _sut.Calculate(Ctx(gross, "GJ")));
        Assert.Equal(expected, _sut.Calculate(Ctx(gross, "GUJARAT")));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Madhya Pradesh
    // ──────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(18_750, 0)]
    [InlineData(18_751, 208)]
    [InlineData(50_000, 208)]
    public void MadhyaPradesh_Slabs(decimal gross, decimal expected)
    {
        Assert.Equal(expected, _sut.Calculate(Ctx(gross, "MP")));
        Assert.Equal(expected, _sut.Calculate(Ctx(gross, "MADHYA PRADESH")));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Odisha
    // ──────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(13_333, 0)]
    [InlineData(13_334, 125)]
    [InlineData(25_000, 125)]
    [InlineData(25_001, 200)]
    [InlineData(50_000, 200)]
    public void Odisha_Slabs(decimal gross, decimal expected)
    {
        Assert.Equal(expected, _sut.Calculate(Ctx(gross, "OR")));
        Assert.Equal(expected, _sut.Calculate(Ctx(gross, "OD")));
        Assert.Equal(expected, _sut.Calculate(Ctx(gross, "ODISHA")));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Assam
    // ──────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(10_000, 0)]
    [InlineData(10_001, 208)]
    [InlineData(50_000, 208)]
    public void Assam_Slabs(decimal gross, decimal expected)
    {
        Assert.Equal(expected, _sut.Calculate(Ctx(gross, "AS")));
        Assert.Equal(expected, _sut.Calculate(Ctx(gross, "ASSAM")));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Default / unknown state
    // ──────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(10_000, 0)]
    [InlineData(10_001, 200)]
    [InlineData(50_000, 200)]
    public void UnknownState_DefaultFallback(decimal gross, decimal expected)
    {
        Assert.Equal(expected, _sut.Calculate(Ctx(gross, "XX")));
        Assert.Equal(expected, _sut.Calculate(Ctx(gross, "")));
    }

    [Fact]
    public void NullStateCode_DefaultFallback()
    {
        // StateCode null → default → gross>10k = 200
        var ctx = new DeductionContext(Guid.NewGuid(), "T1", 20_000, 8_000, 0m,
            OptedVoluntaryPf: false, IsEsicLocked: false, null!, 6, 2025);
        Assert.Equal(200m, _sut.Calculate(ctx));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Case insensitivity
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void StateCode_CaseInsensitive()
    {
        var lower = _sut.Calculate(Ctx(20_000, "karnataka"));
        var upper = _sut.Calculate(Ctx(20_000, "KARNATAKA"));
        var abbrev = _sut.Calculate(Ctx(20_000, "KA"));
        Assert.Equal(upper, lower);
        Assert.Equal(upper, abbrev);
    }
}
