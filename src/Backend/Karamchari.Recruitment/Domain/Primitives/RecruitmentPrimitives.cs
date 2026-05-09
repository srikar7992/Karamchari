namespace Karamchari.Recruitment.Domain.Primitives;

/// <summary>
/// Value object representing a compensation range.
/// Enforces Min <= Max.
/// </summary>
public sealed record CompensationRange
{
    public decimal Min { get; }
    public decimal Max { get; }
    public string Currency { get; }

    private CompensationRange(decimal min, decimal max, string currency)
    {
        Min = min;
        Max = max;
        Currency = currency;
    }

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

    public bool IsWithinRange(decimal amount) => amount >= Min && amount <= Max;
}

public enum HiringPriority
{
    Standard,
    High,
    Critical,
    Executive
}

public enum RequisitionStatus
{
    Draft,
    PendingApproval,
    Open,
    OnHold,
    Filled,
    Cancelled
}

public enum CandidateSource
{
    DirectApply,
    InternalReferral,
    Agency,
    Sourced,
    InternalMobility
}
