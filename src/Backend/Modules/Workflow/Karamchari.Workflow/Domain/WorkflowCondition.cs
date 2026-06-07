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
