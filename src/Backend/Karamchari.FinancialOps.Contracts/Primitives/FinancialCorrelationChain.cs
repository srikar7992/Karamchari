namespace Karamchari.FinancialOps.Contracts.Primitives;

/// <summary>
/// A deterministic operational correlation identifier for financial operations.
/// </summary>
/// <param name="Value">The underlying Guid value.</param>
public record FinancialCorrelationId(Guid Value)
{
    /// <summary>Creates a new unique identifier.</summary>
    public static FinancialCorrelationId New() => new(Guid.NewGuid());
    /// <summary>Represents an empty identifier.</summary>
    public static FinancialCorrelationId Empty => new(Guid.Empty);
    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}

/// <summary>
/// A chain of correlation identifiers to track the provenance of financial operations
/// across multiple steps (e.g., Claim -> Approval -> Settlement -> Payout).
/// </summary>
/// <param name="RootId">The root identifier of the operation chain.</param>
/// <param name="ParentId">The parent identifier in the chain.</param>
/// <param name="CurrentId">The current identifier for this specific operation.</param>
/// <param name="Depth">The depth of the recursion in this chain (RULE 10).</param>
public record FinancialCorrelationChain(
    FinancialCorrelationId RootId,
    FinancialCorrelationId? ParentId,
    FinancialCorrelationId CurrentId,
    int Depth = 1)
{
    /// <summary>Creates a new correlation chain with a fresh root.</summary>
    /// <returns>A new root correlation chain.</returns>
    public static FinancialCorrelationChain CreateNew()
    {
        var id = FinancialCorrelationId.New();
        return new FinancialCorrelationChain(id, null, id, 1);
    }

    /// <summary>Progresses the chain to the next step, bounding recovery recursion.</summary>
    /// <returns>The next step in the correlation chain.</returns>
    /// <exception cref="InvalidOperationException">Thrown when max recursion depth is exceeded.</exception>
    public FinancialCorrelationChain Next()
    {
        // RULE 10 - Recovery Recursion Must Be Bounded
        if (Depth >= 5)
        {
            throw new InvalidOperationException($"Recovery recursion limit exceeded. Max depth 5 reached for root correlation {RootId.Value}.");
        }

        return new FinancialCorrelationChain(RootId, CurrentId, FinancialCorrelationId.New(), Depth + 1);
    }
}
