// -----------------------------------------------------------------------
// <copyright file="Phase4_StateMachineTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Tests;

using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Leaves;
using Xunit;

/// <summary>
/// Phase 4 — State Machine Validation: exhaustive transition matrix.
/// Every from→to pair tested. Illegal transitions must throw.
/// </summary>
public sealed class Phase4_StateMachineTests
{
    private static readonly DateOnly D = DateOnly.FromDateTime(DateTime.UtcNow);

    private static LeaveRequest Draft()
        => LeaveRequest.Create(Guid.NewGuid(), Guid.NewGuid(), D, D.AddDays(4), 5);

    private static LeaveRequest Submitted()
    {
        var r = Draft(); r.Submit(); return r;
    }

    private static LeaveRequest Pending()
    {
        var r = Submitted(); r.StartApproval(); return r;
    }

    private static LeaveRequest Approved()
    {
        var r = Pending(); r.FinalizeApproved(); return r;
    }

    private static LeaveRequest Rejected()
    {
        var r = Pending(); r.FinalizeRejected("rejected"); return r;
    }

    private static LeaveRequest Cancelled_FromDraft()
    {
        var r = Draft(); r.Cancel(); return r;
    }

    private static LeaveRequest Withdrawn()
    {
        var r = Submitted(); r.Withdraw(); return r;
    }

    private static LeaveRequest Expired_FromPending()
    {
        var r = Pending(); r.Expire(); return r;
    }

    // ── Allowed transitions ──────────────────────────────────────────────

    [Fact] public void Draft_To_Submitted_Allowed() => Draft().Invoking(r => r.Submit()).Should().NotThrow();
    [Fact] public void Draft_To_Cancelled_Allowed() => Draft().Invoking(r => r.Cancel()).Should().NotThrow();
    [Fact] public void Submitted_To_PendingApproval_Allowed() => Submitted().Invoking(r => r.StartApproval()).Should().NotThrow();
    [Fact] public void Submitted_To_Withdrawn_Allowed() => Submitted().Invoking(r => r.Withdraw()).Should().NotThrow();
    [Fact] public void Submitted_To_Expired_Allowed() => Submitted().Invoking(r => r.Expire()).Should().NotThrow();
    [Fact] public void PendingApproval_To_Approved_Allowed() => Pending().Invoking(r => r.FinalizeApproved()).Should().NotThrow();
    [Fact] public void PendingApproval_To_Rejected_Allowed() => Pending().Invoking(r => r.FinalizeRejected()).Should().NotThrow();
    [Fact] public void PendingApproval_To_Expired_Allowed() => Pending().Invoking(r => r.Expire()).Should().NotThrow();
    [Fact] public void Approved_To_Cancelled_Allowed() => Approved().Invoking(r => r.Cancel()).Should().NotThrow();

    // ── Illegal transitions (must throw) ─────────────────────────────────

    [Fact]
    public void Approved_To_Draft_Illegal()
        => Approved().Invoking(r => r.Submit()).Should().Throw<InvalidOperationException>();

    [Fact]
    public void Approved_To_Submitted_Illegal()
        => Approved().Invoking(r => r.StartApproval()).Should().Throw<InvalidOperationException>();

    [Fact]
    public void Approved_To_Rejected_Illegal()
        => Approved().Invoking(r => r.FinalizeRejected()).Should().Throw<InvalidOperationException>();

    [Fact]
    public void Rejected_To_Approved_Illegal()
        => Rejected().Invoking(r => r.FinalizeApproved()).Should().Throw<InvalidOperationException>();

    [Fact]
    public void Rejected_To_Cancelled_Illegal()
        => Rejected().Invoking(r => r.Cancel()).Should().Throw<InvalidOperationException>();

    [Fact]
    public void Rejected_To_Submitted_Illegal()
        => Rejected().Invoking(r => r.Submit()).Should().Throw<InvalidOperationException>();

    [Fact]
    public void Cancelled_To_Approved_Illegal()
        => Cancelled_FromDraft().Invoking(r => r.FinalizeApproved()).Should().Throw<InvalidOperationException>();

    [Fact]
    public void Cancelled_To_Submitted_Illegal()
        => Cancelled_FromDraft().Invoking(r => r.Submit()).Should().Throw<InvalidOperationException>();

    [Fact]
    public void Withdrawn_To_Approved_Illegal()
        => Withdrawn().Invoking(r => r.FinalizeApproved()).Should().Throw<InvalidOperationException>();

    [Fact]
    public void Withdrawn_To_Submitted_Illegal()
        => Withdrawn().Invoking(r => r.Submit()).Should().Throw<InvalidOperationException>();

    [Fact]
    public void Expired_To_Approved_Illegal()
        => Expired_FromPending().Invoking(r => r.FinalizeApproved()).Should().Throw<InvalidOperationException>();

    [Fact]
    public void Expired_To_Submitted_Illegal()
        => Expired_FromPending().Invoking(r => r.Submit()).Should().Throw<InvalidOperationException>();

    [Fact]
    public void Draft_To_Approved_Illegal()
        => Draft().Invoking(r => r.FinalizeApproved()).Should().Throw<InvalidOperationException>();

    [Fact]
    public void Draft_To_Rejected_Illegal()
        => Draft().Invoking(r => r.FinalizeRejected()).Should().Throw<InvalidOperationException>();

    [Fact]
    public void Draft_To_Withdrawn_Illegal()
        => Draft().Invoking(r => r.Withdraw()).Should().Throw<InvalidOperationException>();

    [Fact]
    public void Draft_To_Expired_Illegal()
        => Draft().Invoking(r => r.Expire()).Should().Throw<InvalidOperationException>();

    // ── StateMachine.CanTransition exhaustive matrix ─────────────────────

    [Theory]
    [InlineData(LeaveRequestStatus.Draft, LeaveRequestStatus.Submitted, true)]
    [InlineData(LeaveRequestStatus.Draft, LeaveRequestStatus.Cancelled, true)]
    [InlineData(LeaveRequestStatus.Draft, LeaveRequestStatus.PendingApproval, false)]
    [InlineData(LeaveRequestStatus.Draft, LeaveRequestStatus.Approved, false)]
    [InlineData(LeaveRequestStatus.Draft, LeaveRequestStatus.Rejected, false)]
    [InlineData(LeaveRequestStatus.Draft, LeaveRequestStatus.Withdrawn, false)]
    [InlineData(LeaveRequestStatus.Draft, LeaveRequestStatus.Expired, false)]
    [InlineData(LeaveRequestStatus.Submitted, LeaveRequestStatus.PendingApproval, true)]
    [InlineData(LeaveRequestStatus.Submitted, LeaveRequestStatus.Approved, true)]
    [InlineData(LeaveRequestStatus.Submitted, LeaveRequestStatus.Withdrawn, true)]
    [InlineData(LeaveRequestStatus.Submitted, LeaveRequestStatus.Expired, true)]
    [InlineData(LeaveRequestStatus.Submitted, LeaveRequestStatus.Draft, false)]
    [InlineData(LeaveRequestStatus.Submitted, LeaveRequestStatus.Rejected, false)]
    [InlineData(LeaveRequestStatus.PendingApproval, LeaveRequestStatus.Approved, true)]
    [InlineData(LeaveRequestStatus.PendingApproval, LeaveRequestStatus.Rejected, true)]
    [InlineData(LeaveRequestStatus.PendingApproval, LeaveRequestStatus.Expired, true)]
    [InlineData(LeaveRequestStatus.PendingApproval, LeaveRequestStatus.Cancelled, false)]
    [InlineData(LeaveRequestStatus.PendingApproval, LeaveRequestStatus.Withdrawn, false)]
    [InlineData(LeaveRequestStatus.Approved, LeaveRequestStatus.Cancelled, true)]
    [InlineData(LeaveRequestStatus.Approved, LeaveRequestStatus.Rejected, false)]
    [InlineData(LeaveRequestStatus.Approved, LeaveRequestStatus.Withdrawn, false)]
    [InlineData(LeaveRequestStatus.Approved, LeaveRequestStatus.Draft, false)]
    [InlineData(LeaveRequestStatus.Rejected, LeaveRequestStatus.Approved, false)]
    [InlineData(LeaveRequestStatus.Rejected, LeaveRequestStatus.Submitted, false)]
    [InlineData(LeaveRequestStatus.Cancelled, LeaveRequestStatus.Approved, false)]
    [InlineData(LeaveRequestStatus.Withdrawn, LeaveRequestStatus.Submitted, false)]
    [InlineData(LeaveRequestStatus.Expired, LeaveRequestStatus.Approved, false)]
    public void StateMachine_CanTransition_MatchesExpected(
        LeaveRequestStatus from, LeaveRequestStatus to, bool expected)
    {
        LeaveStateMachine.CanTransition(from, to).Should().Be(expected,
            $"{from} → {to} should be {(expected ? "allowed" : "illegal")}");
    }

    // ── Guard throws on all terminal states ──────────────────────────────

    [Theory]
    [InlineData(LeaveRequestStatus.Rejected)]
    [InlineData(LeaveRequestStatus.Cancelled)]
    [InlineData(LeaveRequestStatus.Withdrawn)]
    [InlineData(LeaveRequestStatus.Expired)]
    public void Guard_FromTerminalState_AlwaysThrows(LeaveRequestStatus terminal)
    {
        foreach (var target in Enum.GetValues<LeaveRequestStatus>())
        {
            var from = terminal;
            var to = target;
            var act = () => LeaveStateMachine.Guard(from, to);
            act.Should().Throw<InvalidOperationException>($"{from} is terminal, no transitions allowed");
        }
    }
}
