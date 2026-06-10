// -----------------------------------------------------------------------
// <copyright file="ComplianceSnapshot.cs" company="Karamchari">
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
public enum ComplianceType
{
    EpfEcr,
    EsicMonthly,
    TdsForm24Q
}

/// <summary>
/// Persisted snapshot of a generated compliance file for audit and repeatability.
/// </summary>
public sealed class ComplianceSnapshot : Entity<Guid>, ITenantOwned
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
    public string FileName { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] Content { get; private set; } = Array.Empty<byte>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTime GeneratedAtUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string GeneratedBy { get; private set; } = string.Empty;

    private ComplianceSnapshot() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static ComplianceSnapshot Create(
        string tenantId,
        Guid payrollRunId,
        ComplianceType type,
        string fileName,
        byte[] content,
        string generatedBy)
    {
        return new ComplianceSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PayrollRunId = payrollRunId,
            Type = type,
            FileName = fileName,
            Content = content,
            GeneratedAtUtc = DateTime.UtcNow,
            GeneratedBy = generatedBy
        };
    }
}
