using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Karamchari.Workflow.Services;

/// <summary>
/// Compiles and caches strongly-typed condition expressions against <see cref="ApprovalContext"/>.
/// Cache is keyed by <c>(definitionId, SHA256(conditionJson))</c> so:
/// (a) stale in-process delegates on other instances auto-recompile when JSON changes,
/// (b) no distributed cache or explicit Invalidate() call is needed across pods.
/// CompilerVersion is stored in the snapshot so behavior changes are detectable in audit.
/// </summary>
public sealed class WorkflowConditionCompiler
{
    // Key = (DefinitionId, JsonHash) — deterministic across all instances
    private static readonly ConcurrentDictionary<(Guid, string), Func<ApprovalContext, bool>> _cache = new();

    /// <summary>Monotonic version of the compiler's evaluation semantics. Embed in snapshots.</summary>
    public const int CompilerVersion = 1;

    private static readonly HashSet<string> _allowedFields = typeof(ApprovalContext)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Select(p => p.Name)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Compiles (and caches by definitionId + JSON hash) the condition expression.
    /// Throws <see cref="WorkflowConditionException"/> on invalid JSON — call at definition-save time.
    /// </summary>
    public static Func<ApprovalContext, bool> Compile(Guid definitionId, string conditionJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conditionJson);
        var hash = ComputeHash(conditionJson);
        return _cache.GetOrAdd((definitionId, hash), _ => Build(conditionJson));
    }

    private static string ComputeHash(string input)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16]; // first 8 bytes is enough for a cache key discriminator
    }

    private static Func<ApprovalContext, bool> Build(string json)
    {
        var param = Expression.Parameter(typeof(ApprovalContext), "ctx");
        var body = ParseNode(System.Text.Json.JsonDocument.Parse(json).RootElement, param);
        return Expression.Lambda<Func<ApprovalContext, bool>>(body, param).Compile();
    }

    private static Expression ParseNode(System.Text.Json.JsonElement el, ParameterExpression param)
    {
        if (el.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            return el.EnumerateArray()
                     .Select(child => ParseNode(child, param))
                     .Aggregate(Expression.AndAlso);
        }

        var op = el.GetProperty("op").GetString()
            ?? throw new WorkflowConditionException("Missing 'op' field.");

        if (op.Equals("And", StringComparison.OrdinalIgnoreCase))
            return el.GetProperty("conditions").EnumerateArray()
                     .Select(c => ParseNode(c, param)).Aggregate(Expression.AndAlso);

        if (op.Equals("Or", StringComparison.OrdinalIgnoreCase))
            return el.GetProperty("conditions").EnumerateArray()
                     .Select(c => ParseNode(c, param)).Aggregate(Expression.OrElse);

        var fieldName = el.GetProperty("field").GetString()
            ?? throw new WorkflowConditionException("Missing 'field'.");

        if (!_allowedFields.Contains(fieldName))
            throw new WorkflowConditionException($"Unknown field '{fieldName}'.");

        var propInfo = typeof(ApprovalContext).GetProperty(fieldName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)!;

        var left = Expression.Property(param, propInfo);
        var valueEl = el.GetProperty("value");

        // Null-safe guard: if string field is empty/null at runtime → return false for comparisons
        var right = BuildConstant(propInfo.PropertyType, valueEl);

        Expression comparison = op.ToUpperInvariant() switch
        {
            "EQ"  or "==" => Expression.Equal(left, right),
            "NEQ" or "!=" => Expression.NotEqual(left, right),
            "GT"  or ">"  => Expression.GreaterThan(left, right),
            "GTE" or ">=" => Expression.GreaterThanOrEqual(left, right),
            "LT"  or "<"  => Expression.LessThan(left, right),
            "LTE" or "<=" => Expression.LessThanOrEqual(left, right),
            _ => throw new WorkflowConditionException($"Unknown operator '{op}'.")
        };

        // Wrap string comparisons with a null guard
        if (propInfo.PropertyType == typeof(string))
        {
            var notNullCheck = Expression.NotEqual(left, Expression.Constant(null, typeof(string)));
            return Expression.AndAlso(notNullCheck, comparison);
        }

        return comparison;
    }

    private static ConstantExpression BuildConstant(Type targetType, System.Text.Json.JsonElement valueEl)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // Coerce numeric types: JSON integers → decimal (covers Amount comparisons)
        object value = underlying switch
        {
            _ when underlying == typeof(decimal) =>
                valueEl.ValueKind == System.Text.Json.JsonValueKind.Number
                    ? valueEl.GetDecimal()
                    : throw new WorkflowConditionException("Expected numeric value for decimal field."),
            _ when underlying == typeof(int) => valueEl.GetInt32(),
            _ when underlying == typeof(bool) => valueEl.GetBoolean(),
            _ when underlying == typeof(string) => valueEl.GetString() ?? string.Empty,
            _ => throw new WorkflowConditionException($"Unsupported field type '{underlying.Name}'.")
        };

        return Expression.Constant(value, targetType);
    }
}

public sealed class WorkflowConditionException : Exception
{
    public WorkflowConditionException(string message) : base(message) { }
}

