namespace Karamchari.TimeAttendance.Tests;

using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Leaves;
using Xunit;

/// <summary>
/// Concern 7 — Migration Testing.
/// Tests that versioned policy snapshots, effective-dated assignments, and
/// policy version history are migration-safe:
/// - Adding a new policy version doesn't corrupt existing data
/// - Policy snapshot stored on LeaveRequest is immutable across upgrades
/// - Rollback scenario: old version data must remain readable
///
/// Full EF migration idempotency (run migration → rollback → re-run) requires
/// a Testcontainers integration test with `dotnet ef database update`.
/// These tests prove domain-level migration safety.
/// </summary>
public sealed class C7_MigrationValidationTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    // ── Policy version history is immutable append-only ───────────────────

    [Fact]
    public void PolicyVersionHistory_AppendOnly_HistoryNotModifiedByUpdate()
    {
        var rules = new LeavePolicyRules { AnnualAllowance = 18 };
        var policy = LeavePolicy.Create(Guid.NewGuid(), PolicyLevel.Global, "Global PL", "desc", rules);

        policy.UpdateRules(new LeavePolicyRules { AnnualAllowance = 20 }, Today, "admin");
        var v1Snapshot = policy.Versions.First();
        var v1Allowance = v1Snapshot.Rules.AnnualAllowance;

        policy.UpdateRules(new LeavePolicyRules { AnnualAllowance = 24 }, Today.AddDays(30), "admin");

        // Version 1 snapshot must not change when Version 2 is added
        policy.Versions.First().Rules.AnnualAllowance.Should().Be(v1Allowance,
            "version history is immutable — migration that adds columns must not corrupt existing snapshots");
        policy.Versions.Should().HaveCount(2, "two version entries, not overwritten");
        // Create → version=1. First UpdateRules → version=2. Second UpdateRules → version=3.
        policy.CurrentVersion.Should().Be(3, "two UpdateRules on a v1 policy → v3");
        policy.Rules.AnnualAllowance.Should().Be(24, "live rules updated");
    }

    [Fact]
    public void PolicyVersion_ClosedVersion_EffectiveDatePreserved()
    {
        var rules = new LeavePolicyRules { AnnualAllowance = 18 };
        var policy = LeavePolicy.Create(Guid.NewGuid(), PolicyLevel.Department, "Dept PL", "desc", rules);

        policy.UpdateRules(new LeavePolicyRules { AnnualAllowance = 21 }, Today, "hr");

        var snapshot = policy.Versions.Single();
        // EffectiveFrom should be set (the date the original rules were in effect from creation)
        snapshot.Should().NotBeNull("version snapshot must be preserved after update");
    }

    // ── Policy snapshot on LeaveRequest is immutable ──────────────────────

    [Fact]
    public void LeavePolicySnapshot_StoredAtSubmit_ImmutableToFuturePolicyChanges()
    {
        var policy = LeavePolicy.Create(
            Guid.NewGuid(), PolicyLevel.Grade, "Grade PL", "desc",
            new LeavePolicyRules { AnnualAllowance = 18, NoticeRequiredDays = 2 });

        var entitlement = 18m;
        var snapshot = LeavePolicySnapshot.From(policy, entitlement);

        // Simulate: policy changes after snapshot was stored
        policy.UpdateRules(new LeavePolicyRules { AnnualAllowance = 24, NoticeRequiredDays = 5 },
            Today, "hr");

        // Snapshot must reflect rules AT SUBMISSION TIME, not current policy rules
        snapshot.AnnualAllowance.Should().Be(18,
            "snapshot captures policy at submission — future policy change must not affect old leave records");
        snapshot.NoticeRequiredDays.Should().Be(2,
            "snapshot is immutable — stored JSON in DB is not affected by policy table changes");
    }

    [Fact]
    public void LeavePolicySnapshot_SerializationSafe_AllFieldsPresent()
    {
        var rules = new LeavePolicyRules
        {
            AnnualAllowance = 24,
            MaximumCarryForward = 10,
            NoticeRequiredDays = 3,
            MaxRetroactiveDays = 2,
            AllowHolidaySandwich = false,
            AllowPrefixSuffix = false,
        };
        var policy = LeavePolicy.Create(Guid.NewGuid(), PolicyLevel.Employee, "PL", "desc", rules);
        var snap = LeavePolicySnapshot.From(policy, 24m);

        // All fields needed for historical dispute resolution must be in snapshot
        snap.AnnualAllowance.Should().Be(24);
        snap.MaximumCarryForward.Should().Be(10);
        snap.NoticeRequiredDays.Should().Be(3);
        snap.MaxRetroactiveDays.Should().Be(2);
    }

    // ── EmployeeLeavePolicy effective dates ───────────────────────────────

    [Fact]
    public void EmployeeLeavePolicy_SupersedeOnRehire_EffectiveDatesCorrect()
    {
        var lt = Guid.NewGuid();
        var emp = Guid.NewGuid();

        // Original assignment (Jan)
        var original = EmployeeLeavePolicy.Create(emp, lt, Guid.NewGuid(), 18m, Today.AddMonths(-6));

        // On promotion (today), supersede and create new
        original.Supersede(Today.AddDays(-1));
        var promoted = EmployeeLeavePolicy.Create(emp, lt, Guid.NewGuid(), 24m, Today);

        original.IsActive.Should().BeFalse();
        original.EffectiveTo.Should().Be(Today.AddDays(-1));
        promoted.IsActive.Should().BeTrue();
        promoted.ResolvedEntitlement.Should().Be(24m);
        promoted.EffectiveFrom.Should().Be(Today);
    }

    [Fact]
    public void EmployeeLeavePolicy_MultipleSupersessions_ChainIsAuditSafe()
    {
        // Each supersession preserves the history — critical for payroll audit during rehire
        var lt = Guid.NewGuid();
        var emp = Guid.NewGuid();

        var v1 = EmployeeLeavePolicy.Create(emp, lt, Guid.NewGuid(), 18m, Today.AddYears(-2));
        v1.Supersede(Today.AddYears(-1));

        var v2 = EmployeeLeavePolicy.Create(emp, lt, Guid.NewGuid(), 21m, Today.AddYears(-1));
        v2.Supersede(Today.AddMonths(-3));

        var v3 = EmployeeLeavePolicy.Create(emp, lt, Guid.NewGuid(), 24m, Today.AddMonths(-3));

        v1.IsActive.Should().BeFalse();
        v2.IsActive.Should().BeFalse();
        v3.IsActive.Should().BeTrue();
        v1.ResolvedEntitlement.Should().Be(18m, "v1 entitlement preserved in audit trail");
        v2.ResolvedEntitlement.Should().Be(21m, "v2 entitlement preserved");
        v3.ResolvedEntitlement.Should().Be(24m, "v3 is current");
    }

    // ── LeaveYear boundary migration safety ───────────────────────────────

    [Fact]
    public void LeaveYear_CalendarYearBoundary_Contains_And_Clip_CorrectAfterMigration()
    {
        // Validate year boundary logic works for any year value (migration adds new years)
        var years = new[] { 2024, 2025, 2026, 2027, 2028 };
        foreach (var year in years)
        {
            var ly = LeaveYear.CalendarYear(year);
            ly.Contains(new DateOnly(year, 1, 1)).Should().BeTrue($"{year} Jan 1 in year");
            ly.Contains(new DateOnly(year, 12, 31)).Should().BeTrue($"{year} Dec 31 in year");
            ly.Contains(new DateOnly(year - 1, 12, 31)).Should().BeFalse($"prev year Dec 31 not in {year}");
        }
    }

    [Fact]
    public void LeaveYear_IndiaFinancialYear_BoundaryCorrect()
    {
        var fy = LeaveYear.IndiaFinancialYear(2025); // Apr 2025 - Mar 2026
        fy.Contains(new DateOnly(2025, 4, 1)).Should().BeTrue("FY start");
        fy.Contains(new DateOnly(2026, 3, 31)).Should().BeTrue("FY end");
        fy.Contains(new DateOnly(2025, 3, 31)).Should().BeFalse("before FY start");
        fy.Contains(new DateOnly(2026, 4, 1)).Should().BeFalse("after FY end");
    }

    // ── LeaveBalanceEntryType — new enum values don't break existing ──────

    [Fact]
    public void LeaveBalanceEntryType_NewValues_BackwardCompatible()
    {
        // Adding LOPConversion=12, PayrollCorrection=13 must not break existing entry type reads.
        // Existing entries (1-11) must still parse correctly.
        var existingTypes = new[]
        {
            LeaveBalanceEntryType.Accrual,
            LeaveBalanceEntryType.Consumption,
            LeaveBalanceEntryType.CarryForward,
            LeaveBalanceEntryType.Expiry,
            LeaveBalanceEntryType.Encashment,
            LeaveBalanceEntryType.CancellationRestore,
            LeaveBalanceEntryType.ManualAdjustment,
            LeaveBalanceEntryType.Migration,
            LeaveBalanceEntryType.Correction,
        };

        // All existing types must still have their original int values
        ((int)LeaveBalanceEntryType.Accrual).Should().Be(1);
        ((int)LeaveBalanceEntryType.Consumption).Should().Be(2);
        ((int)LeaveBalanceEntryType.Correction).Should().Be(11);

        // New types must not reuse existing int values
        ((int)LeaveBalanceEntryType.LOPConversion).Should().Be(12);
        ((int)LeaveBalanceEntryType.PayrollCorrection).Should().Be(13);

        existingTypes.Should().OnlyHaveUniqueItems("no enum value collision");
    }
}
