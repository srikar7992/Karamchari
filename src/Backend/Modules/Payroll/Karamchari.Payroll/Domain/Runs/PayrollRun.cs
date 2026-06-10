// -----------------------------------------------------------------------
// <copyright file="PayrollRun.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Payroll.Domain.Runs.Events;

namespace Karamchari.Payroll.Domain.Runs;

/// <summary>
/// Domain aggregate for a payroll run. The central financial record for a pay period.
/// Immutable once Locked — use PayrollRunVersion for reruns.
/// </summary>
public sealed class PayrollRun : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid PayPeriodId { get; private set; }
    public string PeriodName { get; private set; } = string.Empty;
    public PayrollRunStatus Status { get; private set; }
    public int Version { get; private set; }
    public Guid? ParentRunId { get; private set; }
    public int EmployeeCount { get; private set; }
    public decimal TotalGross { get; private set; }
    public decimal TotalDeductions { get; private set; }
    public decimal TotalNet { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public string? LockedBy { get; private set; }
    public DateTimeOffset? LockedAtUtc { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private PayrollRun() { }

    public static PayrollRun Create(string tenantId, Guid payPeriodId, string periodName)
    {
        var run = new PayrollRun
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PayPeriodId = payPeriodId,
            PeriodName = periodName,
            Status = PayrollRunStatus.Draft,
            Version = 1,
            StartedAtUtc = DateTimeOffset.UtcNow
        };

        run.RaiseDomainEvent(new PayrollRunCreatedEvent(run.Id, tenantId, payPeriodId));
        return run;
    }

    public static PayrollRun CreateRerun(string tenantId, Guid payPeriodId, string periodName, Guid parentRunId, int parentVersion)
    {
        var run = new PayrollRun
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PayPeriodId = payPeriodId,
            PeriodName = periodName,
            Status = PayrollRunStatus.Draft,
            Version = parentVersion + 1,
            ParentRunId = parentRunId,
            StartedAtUtc = DateTimeOffset.UtcNow
        };

        run.RaiseDomainEvent(new PayrollRunCreatedEvent(run.Id, tenantId, payPeriodId));
        return run;
    }

    public void StartCalculating()
    {
        EnsureStatus(PayrollRunStatus.Draft);
        Status = PayrollRunStatus.Calculating;
    }

    public void MarkCalculated(int employeeCount, decimal totalGross, decimal totalDeductions, decimal totalNet)
    {
        EnsureStatus(PayrollRunStatus.Calculating);
        EmployeeCount = employeeCount;
        TotalGross = totalGross;
        TotalDeductions = totalDeductions;
        TotalNet = totalNet;
        Status = PayrollRunStatus.PendingApproval;
        RaiseDomainEvent(new PayrollCalculatedEvent(Id, TenantId, employeeCount, totalGross));
    }

    public void Approve(string approvedBy)
    {
        EnsureStatus(PayrollRunStatus.PendingApproval);
        Status = PayrollRunStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new PayrollApprovedEvent(Id, TenantId, approvedBy));
    }

    public void Publish()
    {
        EnsureStatus(PayrollRunStatus.Approved);
        Status = PayrollRunStatus.Published;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new PayrollPublishedEvent(Id, TenantId));
    }

    public void Lock(string lockedBy)
    {
        if (Status == PayrollRunStatus.Locked)
            return;

        if (Status != PayrollRunStatus.Published)
            throw new InvalidOperationException($"Cannot lock run in status {Status}.");

        Status = PayrollRunStatus.Locked;
        LockedBy = lockedBy;
        LockedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new PayrollLockedEvent(Id, TenantId, lockedBy));
    }

    private void EnsureStatus(PayrollRunStatus required)
    {
        if (Status != required)
            throw new InvalidOperationException($"Cannot transition from {Status}; expected {required}.");
    }
}
