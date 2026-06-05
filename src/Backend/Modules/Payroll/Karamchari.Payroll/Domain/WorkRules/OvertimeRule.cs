namespace Karamchari.Payroll.Domain.WorkRules;

public enum OvertimeContext
{
    Regular = 1,
    Holiday = 2,
    Weekend = 3,
    Night = 4
}

/// <summary>
/// Single tier within an overtime policy.
/// Rules are evaluated in priority order: lowest FromHours wins first tier.
/// </summary>
public sealed class OvertimeRule
{
    /// <summary>Hours from this threshold (exclusive) up to ToHours apply this multiplier.</summary>
    public decimal FromHours { get; private set; }

    /// <summary>Null means unbounded — all hours above FromHours.</summary>
    public decimal? ToHours { get; private set; }

    public decimal Multiplier { get; private set; }
    public OvertimeContext Context { get; private set; }
    public int Priority { get; private set; }

    private OvertimeRule() { }

    public static OvertimeRule Create(decimal fromHours, decimal? toHours, decimal multiplier, OvertimeContext context, int priority)
    {
        if (multiplier <= 0) throw new ArgumentException("Multiplier must be positive.");
        if (toHours.HasValue && toHours.Value <= fromHours)
            throw new ArgumentException("ToHours must be greater than FromHours.");

        return new OvertimeRule
        {
            FromHours = fromHours,
            ToHours = toHours,
            Multiplier = multiplier,
            Context = context,
            Priority = priority
        };
    }

    /// <summary>Calculates OT pay for hours within this tier.</summary>
    public decimal Calculate(decimal hourlyRate, decimal hoursInTier) =>
        hourlyRate * Multiplier * hoursInTier;
}
