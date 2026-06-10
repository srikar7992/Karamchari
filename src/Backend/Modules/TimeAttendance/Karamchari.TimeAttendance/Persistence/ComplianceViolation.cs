// -----------------------------------------------------------------------
// <copyright file="ComplianceViolation.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Persistence;

public enum ComplianceViolationType
{
    ConsecutiveWorkingDays = 1,
    LeaveBalanceExposure = 2,
    MaternityEntitlementNotGranted = 3,
    CompOffExpiryRisk = 4,
    MandatoryLeaveUnused = 5,
    NoticePeriodViolation = 6,
}

public enum ComplianceSeverity
{
    Info = 1,
    Warning = 2,
    Critical = 3,
}

public enum ComplianceViolationStatus
{
    Open = 1,
    Acknowledged = 2,
    Resolved = 3,
    Suppressed = 4,
}

/// <summary>
/// Advanced compliance violation detection beyond Bradford Factor.
/// One row per detected violation per employee per detection run.
/// Consumed by compliance dashboard and HR investigation console.
/// </summary>
public sealed class ComplianceViolation : ITenantOwned
{
    private ComplianceViolation() { TenantId = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public ComplianceViolationType ViolationType { get; private set; }
    public ComplianceSeverity Severity { get; private set; }
    public ComplianceViolationStatus Status { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    /// <summary>Machine-readable detail (JSON). E.g. {"consecutiveDays":16,"threshold":14}</summary>
    public string Detail { get; private set; } = string.Empty;
    public Guid? ReferenceEntityId { get; private set; }
    public string? ReferenceEntityType { get; private set; }
    public DateTimeOffset DetectedAt { get; private set; }
    public DateTimeOffset? AcknowledgedAt { get; private set; }
    public string? AcknowledgedBy { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    public static ComplianceViolation Raise(
        string tenantId,
        Guid employeeId,
        ComplianceViolationType violationType,
        ComplianceSeverity severity,
        string summary,
        string detail,
        Guid? referenceEntityId = null,
        string? referenceEntityType = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);

        return new ComplianceViolation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            ViolationType = violationType,
            Severity = severity,
            Status = ComplianceViolationStatus.Open,
            Summary = summary,
            Detail = detail,
            ReferenceEntityId = referenceEntityId,
            ReferenceEntityType = referenceEntityType,
            DetectedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Acknowledge(string by)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(by);
        Status = ComplianceViolationStatus.Acknowledged;
        AcknowledgedBy = by;
        AcknowledgedAt = DateTimeOffset.UtcNow;
    }

    public void Resolve()
    {
        Status = ComplianceViolationStatus.Resolved;
        ResolvedAt = DateTimeOffset.UtcNow;
    }

    public void Suppress() => Status = ComplianceViolationStatus.Suppressed;
}
