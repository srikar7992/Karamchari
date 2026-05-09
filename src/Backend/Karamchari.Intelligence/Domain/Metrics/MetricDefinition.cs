using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Intelligence.Domain.Metrics;

/// <summary>
/// Value object defining the calculation semantics of a metric.
/// </summary>
public sealed record CalculationDefinition(string Formula, string PrimarySourceContext, string RefreshCadence);

/// <summary>
/// Aggregate root representing the governed definition of an organizational metric.
/// Prevents metric drift by enforcing strict versioning and ownership.
/// </summary>
public sealed class MetricDefinition : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty; // e.g., "FlightRiskScore"
    public string Owner { get; private set; } = string.Empty; // e.g., "HR.Analytics"
    public string CurrentVersion { get; private set; } = "1.0";
    
    public CalculationDefinition Calculation { get; private set; } = null!;
    
    public bool IsDeprecated { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private MetricDefinition() { }

    public static MetricDefinition Define(
        string tenantId,
        string name,
        string owner,
        CalculationDefinition calculation)
    {
        return new MetricDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Owner = owner,
            Calculation = calculation,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void Evolve(string newVersion, CalculationDefinition newCalculation)
    {
        if (IsDeprecated) throw new InvalidOperationException("Cannot evolve a deprecated metric.");
        if (newVersion == CurrentVersion) throw new ArgumentException("Must provide a new version identifier.");

        CurrentVersion = newVersion;
        Calculation = newCalculation;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Deprecate()
    {
        IsDeprecated = true;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
