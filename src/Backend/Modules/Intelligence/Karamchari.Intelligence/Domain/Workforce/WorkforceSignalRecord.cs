// -----------------------------------------------------------------------
// <copyright file="WorkforceSignalRecord.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;

namespace Karamchari.Intelligence.Domain.Workforce;

/// <summary>
/// Persisted raw signal measurement for a single employee.
/// One record per (employee, signal type, signal date) — upserted on recalculation.
/// The scoring engine reads the latest record per type to build its input.
/// </summary>
public sealed class WorkforceSignalRecord : ITenantOwned
{
    /// <summary>Surrogate key.</summary>
    public Guid Id { get; private set; }

    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Employee this signal belongs to.</summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>Signal type — determines how Value is interpreted.</summary>
    public WorkforceSignalType SignalType { get; private set; }

    /// <summary>
    /// Numeric value whose semantics depend on SignalType.
    /// e.g. ConsecutiveWorkDays=7, OvertimeHours28d=42.5, HighIntensityShiftRatio=0.6
    /// </summary>
    public decimal Value { get; private set; }

    /// <summary>Calendar date the signal measurement covers.</summary>
    public DateOnly SignalDate { get; private set; }

    /// <summary>When this record was written (for stale-data detection).</summary>
    public DateTime GeneratedAt { get; private set; }

    private WorkforceSignalRecord() { }

    /// <summary>Creates a new signal record.</summary>
    public static WorkforceSignalRecord Record(
        string tenantId,
        Guid employeeId,
        WorkforceSignalType signalType,
        decimal value,
        DateOnly signalDate)
    {
        return new WorkforceSignalRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            SignalType = signalType,
            Value = value,
            SignalDate = signalDate,
            GeneratedAt = DateTime.UtcNow
        };
    }
}
