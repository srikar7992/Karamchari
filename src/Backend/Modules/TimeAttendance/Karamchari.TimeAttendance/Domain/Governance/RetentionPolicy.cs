// -----------------------------------------------------------------------
// <copyright file="RetentionPolicy.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Governance;

public enum RetentionDataClass
{
    LeaveRequests = 1,
    LeaveBalance = 2,
    AuditTimeline = 3,
    AttendanceRecords = 4,
    PayrollAdvisories = 5,
    InvestigationCases = 6,
    StatutoryRegisters = 7,
    NotificationOutbox = 8,
}

public enum PurgeJobStatus
{
    Pending = 1,
    Running = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5,
}

/// <summary>
/// Data retention configuration for GDPR, DPDP India, and enterprise security reviews.
/// Drives PurgeJob scheduling — one policy per data class.
/// </summary>
public sealed class RetentionPolicy : AggregateRoot<Guid>, ITenantOwned
{
    private RetentionPolicy() { TenantId = string.Empty; }

    public string TenantId { get; private set; }
    public RetentionDataClass DataClass { get; private set; }
    public string DataClassName { get; private set; } = string.Empty;
    /// <summary>Retention duration in years. After this, records are eligible for purge.</summary>
    public int RetentionYears { get; private set; }
    /// <summary>Legal basis for retention (GDPR Article 6 clause, DPDP Section, etc.).</summary>
    public string LegalBasis { get; private set; } = string.Empty;
    public bool AnonymizeInsteadOfDelete { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset LastReviewedAt { get; private set; }
    public string LastReviewedBy { get; private set; } = string.Empty;

    public static RetentionPolicy Create(
        string tenantId,
        RetentionDataClass dataClass,
        string dataClassName,
        int retentionYears,
        string legalBasis,
        bool anonymizeInsteadOfDelete,
        string reviewedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataClassName);
        ArgumentException.ThrowIfNullOrWhiteSpace(legalBasis);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retentionYears);

        return new RetentionPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DataClass = dataClass,
            DataClassName = dataClassName,
            RetentionYears = retentionYears,
            LegalBasis = legalBasis,
            AnonymizeInsteadOfDelete = anonymizeInsteadOfDelete,
            IsActive = true,
            LastReviewedAt = DateTimeOffset.UtcNow,
            LastReviewedBy = reviewedBy,
        };
    }

    public void Review(int retentionYears, string legalBasis, string reviewedBy)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retentionYears);
        RetentionYears = retentionYears;
        LegalBasis = legalBasis;
        LastReviewedAt = DateTimeOffset.UtcNow;
        LastReviewedBy = reviewedBy;
    }

    public DateOnly CutoffDate(DateOnly asOf) =>
        asOf.AddYears(-RetentionYears);
}

/// <summary>
/// Scheduled purge execution record. Append-only audit trail of every purge run.
/// Executor reads RetentionPolicy cutoff, deletes/anonymizes records, writes result here.
/// </summary>
public sealed class PurgeJob : ITenantOwned
{
    private PurgeJob() { TenantId = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public Guid RetentionPolicyId { get; private set; }
    public RetentionDataClass DataClass { get; private set; }
    public DateOnly CutoffDate { get; private set; }
    public PurgeJobStatus Status { get; private set; }
    public string TriggeredBy { get; private set; } = string.Empty;
    public DateTimeOffset ScheduledAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int RecordsAffected { get; private set; }
    public string? FailureReason { get; private set; }

    public static PurgeJob Schedule(
        string tenantId,
        Guid retentionPolicyId,
        RetentionDataClass dataClass,
        DateOnly cutoffDate,
        string triggeredBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(triggeredBy);

        return new PurgeJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RetentionPolicyId = retentionPolicyId,
            DataClass = dataClass,
            CutoffDate = cutoffDate,
            Status = PurgeJobStatus.Pending,
            TriggeredBy = triggeredBy,
            ScheduledAt = DateTimeOffset.UtcNow,
        };
    }

    public void Start()
    {
        if (Status != PurgeJobStatus.Pending)
            throw new InvalidOperationException("Purge job not in pending state.");
        Status = PurgeJobStatus.Running;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void Complete(int recordsAffected)
    {
        Status = PurgeJobStatus.Completed;
        RecordsAffected = recordsAffected;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Fail(string reason)
    {
        Status = PurgeJobStatus.Failed;
        FailureReason = reason;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
