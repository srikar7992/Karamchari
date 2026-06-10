// -----------------------------------------------------------------------
// <copyright file="RF9_ContractTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Tests;

using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Domain.Leaves.Events;
using Xunit;

/// <summary>
/// RF#9 — Contract Tests: event shape verification.
/// Ensures domain events carry the data notification/payroll/attendance consumers expect.
/// Without these tests, refactoring silently breaks integrations.
/// Full consumer contract test (MassTransit InMemory harness) goes in integration test project.
/// </summary>
public sealed class RF9_ContractTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    // ── LeaveRequestCreated ───────────────────────────────────────────────

    [Fact]
    public void LeaveRequestCreated_Shape_HasRequiredFields()
    {
        var requestId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        var evt = new LeaveRequestCreated(requestId, "t1", employeeId,
            Today, Today.AddDays(4), 5.0);

        evt.RequestId.Should().Be(requestId, "downstream routes by request ID");
        evt.TenantId.Should().Be("t1", "consumers need tenant for DB routing");
        evt.EmployeeId.Should().Be(employeeId, "notification module routes by employee");
        evt.StartDate.Should().Be(Today);
        evt.EndDate.Should().Be(Today.AddDays(4));
        evt.TotalDays.Should().Be(5.0, "payroll needs actual day count");
        evt.EventId.Should().NotBe(Guid.Empty, "dedup key for ProcessedEventLog");
        evt.OccurredOnUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ── LeaveRequestSubmitted ─────────────────────────────────────────────

    [Fact]
    public void LeaveRequestSubmitted_Shape_HasRequiredFields()
    {
        var evt = new LeaveRequestSubmitted(Guid.NewGuid(), "t1", Guid.NewGuid(),
            Today, Today.AddDays(2), 3.0);

        evt.RequestId.Should().NotBe(Guid.Empty);
        evt.TenantId.Should().NotBeEmpty();
        evt.EmployeeId.Should().NotBe(Guid.Empty);
        evt.TotalDays.Should().Be(3.0);
    }

    // ── LeaveRequestApproved ──────────────────────────────────────────────

    [Fact]
    public void LeaveRequestApproved_Shape_HasRequiredFields()
    {
        var requestId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        var evt = new LeaveRequestApproved(requestId, "t1", employeeId,
            Today, Today.AddDays(4), 5.0);

        evt.RequestId.Should().Be(requestId);
        evt.EmployeeId.Should().Be(employeeId, "payroll consumer correlates by employee");
        evt.TotalDays.Should().Be(5.0, "balance consumer debits this exact amount");
    }

    // ── LeaveRequestRejected ──────────────────────────────────────────────

    [Fact]
    public void LeaveRequestRejected_Shape_HasRequiredFields()
    {
        var evt = new LeaveRequestRejected(Guid.NewGuid(), "t1", Guid.NewGuid());
        evt.RequestId.Should().NotBe(Guid.Empty);
        evt.EmployeeId.Should().NotBe(Guid.Empty, "notification consumer needs employee to notify");
    }

    // ── LeaveRequestCancelled ─────────────────────────────────────────────

    [Fact]
    public void LeaveRequestCancelled_WasApproved_True_MeansRestoreBalance()
    {
        var evt = new LeaveRequestCancelled(Guid.NewGuid(), "t1", Guid.NewGuid(), true);
        evt.WasApproved.Should().BeTrue("balance consumer restores days when WasApproved=true");
    }

    [Fact]
    public void LeaveRequestCancelled_WasApproved_False_NoBalanceRestore()
    {
        var evt = new LeaveRequestCancelled(Guid.NewGuid(), "t1", Guid.NewGuid(), false);
        evt.WasApproved.Should().BeFalse("draft cancellation: no balance was debited, no restore needed");
    }

    // ── Events raised via LeaveRequest state transitions ──────────────────

    [Fact]
    public void LeaveRequest_Create_RaisesLeaveRequestCreated()
    {
        var employeeId = Guid.NewGuid();
        var r = LeaveRequest.Create(employeeId, Guid.NewGuid(), Today, Today.AddDays(4), 5);

        r.DomainEvents.Should().ContainSingle(e => e is LeaveRequestCreated,
            "Create must publish LeaveRequestCreated for downstream consumers");

        var evt = (LeaveRequestCreated)r.DomainEvents.Single();
        evt.EmployeeId.Should().Be(employeeId, "event must carry correct employeeId");
        evt.TotalDays.Should().Be(5.0);
    }

    [Fact]
    public void LeaveRequest_Submit_RaisesLeaveRequestSubmitted()
    {
        var r = LeaveRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Today, Today.AddDays(4), 5);
        r.ClearDomainEvents();
        r.Submit();

        r.DomainEvents.Should().ContainSingle(e => e is LeaveRequestSubmitted,
            "Submit must raise LeaveRequestSubmitted for SLA tracking");
    }

    [Fact]
    public void LeaveRequest_FinalizeApproved_RaisesLeaveRequestApproved()
    {
        var r = LeaveRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Today, Today.AddDays(4), 5);
        r.Submit(); r.StartApproval();
        r.ClearDomainEvents();

        r.FinalizeApproved();

        r.DomainEvents.Should().ContainSingle(e => e is LeaveRequestApproved,
            "FinalizeApproved must raise event for balance deduction + payroll");
    }

    [Fact]
    public void LeaveRequest_Cancel_RaisesLeaveRequestCancelled_WithWasApprovedTrue()
    {
        var r = LeaveRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Today, Today.AddDays(4), 5);
        r.Submit(); r.StartApproval(); r.FinalizeApproved();
        r.ClearDomainEvents();

        r.Cancel();

        var evt = r.DomainEvents.OfType<LeaveRequestCancelled>().Single();
        evt.WasApproved.Should().BeTrue("cancelling an approved request triggers balance restore");
    }

    [Fact]
    public void LeaveRequest_Cancel_FromDraft_WasApprovedFalse()
    {
        var r = LeaveRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Today, Today.AddDays(4), 5);
        r.ClearDomainEvents();
        r.Cancel();

        var evt = r.DomainEvents.OfType<LeaveRequestCancelled>().Single();
        evt.WasApproved.Should().BeFalse("draft cancellation has no balance impact");
    }

    // ── Event dedup: each event has unique EventId ────────────────────────

    [Fact]
    public void DomainEvents_EachHasUniqueEventId()
    {
        var r1 = new LeaveRequestCreated(Guid.NewGuid(), "t1", Guid.NewGuid(), Today, Today, 1);
        var r2 = new LeaveRequestCreated(Guid.NewGuid(), "t1", Guid.NewGuid(), Today, Today, 1);

        r1.EventId.Should().NotBe(r2.EventId, "ProcessedEventLog dedup requires unique EventId per event");
    }
}
