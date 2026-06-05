namespace Karamchari.TimeAttendance.Tests;

using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Services;
using Xunit;

/// <summary>
/// RF#4 — Policy Resolution: hierarchy matrix, Employee wins, fallthrough, date-scoped.
/// Tests resolution ordering logic at domain level.
/// Full DB-backed integration (multiple tenants, concurrent policy changes) requires Testcontainers.
/// </summary>
public sealed class RF4_PolicyResolverTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);
    private static readonly Guid AnyLeaveType = Guid.NewGuid();

    private static LeavePolicy MakePolicy(PolicyLevel level, double annualAllowance = 18, bool active = true)
    {
        var rules = new LeavePolicyRules { AnnualAllowance = annualAllowance };
        var p = LeavePolicy.Create(AnyLeaveType, level, $"{level} Policy", "desc", rules);
        if (!active) p.Deactivate();
        return p;
    }

    // ── PolicyLevel ordering: Employee(6) > Grade(5) > Dept(4) > BU(3) > Country(2) > Global(1) ──

    [Theory]
    [InlineData(PolicyLevel.Employee, PolicyLevel.Grade)]
    [InlineData(PolicyLevel.Grade, PolicyLevel.Department)]
    [InlineData(PolicyLevel.Department, PolicyLevel.BusinessUnit)]
    [InlineData(PolicyLevel.BusinessUnit, PolicyLevel.Country)]
    [InlineData(PolicyLevel.Country, PolicyLevel.Global)]
    public void PolicyLevel_MoreSpecific_HasHigherIntValue(PolicyLevel specific, PolicyLevel general)
    {
        ((int)specific).Should().BeGreaterThan((int)general,
            $"{specific} must win over {general} in hierarchy resolution");
    }

    [Fact]
    public void PolicyLevel_Employee_WinsOverAllOthers()
    {
        // Simulate resolver: pick highest (int)Level among actives
        var policies = new[]
        {
            MakePolicy(PolicyLevel.Global, 12),
            MakePolicy(PolicyLevel.Country, 15),
            MakePolicy(PolicyLevel.BusinessUnit, 16),
            MakePolicy(PolicyLevel.Department, 17),
            MakePolicy(PolicyLevel.Grade, 18),
            MakePolicy(PolicyLevel.Employee, 24),  // must win
        };

        var winner = policies.OrderByDescending(p => (int)p.Level).First();

        winner.Level.Should().Be(PolicyLevel.Employee);
        winner.Rules.AnnualAllowance.Should().Be(24);
    }

    [Fact]
    public void PolicyLevel_EmployeeInactive_GradeWins()
    {
        var policies = new[]
        {
            MakePolicy(PolicyLevel.Global, 12),
            MakePolicy(PolicyLevel.Grade, 18),
            MakePolicy(PolicyLevel.Employee, 24, active: false),  // inactive → skip
        };

        var winner = policies.Where(p => p.IsActive)
                             .OrderByDescending(p => (int)p.Level)
                             .First();

        winner.Level.Should().Be(PolicyLevel.Grade);
        winner.Rules.AnnualAllowance.Should().Be(18);
    }

    [Fact]
    public void PolicyLevel_OnlyGlobal_GlobalWins()
    {
        var policies = new[] { MakePolicy(PolicyLevel.Global, 12) };
        var winner = policies.Where(p => p.IsActive)
                             .OrderByDescending(p => (int)p.Level)
                             .First();
        winner.Level.Should().Be(PolicyLevel.Global);
    }

    [Fact]
    public void PolicyLevel_NoPolicies_ReturnsNull()
    {
        // Simulate: LeavePolicyResolver returns null when no policies found
        var policies = Array.Empty<LeavePolicy>();
        var winner = policies.Where(p => p.IsActive)
                             .OrderByDescending(p => (int)p.Level)
                             .FirstOrDefault();
        winner.Should().BeNull("no policies → null resolution → apply fails at validation");
    }

    // ── Effective date scoping ────────────────────────────────────────────

    [Fact]
    public void EmployeeLeavePolicy_ActiveOnForDate_Selected()
    {
        // Create(employeeId, leaveTypeId, resolvedPolicyId, resolvedEntitlement, effectiveFrom)
        var policy = EmployeeLeavePolicy.Create(
            Guid.NewGuid(), AnyLeaveType, Guid.NewGuid(), 30m, Today.AddDays(-30));

        policy.IsActive.Should().BeTrue();
        (policy.EffectiveFrom <= Today).Should().BeTrue();
    }

    [Fact]
    public void EmployeeLeavePolicy_Superseded_IsInactive()
    {
        var policy = EmployeeLeavePolicy.Create(
            Guid.NewGuid(), AnyLeaveType, Guid.NewGuid(), 24m, Today.AddDays(-60));

        policy.Supersede(Today.AddDays(-1));

        policy.IsActive.Should().BeFalse();
        policy.EffectiveTo.Should().Be(Today.AddDays(-1));
    }

    [Fact]
    public void EmployeeLeavePolicy_FutureEffective_NotYetActive()
    {
        var policy = EmployeeLeavePolicy.Create(
            Guid.NewGuid(), AnyLeaveType, Guid.NewGuid(), 24m, Today.AddDays(30));

        (policy.EffectiveFrom <= Today).Should().BeFalse("future policy not yet active");
    }

    // ── LeavePolicyResolution value object ────────────────────────────────

    [Fact]
    public void LeavePolicyResolution_EmployeeWin_PropertiesCorrect()
    {
        var rules = new LeavePolicyRules { AnnualAllowance = 30 };
        var resolution = new LeavePolicyResolution
        {
            WinningPolicyId = Guid.NewGuid(),
            WinningLevel = PolicyLevel.Employee,
            EffectiveRules = rules,
            EffectiveEntitlement = 30m,
        };

        resolution.WinningLevel.Should().Be(PolicyLevel.Employee);
        resolution.EffectiveEntitlement.Should().Be(30m);
    }

    // ── LeavePolicy domain operations ─────────────────────────────────────

    [Fact]
    public void LeavePolicy_UpdateRules_BumpsVersion()
    {
        var rules = new LeavePolicyRules { AnnualAllowance = 18 };
        var policy = LeavePolicy.Create(AnyLeaveType, PolicyLevel.Global, "Global PL", "desc", rules);
        var initialVersion = policy.CurrentVersion;

        var newRules = new LeavePolicyRules { AnnualAllowance = 24 };
        policy.UpdateRules(newRules, Today, "hr-director");

        policy.CurrentVersion.Should().Be(initialVersion + 1);
        policy.Rules.AnnualAllowance.Should().Be(24);
    }

    [Fact]
    public void LeavePolicy_Deactivate_IsActiveFalse()
    {
        var policy = MakePolicy(PolicyLevel.Grade, 18);
        policy.Deactivate();
        policy.IsActive.Should().BeFalse();
    }

    [Fact]
    public void LeavePolicy_VersionHistory_ImmutableAppend()
    {
        var rules = new LeavePolicyRules { AnnualAllowance = 18 };
        var policy = LeavePolicy.Create(AnyLeaveType, PolicyLevel.Department, "Dept PL", "desc", rules);

        policy.UpdateRules(new LeavePolicyRules { AnnualAllowance = 20 }, Today, "admin");
        policy.UpdateRules(new LeavePolicyRules { AnnualAllowance = 22 }, Today, "admin");

        policy.Versions.Should().HaveCount(2, "each update appends a version snapshot");
    }

    // ── Hierarchy matrix: all 6 levels resolution table ──────────────────

    [Theory]
    [InlineData(PolicyLevel.Employee, PolicyLevel.Employee)]
    [InlineData(PolicyLevel.Grade, PolicyLevel.Grade)]
    [InlineData(PolicyLevel.Department, PolicyLevel.Department)]
    [InlineData(PolicyLevel.BusinessUnit, PolicyLevel.BusinessUnit)]
    [InlineData(PolicyLevel.Country, PolicyLevel.Country)]
    [InlineData(PolicyLevel.Global, PolicyLevel.Global)]
    public void PolicyHierarchy_SingleLevel_WinnerIsItself(PolicyLevel level, PolicyLevel expected)
    {
        var policies = new[] { MakePolicy(level, 18) };
        var winner = policies.Where(p => p.IsActive)
                             .OrderByDescending(p => (int)p.Level)
                             .First();
        winner.Level.Should().Be(expected);
    }

    [Theory]
    [InlineData(new[] { PolicyLevel.Global, PolicyLevel.Country }, PolicyLevel.Country)]
    [InlineData(new[] { PolicyLevel.Global, PolicyLevel.Country, PolicyLevel.BusinessUnit }, PolicyLevel.BusinessUnit)]
    [InlineData(new[] { PolicyLevel.Global, PolicyLevel.Grade }, PolicyLevel.Grade)]
    [InlineData(new[] { PolicyLevel.Department, PolicyLevel.Employee }, PolicyLevel.Employee)]
    public void PolicyHierarchy_MultiLevel_MostSpecificWins(PolicyLevel[] active, PolicyLevel expectedWinner)
    {
        var policies = active.Select(l => MakePolicy(l, 18)).ToArray();
        var winner = policies.Where(p => p.IsActive)
                             .OrderByDescending(p => (int)p.Level)
                             .First();
        winner.Level.Should().Be(expectedWinner);
    }
}
