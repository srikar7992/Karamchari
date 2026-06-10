// -----------------------------------------------------------------------
// <copyright file="BoundaryPredicateIntegrationTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Forecasting.Domain.Workforce;
using Karamchari.Forecasting.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Karamchari.Forecasting.IntegrationTests.Consumers;

/// <summary>
/// Verifies that the DB query predicates in SupplyForecastCalculator match the boundary
/// semantics documented in domain spec tests (WorkforcePlanningTests.cs, BoundarySemantics_* tests).
///
/// These tests exercise real SQL Server queries via EF Core — they prove the predicate operators
/// (< vs <=) are correct against a live database, something unit tests cannot do.
///
/// Semantics being validated:
///   SkillExpiry:    ExpiryDate        <  forecastDate  (exclusive — expires ON day = still valid)
///   ContractExpiry: ContractEndDate   <  forecastDate  (exclusive — ends ON day = still employed)
///   Retirement:     RetirementDate    <  forecastDate  (exclusive — retires ON day = still counted)
///   FutureHire:     ExpectedStartDate <= forecastDate  (inclusive — starts ON day = available)
/// </summary>
[Collection(nameof(ForecastingIntegrationCollection))]
public sealed class BoundaryPredicateIntegrationTests(ForecastingSqlServerFixture fixture)
{
    private static readonly DateOnly ForecastDate = new(2026, 9, 1);

    // ── Helper: build a projection with N employees, unique per test ──────────

    private static WorkforcePlanningProjection MakeProjection(
        string tenantId, string locationCode, string skillCode, int headcount = 10)
    {
        var proj = WorkforcePlanningProjection.Create(tenantId, locationCode, skillCode);
        for (var i = 0; i < headcount; i++)
            proj.IncrementHeadcount();
        return proj;
    }

    // ── Skill Expiry predicate ─────────────────────────────────────────────────

    [DockerRequiredFact]
    public async Task SkillExpiry_ExpiryDateEqualsForecaseDate_NotDeducted()
    {
        // Semantic: ExpiryDate < forecastDate → expires ON the day = false → not deducted
        var tenantId = $"se-eq-{Guid.NewGuid():N}";
        const string loc = "DEFAULT", skill = "WELDER";
        var employeeId = Guid.NewGuid();

        await using var db = fixture.CreateDbContext();
        db.WfpSkillExpiries.Add(
            WfpSkillExpiry.Create(employeeId, tenantId, loc, skill, ForecastDate)); // expires ON forecast day
        await db.SaveChangesAsync();

        var calc = fixture.CreateSupplyCalculator(db);
        var proj = MakeProjection(tenantId, loc, skill, headcount: 10);
        var result = await calc.CalculateAsync(tenantId, proj, ForecastDate, ForecastHorizon.Days30, default);

        result.SkillExpiryCount.Should().Be(0,
            "ExpiryDate == ForecastDate → predicate ExpiryDate < ForecastDate is false → cert still valid that day");
        result.ExpectedHeadcount.Should().Be(10,
            "no deduction applied — employee with expiry on the forecast date is still counted");
    }

    [DockerRequiredFact]
    public async Task SkillExpiry_ExpiryDateBeforeForecaseDate_Deducted()
    {
        // Semantic: ExpiryDate < forecastDate → expired yesterday = true → deducted
        var tenantId = $"se-lt-{Guid.NewGuid():N}";
        const string loc = "DEFAULT", skill = "WELDER";
        var employeeId = Guid.NewGuid();

        await using var db = fixture.CreateDbContext();
        db.WfpSkillExpiries.Add(
            WfpSkillExpiry.Create(employeeId, tenantId, loc, skill, ForecastDate.AddDays(-1)));
        await db.SaveChangesAsync();

        var calc = fixture.CreateSupplyCalculator(db);
        var proj = MakeProjection(tenantId, loc, skill, headcount: 10);
        var result = await calc.CalculateAsync(tenantId, proj, ForecastDate, ForecastHorizon.Days30, default);

        result.SkillExpiryCount.Should().Be(1,
            "ExpiryDate = ForecastDate - 1 → predicate ExpiryDate < ForecastDate is true → deducted");
        result.ExpectedHeadcount.Should().Be(9);
    }

    // ── Future Hire predicate ─────────────────────────────────────────────────

    [DockerRequiredFact]
    public async Task FutureHire_StartDateEqualsForecaseDate_CountsAsAvailable()
    {
        // Semantic: ExpectedStartDate <= forecastDate → starts ON the day = true → inflates supply
        var tenantId = $"fh-eq-{Guid.NewGuid():N}";
        const string loc = "DEFAULT", skill = "OPERATOR";
        var offerId = Guid.NewGuid();

        await using var db = fixture.CreateDbContext();
        db.WfpFutureHires.Add(
            WfpFutureHire.Create(offerId, tenantId, loc, skill, ForecastDate, joiningProbability: 1.0m));
        await db.SaveChangesAsync();

        var calc = fixture.CreateSupplyCalculator(db);
        var proj = MakeProjection(tenantId, loc, skill, headcount: 10);
        var result = await calc.CalculateAsync(tenantId, proj, ForecastDate, ForecastHorizon.Days30, default);

        result.FutureHireAddition.Should().Be(1,
            "ExpectedStartDate == ForecastDate → predicate StartDate <= ForecastDate is true → hire counts");
        result.ExpectedHeadcount.Should().Be(11);
    }

    [DockerRequiredFact]
    public async Task FutureHire_StartDateAfterForecastDate_DoesNotCount()
    {
        // Semantic: ExpectedStartDate <= forecastDate → starts tomorrow = false → doesn't count
        var tenantId = $"fh-gt-{Guid.NewGuid():N}";
        const string loc = "DEFAULT", skill = "OPERATOR";
        var offerId = Guid.NewGuid();

        await using var db = fixture.CreateDbContext();
        db.WfpFutureHires.Add(
            WfpFutureHire.Create(offerId, tenantId, loc, skill, ForecastDate.AddDays(1)));
        await db.SaveChangesAsync();

        var calc = fixture.CreateSupplyCalculator(db);
        var proj = MakeProjection(tenantId, loc, skill, headcount: 10);
        var result = await calc.CalculateAsync(tenantId, proj, ForecastDate, ForecastHorizon.Days30, default);

        result.FutureHireAddition.Should().Be(0,
            "ExpectedStartDate > ForecastDate → hire not yet available on this forecast date");
        result.ExpectedHeadcount.Should().Be(10);
    }

    [DockerRequiredFact]
    public async Task FutureHire_ProbabilisticSum_RoundedCorrectly()
    {
        // Semantic: SUM(JoiningProbability) not COUNT(*). 3 hires at 0.75 = 2.25 → rounds to 2.
        var tenantId = $"fh-prob-{Guid.NewGuid():N}";
        const string loc = "DEFAULT", skill = "ENGINEER";

        await using var db = fixture.CreateDbContext();
        for (var i = 0; i < 3; i++)
        {
            db.WfpFutureHires.Add(
                WfpFutureHire.Create(Guid.NewGuid(), tenantId, loc, skill, ForecastDate.AddDays(-1), 0.75m));
        }
        await db.SaveChangesAsync();

        var calc = fixture.CreateSupplyCalculator(db);
        var proj = MakeProjection(tenantId, loc, skill, headcount: 10);
        var result = await calc.CalculateAsync(tenantId, proj, ForecastDate, ForecastHorizon.Days30, default);

        result.FutureHireAddition.Should().Be(2,
            "SUM(0.75 × 3) = 2.25 → Math.Round → 2; not COUNT(*) = 3");
        result.ExpectedHeadcount.Should().Be(12);
    }

    // ── Contract Expiry predicate ─────────────────────────────────────────────

    [DockerRequiredFact]
    public async Task ContractExpiry_ContractEndDateEqualsForecaseDate_NotDeducted()
    {
        // Semantic: ContractEndDate < forecastDate → ends ON the day = false → still employed that day
        var tenantId = $"ce-eq-{Guid.NewGuid():N}";
        const string loc = "DEFAULT", skill = "CONTRACTOR";
        var employeeId = Guid.NewGuid();

        await using var db = fixture.CreateDbContext();
        db.WfpContractExpiries.Add(
            WfpContractExpiry.Create(employeeId, tenantId, loc, skill, ForecastDate));
        await db.SaveChangesAsync();

        var calc = fixture.CreateSupplyCalculator(db);
        var proj = MakeProjection(tenantId, loc, skill, headcount: 10);
        var result = await calc.CalculateAsync(tenantId, proj, ForecastDate, ForecastHorizon.Days30, default);

        result.ContractExpiryDeduction.Should().Be(0,
            "ContractEndDate == ForecastDate → predicate ContractEndDate < ForecastDate is false → still employed that day");
        result.ExpectedHeadcount.Should().Be(10);
    }

    [DockerRequiredFact]
    public async Task ContractExpiry_ContractEndDateBeforeForecaseDate_Deducted()
    {
        var tenantId = $"ce-lt-{Guid.NewGuid():N}";
        const string loc = "DEFAULT", skill = "CONTRACTOR";
        var employeeId = Guid.NewGuid();

        await using var db = fixture.CreateDbContext();
        db.WfpContractExpiries.Add(
            WfpContractExpiry.Create(employeeId, tenantId, loc, skill, ForecastDate.AddDays(-1)));
        await db.SaveChangesAsync();

        var calc = fixture.CreateSupplyCalculator(db);
        var proj = MakeProjection(tenantId, loc, skill, headcount: 10);
        var result = await calc.CalculateAsync(tenantId, proj, ForecastDate, ForecastHorizon.Days30, default);

        result.ContractExpiryDeduction.Should().Be(1,
            "ContractEndDate = ForecastDate - 1 → expired before forecast → deducted");
        result.ExpectedHeadcount.Should().Be(9);
    }

    // ── Retirement predicate ───────────────────────────────────────────────────

    [DockerRequiredFact]
    public async Task Retirement_RetirementDateEqualsForecaseDate_NotDeducted()
    {
        // Semantic: ExpectedRetirementDate < forecastDate → retires ON the day = false → still counted
        var tenantId = $"ret-eq-{Guid.NewGuid():N}";
        const string loc = "DEFAULT", skill = "SENIOR";
        var employeeId = Guid.NewGuid();

        await using var db = fixture.CreateDbContext();
        db.WfpRetirementRisks.Add(
            WfpRetirementRisk.Create(employeeId, tenantId, loc, skill, ForecastDate));
        await db.SaveChangesAsync();

        var calc = fixture.CreateSupplyCalculator(db);
        var proj = MakeProjection(tenantId, loc, skill, headcount: 10);
        var result = await calc.CalculateAsync(tenantId, proj, ForecastDate, ForecastHorizon.Days365, default);

        result.RetirementDeduction.Should().Be(0,
            "RetirementDate == ForecastDate → predicate RetirementDate < ForecastDate is false → still counted on retirement day itself");
        result.ExpectedHeadcount.Should().BeGreaterThan(0);
    }

    [DockerRequiredFact]
    public async Task Retirement_RetirementDateBeforeForecaseDate_Deducted()
    {
        var tenantId = $"ret-lt-{Guid.NewGuid():N}";
        const string loc = "DEFAULT", skill = "SENIOR";
        var employeeId = Guid.NewGuid();

        await using var db = fixture.CreateDbContext();
        db.WfpRetirementRisks.Add(
            WfpRetirementRisk.Create(employeeId, tenantId, loc, skill, ForecastDate.AddDays(-1)));
        await db.SaveChangesAsync();

        var calc = fixture.CreateSupplyCalculator(db);
        var proj = MakeProjection(tenantId, loc, skill, headcount: 10);
        var result = await calc.CalculateAsync(tenantId, proj, ForecastDate, ForecastHorizon.Days365, default);

        result.RetirementDeduction.Should().Be(1,
            "RetirementDate = ForecastDate - 1 → retired before forecast → deducted");
    }

    // ── Cross-predicate: multiple components on same forecast date ────────────

    [DockerRequiredFact]
    public async Task MultipleComponents_OnBoundaryDate_PredicatesApplyIndependently()
    {
        // One SkillExpiry ON the day (not deducted), one ContractExpiry BEFORE (deducted),
        // one FutureHire ON the day (counted). Net: +1 hire -1 contract = 0 change from 10.
        var tenantId = $"multi-{Guid.NewGuid():N}";
        const string loc = "DEFAULT", skill = "MIXED";
        var emp1 = Guid.NewGuid();
        var emp2 = Guid.NewGuid();
        var offer = Guid.NewGuid();

        await using var db = fixture.CreateDbContext();
        db.WfpSkillExpiries.Add(
            WfpSkillExpiry.Create(emp1, tenantId, loc, skill, ForecastDate)); // ON → not deducted
        db.WfpContractExpiries.Add(
            WfpContractExpiry.Create(emp2, tenantId, loc, skill, ForecastDate.AddDays(-1))); // BEFORE → deducted
        db.WfpFutureHires.Add(
            WfpFutureHire.Create(offer, tenantId, loc, skill, ForecastDate)); // ON → counted
        await db.SaveChangesAsync();

        var calc = fixture.CreateSupplyCalculator(db);
        var proj = MakeProjection(tenantId, loc, skill, headcount: 10);
        var result = await calc.CalculateAsync(tenantId, proj, ForecastDate, ForecastHorizon.Days30, default);

        result.SkillExpiryCount.Should().Be(0, "expiry ON day = not deducted");
        result.ContractExpiryDeduction.Should().Be(1, "contract before day = deducted");
        result.FutureHireAddition.Should().Be(1, "hire ON day = counted");
        // Total: 10 + 1 - 0 - 1 = 10
        result.ExpectedHeadcount.Should().Be(10);
    }
}
