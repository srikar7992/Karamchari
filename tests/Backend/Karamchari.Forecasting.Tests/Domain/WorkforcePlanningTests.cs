using FluentAssertions;
using Karamchari.Forecasting.Domain.Workforce;
using Xunit;

namespace Karamchari.Forecasting.Tests.Domain;

/// <summary>
/// Tests for Phase 5 workforce planning domain correctness.
/// Covers the five implementation risks identified in the architect review:
///   Risk 1 — Forecast horizon leakage (formula respects date predicates)
///   Risk 2 — Double counting (idempotent domain operations)
///   Risk 3 — Cross-skill cycles (self-reference guard)
///   Risk 4 — Explainability drift (sum-of-components invariant)
///   Risk 5 — Forecast accuracy pollution (per-horizon isolation)
/// </summary>
public class WorkforcePlanningTests
{
    // ── Shared helpers ────────────────────────────────────────────────────────

    private static readonly string Tenant = "tenant-wfp-test";
    private static readonly string Loc = "HYD";
    private static readonly string Skill = "WELDER";
    private static readonly DateOnly Today = new(2026, 6, 6);

    private static SupplyForecast MakeSupply(
        int total, int futureHires = 0, int skillExpiry = 0, int attrition = 0,
        int contractExpiry = 0, int retirement = 0, int leave = 0, int absent = 0,
        ForecastHorizon horizon = ForecastHorizon.Days30)
        => SupplyForecast.Create(
            Tenant, Today, Loc, Skill,
            total, leave, skillExpiry, attrition, absent, horizon,
            reliabilityScore: 0.9m,
            futureHireAddition: futureHires,
            contractExpiryDeduction: contractExpiry,
            retirementDeduction: retirement);

    // ── Risk 1: Forecast horizon leakage ─────────────────────────────────────
    // The supply calculator (SupplyForecastCalculator) uses date predicates in DB queries.
    // These tests verify the formula layer: only components passed in affect the result.
    // DB-level predicate correctness is verified by the integration test layer.

    [Fact]
    public void Supply_FutureHireAddition_OnlyAppliedWhenPassedIn()
    {
        // Simulates: ForecastDate = Today+7 (7-day), FutureHire.StartDate = Today+90.
        // Calculator should NOT pass futureHires=3 for a 7-day window — that's the DB query.
        // At formula level: if 0 future hires passed, supply ignores them.
        var withoutHires = MakeSupply(total: 100, futureHires: 0);
        var withHires = MakeSupply(total: 100, futureHires: 3);

        withoutHires.ExpectedHeadcount.Should().Be(100);
        withHires.ExpectedHeadcount.Should().Be(103);
        withHires.FutureHireAddition.Should().Be(3);
    }

    [Fact]
    public void Supply_RetirementDeduction_OnlyAppliedWhenPassedIn()
    {
        // Retirement at Today+180 should not appear in 30-day calculation.
        // DB query for 30-day window will pass retirementDeduction=0.
        var without = MakeSupply(total: 100, retirement: 0);
        var with = MakeSupply(total: 100, retirement: 2, horizon: ForecastHorizon.Days365);

        without.ExpectedHeadcount.Should().Be(100);
        with.ExpectedHeadcount.Should().Be(98);
        with.RetirementDeduction.Should().Be(2);
    }

    [Fact]
    public void Supply_ContractExpiry_OnlyAppliedWhenPassedIn()
    {
        // Contract ending in 180 days: 30-day query passes 0, 365-day query passes actual count.
        var short_horizon = MakeSupply(total: 100, contractExpiry: 0, horizon: ForecastHorizon.Days30);
        var long_horizon = MakeSupply(total: 100, contractExpiry: 5, horizon: ForecastHorizon.Days365);

        short_horizon.ExpectedHeadcount.Should().Be(100);
        long_horizon.ExpectedHeadcount.Should().Be(95);
        long_horizon.ContractExpiryDeduction.Should().Be(5);
    }

    // ── Boundary predicate semantics (specification) ──────────────────────────
    // SupplyForecastCalculator uses these DB query predicates:
    //   SkillExpiry:    ExpiryDate        < forecastDate  (strict — expires ON day = still valid)
    //   ContractExpiry: ContractEndDate   < forecastDate  (strict — ends ON day = still employed)
    //   Retirement:     RetirementDate    < forecastDate  (strict — retires ON day = still counted)
    //   FutureHire:     ExpectedStartDate <= forecastDate (inclusive — starts ON day = available)
    //
    // These tests document the INTENDED semantics. Integration tests against a real DB must
    // verify the predicate operators match these assertions. If a predicate is changed from
    // `<` to `<=` or vice versa, the corresponding integration test must fail.

    [Fact]
    public void BoundarySemantics_SkillExpiry_OnExpiryDay_CountsAsZeroExpired()
    {
        // Semantic: ExpiryDate = forecastDate → predicate `ExpiryDate < forecastDate` = false
        // → employee still valid on that day → 0 passed to formula.
        // Formula test: passing 0 skill expiry deducts nothing.
        var supply = MakeSupply(total: 100, skillExpiry: 0);
        supply.ExpectedHeadcount.Should().Be(100,
            "employee whose cert expires ON the forecast date is still valid that day");
        supply.SkillExpiryCount.Should().Be(0);
    }

    [Fact]
    public void BoundarySemantics_SkillExpiry_DayAfterExpiry_Counts()
    {
        // Semantic: ExpiryDate = forecastDate - 1 → predicate `ExpiryDate < forecastDate` = true
        // → employee lost cert before forecast day → deducted.
        var supply = MakeSupply(total: 100, skillExpiry: 1);
        supply.ExpectedHeadcount.Should().Be(99,
            "employee whose cert expired yesterday is deducted from supply today");
        supply.SkillExpiryCount.Should().Be(1);
    }

    [Fact]
    public void BoundarySemantics_FutureHire_OnStartDay_CountsAsAvailable()
    {
        // Semantic: ExpectedStartDate = forecastDate → predicate `StartDate <= forecastDate` = true
        // → hire starting ON the forecast date is available that day → included.
        var supply = MakeSupply(total: 100, futureHires: 1);
        supply.ExpectedHeadcount.Should().Be(101,
            "hire whose start date equals the forecast date is available that day");
        supply.FutureHireAddition.Should().Be(1);
    }

    [Fact]
    public void BoundarySemantics_ContractExpiry_OnContractEndDay_CountsAsEmployed()
    {
        // Semantic: ContractEndDate = forecastDate → predicate `ContractEndDate < forecastDate` = false
        // → employee still employed on that day → 0 passed to formula.
        var supply = MakeSupply(total: 100, contractExpiry: 0);
        supply.ExpectedHeadcount.Should().Be(100,
            "employee whose contract ends ON the forecast date is still employed that day");
        supply.ContractExpiryDeduction.Should().Be(0);
    }

    [Fact]
    public void BoundarySemantics_ContractExpiry_DayAfterEnd_Counts()
    {
        // Semantic: ContractEndDate = forecastDate - 1 → predicate = true → deducted.
        var supply = MakeSupply(total: 100, contractExpiry: 2);
        supply.ExpectedHeadcount.Should().Be(98);
        supply.ContractExpiryDeduction.Should().Be(2);
    }

    [Fact]
    public void BoundarySemantics_Retirement_OnRetirementDay_CountsAsPresent()
    {
        // Semantic: ExpectedRetirementDate = forecastDate → predicate `RetirementDate < forecastDate` = false
        // → employee still counted on retirement day itself → 0 passed to formula.
        var supply = MakeSupply(total: 100, retirement: 0);
        supply.ExpectedHeadcount.Should().Be(100,
            "employee retiring ON the forecast date is still counted that day");
        supply.RetirementDeduction.Should().Be(0);
    }

    [Fact]
    public void BoundarySemantics_Retirement_DayAfterRetirementDate_Counts()
    {
        // Semantic: ExpectedRetirementDate = forecastDate - 1 → predicate = true → deducted.
        var supply = MakeSupply(total: 100, retirement: 3);
        supply.ExpectedHeadcount.Should().Be(97);
        supply.RetirementDeduction.Should().Be(3);
    }

    // ── Risk 2: Double counting / idempotency ─────────────────────────────────

    [Fact]
    public void EmployeeIndex_AssignSkill_Idempotent_NoDuplicates()
    {
        var index = WorkforceEmployeeIndex.Create(Guid.NewGuid(), Tenant);

        index.AssignSkill("WELDER");
        index.AssignSkill("WELDER"); // replay
        index.AssignSkill("WELDER"); // replay again

        index.SkillCodes.Should().ContainSingle()
            .Which.Should().Be("WELDER");
    }

    [Fact]
    public void EmployeeIndex_AssignMultipleSkills_NoDuplicates()
    {
        var index = WorkforceEmployeeIndex.Create(Guid.NewGuid(), Tenant);

        index.AssignSkill("WELDER");
        index.AssignSkill("OPERATOR");
        index.AssignSkill("WELDER"); // replay
        index.AssignSkill("OPERATOR"); // replay

        index.SkillCodes.Should().HaveCount(2);
        index.SkillCodes.Should().Contain("WELDER").And.Contain("OPERATOR");
    }

    [Fact]
    public void EmployeeIndex_Terminate_Idempotent()
    {
        var index = WorkforceEmployeeIndex.Create(Guid.NewGuid(), Tenant);

        index.Terminate();
        index.Terminate(); // replay

        index.IsActive.Should().BeFalse();
    }

    [Fact]
    public void WfpFutureHire_MarkOnboarded_Idempotent()
    {
        var hire = WfpFutureHire.Create(
            Guid.NewGuid(), Tenant, Loc, Skill, Today.AddDays(30));

        hire.MarkOnboarded();
        hire.MarkOnboarded(); // replay

        hire.IsOnboarded.Should().BeTrue();
    }

    // ── Risk 3: Cross-skill cycles ────────────────────────────────────────────
    // Current substitution is single-hop only (no traversal) so indirect cycles (A->B->A)
    // cannot produce infinite loops. Direct cycles (A->A) are prevented at domain level.

    [Fact]
    public void WfpSkillSubstitution_SelfReference_Throws()
    {
        var act = () => WfpSkillSubstitution.Create(
            Tenant, skillCode: "WELDER", substituteSkillCode: "WELDER", capacityFraction: 0.8m);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot substitute itself*");
    }

    [Fact]
    public void WfpSkillSubstitution_SelfReference_CaseInsensitive_Throws()
    {
        var act = () => WfpSkillSubstitution.Create(
            Tenant, skillCode: "welder", substituteSkillCode: "WELDER", capacityFraction: 0.8m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WfpSkillSubstitution_ValidSubstitution_Succeeds()
    {
        var sub = WfpSkillSubstitution.Create(
            Tenant, skillCode: "WELDER", substituteSkillCode: "OPERATOR", capacityFraction: 0.75m);

        sub.SkillCode.Should().Be("WELDER");
        sub.SubstituteSkillCode.Should().Be("OPERATOR");
        sub.CapacityFraction.Should().Be(0.75m);
    }

    [Fact]
    public void WfpSkillSubstitution_CapacityFraction_ClampedToRange()
    {
        var tooHigh = WfpSkillSubstitution.Create(Tenant, "A", "B", 1.5m);
        var tooLow = WfpSkillSubstitution.Create(Tenant, "A", "B", -0.5m);

        tooHigh.CapacityFraction.Should().Be(1.0m);
        tooLow.CapacityFraction.Should().Be(0.0m);
    }

    // ── Risk 4: Explainability drift — sum-of-components invariant ────────────
    // ExpectedHeadcount must always equal the formula. If someone changes the formula
    // and forgets the breakdown fields, this test fails.

    [Fact]
    public void SupplyForecast_ExplainabilityInvariant_ComponentsSumToExpectedHeadcount()
    {
        int total = 100, futureHires = 3, skillExpiry = 2, attrition = 4,
            contractExpiry = 1, retirement = 1, leave = 5, absent = 3;

        // adjustedTotal = max(0, 100 + 3 - 2 - 4 - 1 - 1) = 95
        // expectedHeadcount = max(0, 95 - 5 - 3) = 87
        var supply = MakeSupply(total, futureHires, skillExpiry, attrition,
            contractExpiry, retirement, leave, absent);

        var recomputed = Math.Max(0,
            Math.Max(0,
                supply.TotalHeadcount
                + supply.FutureHireAddition
                - supply.SkillExpiryCount
                - supply.ExpectedAttrition
                - supply.ContractExpiryDeduction
                - supply.RetirementDeduction)
            - supply.ApprovedLeaveCount
            - supply.ExpectedAbsent);

        supply.ExpectedHeadcount.Should().Be(recomputed,
            "ExpectedHeadcount must exactly equal the formula applied to the stored breakdown components");
    }

    [Fact]
    public void SupplyForecast_ExplainabilityInvariant_NeverGoesNegative()
    {
        // Even when deductions exceed supply, headcount floor is 0.
        var supply = MakeSupply(total: 10, attrition: 50, leave: 20);

        supply.ExpectedHeadcount.Should().Be(0);
    }

    [Fact]
    public void SupplyForecast_ExplainabilityInvariant_FutureHireFloor_ZeroTotal()
    {
        // 0 base headcount + 5 future hires - 10 deductions = max(0, -5) = 0
        var supply = MakeSupply(total: 0, futureHires: 5, attrition: 10);

        supply.ExpectedHeadcount.Should().Be(0);
    }

    [Theory]
    [InlineData(100, 0, 0, 0, 0, 0, 0, 0, 100)]
    [InlineData(100, 5, 0, 0, 0, 0, 0, 0, 105)] // future hires inflate
    [InlineData(100, 0, 10, 0, 0, 0, 0, 0, 90)]  // skill expiry deducts
    [InlineData(100, 0, 0, 5, 0, 0, 0, 0, 95)]   // attrition deducts
    [InlineData(100, 0, 0, 0, 3, 0, 0, 0, 97)]   // contract expiry deducts
    [InlineData(100, 0, 0, 0, 0, 2, 0, 0, 98)]   // retirement deducts
    [InlineData(100, 0, 0, 0, 0, 0, 8, 0, 92)]   // leave deducts
    [InlineData(100, 0, 0, 0, 0, 0, 0, 6, 94)]   // absence deducts
    [InlineData(100, 3, 2, 4, 1, 1, 5, 3, 87)]   // all components combined
    public void SupplyForecast_Create_CorrectExpectedHeadcount(
        int total, int futureHires, int skillExpiry, int attrition,
        int contractExpiry, int retirement, int leave, int absent, int expectedHeadcount)
    {
        var supply = MakeSupply(total, futureHires, skillExpiry, attrition,
            contractExpiry, retirement, leave, absent);

        supply.ExpectedHeadcount.Should().Be(expectedHeadcount);
    }

    // ── Risk 5: Forecast accuracy per horizon ─────────────────────────────────
    // WfpForecastAccuracy stores Horizon — so 7d and 365d accuracy are separate rows.
    // These tests verify the Record() factory stores horizon correctly and that
    // MAPE/RMSE/Bias computations are per-record (no aggregate pollution at domain level).

    [Fact]
    public void WfpForecastAccuracy_DifferentHorizons_StoredIndependently()
    {
        var acc7 = WfpForecastAccuracy.Record(Tenant, Loc, Skill, Today, ForecastHorizon.Days7, 100, 98);
        var acc365 = WfpForecastAccuracy.Record(Tenant, Loc, Skill, Today, ForecastHorizon.Days365, 100, 65);

        acc7.Horizon.Should().Be(ForecastHorizon.Days7);
        acc365.Horizon.Should().Be(ForecastHorizon.Days365);
        acc7.AbsolutePercentError.Should().BeLessThan(acc365.AbsolutePercentError,
            "7-day forecast is more accurate than 365-day in this scenario");
    }

    [Fact]
    public void WfpForecastAccuracy_SignedError_PositiveWhenOverforecast()
    {
        // Predicted 110, Actual 100 → overforecast → positive SignedError
        var acc = WfpForecastAccuracy.Record(Tenant, Loc, Skill, Today, ForecastHorizon.Days30, 110, 100);

        acc.SignedError.Should().Be(10, "positive = overforecast");
        acc.SquaredError.Should().Be(100m);
    }

    [Fact]
    public void WfpForecastAccuracy_SignedError_NegativeWhenUnderforecast()
    {
        // Predicted 90, Actual 100 → underforecast → negative SignedError
        var acc = WfpForecastAccuracy.Record(Tenant, Loc, Skill, Today, ForecastHorizon.Days30, 90, 100);

        acc.SignedError.Should().Be(-10, "negative = underforecast");
        acc.SquaredError.Should().Be(100m);
    }

    [Fact]
    public void WfpForecastAccuracy_PerfectForecast_ZeroAllErrors()
    {
        var acc = WfpForecastAccuracy.Record(Tenant, Loc, Skill, Today, ForecastHorizon.Days7, 100, 100);

        acc.SignedError.Should().Be(0);
        acc.SquaredError.Should().Be(0m);
        acc.AbsolutePercentError.Should().Be(0m);
        acc.AbsoluteError.Should().Be(0);
    }

    [Fact]
    public void WfpForecastAccuracy_ZeroActual_MapeIs100WhenPredictionNonZero()
    {
        // Edge case: no workers actually showed but forecast predicted 50
        var acc = WfpForecastAccuracy.Record(Tenant, Loc, Skill, Today, ForecastHorizon.Days7, 50, 0);

        acc.AbsolutePercentError.Should().Be(100m);
    }

    [Fact]
    public void WfpForecastAccuracy_ZeroBoth_MapeIsZero()
    {
        var acc = WfpForecastAccuracy.Record(Tenant, Loc, Skill, Today, ForecastHorizon.Days7, 0, 0);

        acc.AbsolutePercentError.Should().Be(0m);
    }

    // ── JoiningProbability correctness ────────────────────────────────────────

    [Fact]
    public void WfpFutureHire_JoiningProbability_DefaultsToOne()
    {
        var hire = WfpFutureHire.Create(Guid.NewGuid(), Tenant, Loc, Skill, Today.AddDays(30));

        hire.JoiningProbability.Should().Be(1.0m);
    }

    [Fact]
    public void WfpFutureHire_JoiningProbability_ClampsAboveOne()
    {
        var hire = WfpFutureHire.Create(Guid.NewGuid(), Tenant, Loc, Skill, Today.AddDays(30), joiningProbability: 1.5m);

        hire.JoiningProbability.Should().Be(1.0m);
    }

    [Fact]
    public void WfpFutureHire_JoiningProbability_ClampsBelowZero()
    {
        var hire = WfpFutureHire.Create(Guid.NewGuid(), Tenant, Loc, Skill, Today.AddDays(30), joiningProbability: -0.2m);

        hire.JoiningProbability.Should().Be(0.0m);
    }

    [Fact]
    public void WfpFutureHire_JoiningProbability_StoresCorrectValue()
    {
        var hire = WfpFutureHire.Create(Guid.NewGuid(), Tenant, Loc, Skill, Today.AddDays(30), joiningProbability: 0.75m);

        hire.JoiningProbability.Should().Be(0.75m);
    }

    // ── Retirement policy correctness ─────────────────────────────────────────

    [Fact]
    public void WfpRetirementPolicy_Create_StoresCorrectAge()
    {
        var policy = WfpRetirementPolicy.Create(Tenant, retirementAge: 62);

        policy.RetirementAge.Should().Be(62);
        policy.RoleCode.Should().BeNull("default policy has no role scope");
        policy.IsActive.Should().BeTrue();
    }

    [Fact]
    public void WfpRetirementPolicy_Create_RoleSpecific_StoresRoleCode()
    {
        var policy = WfpRetirementPolicy.Create(Tenant, retirementAge: 58, roleCode: "PILOT");

        policy.RetirementAge.Should().Be(58);
        policy.RoleCode.Should().Be("PILOT");
    }

    [Fact]
    public void WfpRetirementPolicy_Create_AgeBelowMin_Throws()
    {
        var act = () => WfpRetirementPolicy.Create(Tenant, retirementAge: 39);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Retirement age must be between 40 and 80*");
    }

    [Fact]
    public void WfpRetirementPolicy_Create_AgeAboveMax_Throws()
    {
        var act = () => WfpRetirementPolicy.Create(Tenant, retirementAge: 81);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Retirement age must be between 40 and 80*");
    }

    [Fact]
    public void WfpRetirementPolicy_FallbackConstant_Is60()
    {
        WfpRetirementPolicy.FallbackRetirementAge.Should().Be(60);
    }

    [Fact]
    public void WfpRetirementPolicy_Deactivate_SetsIsActiveFalse()
    {
        var policy = WfpRetirementPolicy.Create(Tenant, retirementAge: 60);
        policy.Deactivate();

        policy.IsActive.Should().BeFalse();
    }
}
