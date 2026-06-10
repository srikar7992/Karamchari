// -----------------------------------------------------------------------
// <copyright file="BradfordFactorScore.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Persistence;

using Karamchari.Core.Multitenancy;

/// <summary>
/// Compliance read model: Bradford Factor score per employee.
/// Formula: B = S² × D
///   S = number of separate absence episodes in rolling window
///   D = total days absent in rolling window
///
/// Risk thresholds (common defaults, configurable per tenant):
///   0-44   = Low
///   45-99  = Medium
///   100-399 = High
///   400+   = Critical (typically triggers formal review)
///
/// Updated by attendance reconciliation consumer.
/// </summary>
public sealed class BradfordFactorScore : ITenantOwned
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = string.Empty;
    public Guid EmployeeId { get; init; }

    /// <summary>Rolling window in days. Typically 365 (52 weeks).</summary>
    public int RollingWindowDays { get; init; } = 365;

    /// <summary>S: Number of separate absence episodes (not consecutive = 1 episode).</summary>
    public int AbsenceEpisodes { get; set; }

    /// <summary>D: Total calendar days absent in the rolling window.</summary>
    public decimal TotalAbsenceDays { get; set; }

    /// <summary>Bradford Factor score = S² × D.</summary>
    public decimal Score => AbsenceEpisodes * AbsenceEpisodes * TotalAbsenceDays;

    public BradfordRiskLevel RiskLevel { get; set; }
    public DateOnly WindowStartDate { get; set; }
    public DateOnly WindowEndDate { get; set; }
    public DateTimeOffset LastUpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public static BradfordRiskLevel CalculateRiskLevel(decimal score) => score switch
    {
        < 45 => BradfordRiskLevel.Low,
        < 100 => BradfordRiskLevel.Medium,
        < 400 => BradfordRiskLevel.High,
        _ => BradfordRiskLevel.Critical
    };
}

public enum BradfordRiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
