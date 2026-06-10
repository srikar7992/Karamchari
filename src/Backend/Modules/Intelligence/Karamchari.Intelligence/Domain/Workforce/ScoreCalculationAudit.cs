// -----------------------------------------------------------------------
// <copyright file="ScoreCalculationAudit.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;

namespace Karamchari.Intelligence.Domain.Workforce;

/// <summary>
/// Append-only audit record for every score recalculation.
/// Stores component contributions, ranked root causes, and raw signal values
/// so managers can challenge or audit any historical score.
///
/// Never updated — one row per recalculation event.
/// Maps to Intel_ScoreAudits.
/// </summary>
public sealed class ScoreCalculationAudit : ITenantOwned
{
    public Guid Id { get; private set; }

    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    public Guid EmployeeId { get; private set; }

    /// <summary>"Burnout" or "Attrition".</summary>
    public string ScoreType { get; private set; } = string.Empty;

    public decimal Score { get; private set; }

    public WorkforceRiskLevel RiskLevel { get; private set; }

    /// <summary>JSON-serialized Dictionary&lt;string, decimal&gt; — component name → contribution points.</summary>
    public string ComponentScoresJson { get; private set; } = string.Empty;

    /// <summary>JSON-serialized list of component names in descending contribution order.</summary>
    public string RootCauseRankingJson { get; private set; } = string.Empty;

    /// <summary>JSON-serialized key/value snapshot of raw signal inputs used for this calculation.</summary>
    public string SignalValuesJson { get; private set; } = string.Empty;

    public DateTime CalculatedAt { get; private set; }

    private ScoreCalculationAudit() { }

    public static ScoreCalculationAudit Record(
        string tenantId,
        Guid employeeId,
        string scoreType,
        decimal score,
        WorkforceRiskLevel riskLevel,
        string componentScoresJson,
        string rootCauseRankingJson,
        string signalValuesJson)
    {
        return new ScoreCalculationAudit
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            ScoreType = scoreType,
            Score = score,
            RiskLevel = riskLevel,
            ComponentScoresJson = componentScoresJson,
            RootCauseRankingJson = rootCauseRankingJson,
            SignalValuesJson = signalValuesJson,
            CalculatedAt = DateTime.UtcNow
        };
    }
}
