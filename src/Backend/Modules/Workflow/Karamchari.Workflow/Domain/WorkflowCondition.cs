using System.Globalization;

namespace Karamchari.Workflow.Domain;

/// <summary>
/// Operator used in a workflow routing condition.
/// </summary>
public enum ConditionOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Contains,
    In,
}

/// <summary>
/// Boolean combinator for a <see cref="ConditionGroup"/>.
/// </summary>
public enum LogicalOperator
{
    And,
    Or,
}

/// <summary>
/// A single condition that must hold for a <see cref="WorkflowDefinition"/> to match a routing request.
/// Field names are case-insensitive strings that callers place in the routing context dictionary.
/// Values are always stored as strings and compared after type coercion.
/// </summary>
public sealed record WorkflowCondition(string Field, ConditionOperator Operator, string Value)
{
    /// <summary>
    /// Evaluates this condition against the supplied routing context.
    /// Returns <c>true</c> when the condition is satisfied (or when the field is absent and the
    /// operator is <see cref="ConditionOperator.NotEquals"/>).
    /// </summary>
    public bool Evaluate(IReadOnlyDictionary<string, object> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.TryGetValue(Field, out var raw))
        {
            // Missing field: only NotEquals is vacuously true.
            return Operator == ConditionOperator.NotEquals;
        }

        var contextValue = Convert.ToString(raw, CultureInfo.InvariantCulture) ?? string.Empty;

        return Operator switch
        {
            ConditionOperator.Equals => string.Equals(contextValue, Value, StringComparison.OrdinalIgnoreCase),
            ConditionOperator.NotEquals => !string.Equals(contextValue, Value, StringComparison.OrdinalIgnoreCase),
            ConditionOperator.Contains => contextValue.Contains(Value, StringComparison.OrdinalIgnoreCase),
            ConditionOperator.In => Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(v => string.Equals(contextValue, v, StringComparison.OrdinalIgnoreCase)),
            ConditionOperator.GreaterThan => CompareNumeric(contextValue, Value) > 0,
            ConditionOperator.GreaterThanOrEqual => CompareNumeric(contextValue, Value) >= 0,
            ConditionOperator.LessThan => CompareNumeric(contextValue, Value) < 0,
            ConditionOperator.LessThanOrEqual => CompareNumeric(contextValue, Value) <= 0,
            _ => false,
        };
    }

    private static int CompareNumeric(string left, string right)
    {
        if (decimal.TryParse(left, NumberStyles.Any, CultureInfo.InvariantCulture, out var l)
            && decimal.TryParse(right, NumberStyles.Any, CultureInfo.InvariantCulture, out var r))
        {
            return l.CompareTo(r);
        }

        // Fall back to ordinal string comparison so non-numeric fields don't throw.
        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// A composable boolean expression group supporting AND/OR/NOT semantics and arbitrary nesting.
/// Enables enterprise routing rules such as:
/// <code>
/// (Amount > 10000 OR Department = Finance)
/// AND (Tenure &lt; 90 OR PerformanceRating &lt; 3)
/// AND NOT (Executive = true)
/// </code>
/// All children (leaf conditions and nested groups) are combined with <see cref="Operator"/> semantics.
/// When <see cref="Negate"/> is true the group result is inverted.
/// An empty group (no conditions, no nested groups) always evaluates to <c>true</c>.
/// </summary>
public sealed record ConditionGroup
{
    /// <summary>How to combine child conditions and sub-groups.</summary>
    public LogicalOperator Operator { get; init; } = LogicalOperator.And;

    /// <summary>When true, the evaluated result is inverted (NOT).</summary>
    public bool Negate { get; init; }

    /// <summary>Leaf conditions evaluated by this group.</summary>
    public IReadOnlyList<WorkflowCondition> Conditions { get; init; } = [];

    /// <summary>Nested sub-groups for compound boolean expressions.</summary>
    public IReadOnlyList<ConditionGroup> Groups { get; init; } = [];

    /// <summary>Singleton empty group — always evaluates to true.</summary>
    public static ConditionGroup Empty { get; } = new();

    /// <summary>
    /// Wraps a flat list of conditions in an AND group.
    /// Preserves the Phase 12 behavior for existing <c>ConditionsJson</c> data.
    /// </summary>
    public static ConditionGroup FromFlatList(IReadOnlyList<WorkflowCondition> conditions) =>
        new() { Operator = LogicalOperator.And, Conditions = conditions };

    /// <summary>
    /// Evaluates the expression against the supplied routing context.
    /// </summary>
    public bool Evaluate(IReadOnlyDictionary<string, object> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        bool result;
        bool hasChildren = Conditions.Count > 0 || Groups.Count > 0;

        if (!hasChildren)
        {
            result = true; // empty group always matches
        }
        else if (Operator == LogicalOperator.And)
        {
            result = Conditions.All(c => c.Evaluate(context))
                  && Groups.All(g => g.Evaluate(context));
        }
        else
        {
            // Or — short-circuit on first true child.
            result = Conditions.Any(c => c.Evaluate(context))
                  || Groups.Any(g => g.Evaluate(context));
        }

        return Negate ? !result : result;
    }
}
