using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.KPIs;

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

    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Machine-readable code. Unique per tenant. e.g. "UTIL_PCT", "ATTENDANCE_PCT".</summary>
    public string Code { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public KPIType Type { get; private set; }
    public KPIAggregation Aggregation { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public KPIPolarity Polarity { get; private set; }
    public KPIPeriodicity Periodicity { get; private set; }
    public KPIThreshold Threshold { get; private set; } = null!;

    /// <summary>
    /// DSL expression for Formula-type KPIs.
    /// TEMPORARY: evaluated via Roslyn scripting with allowlist.
    /// See ADR-0006 for planned migration to AST-based declarative DSL.
    /// </summary>
    public string? FormulaExpression { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

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

    public void Deactivate() => IsActive = false;

    public void UpdateThreshold(KPIThreshold threshold)
    {
        ArgumentNullException.ThrowIfNull(threshold);
        Threshold = threshold;
    }

    public void UpdateFormula(string expression)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        if (Type != KPIType.Formula)
            throw new InvalidOperationException("Can only update formula on Formula-type KPIs.");
        FormulaExpression = expression.Trim();
    }
}
