// -----------------------------------------------------------------------
// <copyright file="KPIDefinition.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.KPIs;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class KPIDefinition : AggregateRoot<Guid>, ITenantOwned
{
    private KPIDefinition() { /* EF materialization */ }

    private KPIDefinition(
        Guid id,
        string tenantId,
        string code,
        string displayName,
        KPIType type,
        KPIAggregation aggregation,
        string unit,
        KPIPolarity polarity,
        KPIPeriodicity periodicity,
        KPIThreshold threshold,
        string? formulaExpression) : base(id)
    {
        TenantId = tenantId;
        Code = code;
        DisplayName = displayName;
        Type = type;
        Aggregation = aggregation;
        Unit = unit;
        Polarity = polarity;
        Periodicity = periodicity;
        Threshold = threshold;
        FormulaExpression = formulaExpression;
        IsActive = true;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Machine-readable code. Unique per tenant. e.g. "UTIL_PCT", "ATTENDANCE_PCT".</summary>
    public string Code { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public KPIType Type { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public KPIAggregation Aggregation { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Unit { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public KPIPolarity Polarity { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public KPIPeriodicity Periodicity { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public KPIThreshold Threshold { get; private set; } = null!;

    /// <summary>
    /// DSL expression for Formula-type KPIs.
    /// TEMPORARY: evaluated via Roslyn scripting with allowlist.
    /// See ADR-0006 for planned migration to AST-based declarative DSL.
    /// </summary>
    public string? FormulaExpression { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsActive { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static KPIDefinition Create(
        string tenantId,
        string code,
        string displayName,
        KPIType type,
        KPIAggregation aggregation,
        string unit,
        KPIPolarity polarity,
        KPIPeriodicity periodicity,
        KPIThreshold threshold,
        string? formulaExpression = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(unit);
        ArgumentNullException.ThrowIfNull(threshold);

        if (type == KPIType.Formula && string.IsNullOrWhiteSpace(formulaExpression))
            throw new ArgumentException("Formula KPI requires a FormulaExpression.");

        return new KPIDefinition(Guid.NewGuid(), tenantId, code.Trim().ToUpperInvariant(),
            displayName.Trim(), type, aggregation, unit.Trim(), polarity, periodicity,
            threshold, formulaExpression?.Trim());
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void UpdateThreshold(KPIThreshold threshold)
    {
        ArgumentNullException.ThrowIfNull(threshold);
        Threshold = threshold;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void UpdateFormula(string expression)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        if (Type != KPIType.Formula)
            throw new InvalidOperationException("Can only update formula on Formula-type KPIs.");
        FormulaExpression = expression.Trim();
    }
}
