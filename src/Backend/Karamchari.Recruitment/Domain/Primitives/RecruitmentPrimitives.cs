namespace Karamchari.Recruitment.Domain.Primitives;

/// <summary>
/// Value object representing a compensation range.
/// Enforces Min &lt;= Max.
/// </summary>
public sealed record CompensationRange
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal Min { get; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal Max { get; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Currency { get; }

    private CompensationRange(decimal min, decimal max, string currency)
    {
        Min = min;
        Max = max;
        Currency = currency;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static CompensationRange Create(decimal min, decimal max, string currency = "USD")
    {
        if (min < 0 || max < 0)
            throw new ArgumentException("Compensation cannot be negative.");
        if (min > max)
            throw new ArgumentException("Min compensation cannot exceed Max.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.");

        return new CompensationRange(min, max, currency.ToUpperInvariant());
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsWithinRange(decimal amount) => amount >= Min && amount <= Max;
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum HiringPriority
{
    /// <inheritdoc/>
    Standard,
    /// <inheritdoc/>
    High,
    /// <inheritdoc/>
    Critical,
    /// <inheritdoc/>
    Executive
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum RequisitionStatus
{
    /// <inheritdoc/>
    Draft,
    /// <inheritdoc/>
    PendingApproval,
    /// <inheritdoc/>
    Open,
    /// <inheritdoc/>
    OnHold,
    /// <inheritdoc/>
    Filled,
    /// <inheritdoc/>
    Cancelled
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum CandidateSource
{
    /// <inheritdoc/>
    DirectApply,
    /// <inheritdoc/>
    InternalReferral,
    /// <inheritdoc/>
    Agency,
    /// <inheritdoc/>
    Sourced,
    /// <inheritdoc/>
    InternalMobility
}
