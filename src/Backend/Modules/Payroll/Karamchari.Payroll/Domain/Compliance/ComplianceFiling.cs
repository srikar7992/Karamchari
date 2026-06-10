// -----------------------------------------------------------------------
// <copyright file="ComplianceFiling.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Payroll.Domain.Compliance;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum FilingStatus
{
    Pending,
    Filed,
    Failed
}

/// <summary>
/// Tracks the actual filing state of a compliance return on government portals.
/// </summary>
public sealed class ComplianceFiling : Entity<Guid>, ITenantOwned
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid PayrollRunId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public ComplianceType Type { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public FilingStatus Status { get; private set; }
    public DateTime? FiledAtUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string FiledBy { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string ReferenceNumber { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Remarks { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTime DueDate { get; private set; }

    private ComplianceFiling() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static ComplianceFiling CreatePending(
        string tenantId,
        Guid payrollRunId,
        ComplianceType type,
        DateTime dueDate)
    {
        return new ComplianceFiling
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PayrollRunId = payrollRunId,
            Type = type,
            Status = FilingStatus.Pending,
            DueDate = dueDate
        };
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void MarkAsFiled(string referenceNumber, string filedBy, string remarks = "")
    {
        Status = FilingStatus.Filed;
        FiledAtUtc = DateTime.UtcNow;
        ReferenceNumber = referenceNumber;
        FiledBy = filedBy;
        Remarks = remarks;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void MarkAsFailed(string remarks)
    {
        Status = FilingStatus.Failed;
        Remarks = remarks;
    }
}
