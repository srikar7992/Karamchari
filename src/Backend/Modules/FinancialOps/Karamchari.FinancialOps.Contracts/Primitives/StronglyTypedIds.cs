namespace Karamchari.FinancialOps.Contracts.Primitives;

/// <summary>
/// A strongly-typed identifier for an employee.
/// </summary>
/// <param name="Value">The underlying Guid value.</param>
public record EmployeeId(Guid Value)
{
    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}

/// <summary>
/// A strongly-typed identifier for a tenant.
/// </summary>
/// <param name="Value">The underlying string value.</param>
public record TenantId(string Value)
{
    /// <inheritdoc/>
    public override string ToString() => Value;
}

/// <summary>
/// A strongly-typed identifier for a workforce financial operational period.
/// </summary>
/// <param name="Value">The underlying Guid value.</param>
public record FinancialOperationalPeriodId(Guid Value)
{
    /// <summary>Creates a new unique identifier.</summary>
    public static FinancialOperationalPeriodId New() => new(Guid.NewGuid());
    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}

/// <summary>
/// A strongly-typed identifier for a financial entry in the operational ledger.
/// </summary>
/// <param name="Value">The underlying Guid value.</param>
public record WorkforceFinancialEntryId(Guid Value)
{
    /// <summary>Creates a new unique identifier.</summary>
    public static WorkforceFinancialEntryId New() => new(Guid.NewGuid());
    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Represents a currency code (e.g., "INR", "USD").
/// </summary>
/// <param name="Value">The underlying string value.</param>
public record CurrencyCode(string Value)
{
    /// <summary>Indian Rupee.</summary>
    public static CurrencyCode INR => new("INR");
    /// <summary>United States Dollar.</summary>
    public static CurrencyCode USD => new("USD");
    /// <inheritdoc/>
    public override string ToString() => Value.ToUpperInvariant();
}
