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
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Name { get; private set; } = string.Empty; // e.g., "FlightRiskScore"
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Owner { get; private set; } = string.Empty; // e.g., "HR.Analytics"
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string CurrentVersion { get; private set; } = "1.0";

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public CalculationDefinition Calculation { get; private set; } = null!;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsDeprecated { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }
    /// <inheritdoc/>
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    private MetricDefinition() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Evolve(string newVersion, CalculationDefinition newCalculation)
    {
        if (IsDeprecated) throw new InvalidOperationException("Cannot evolve a deprecated metric.");
        if (newVersion == CurrentVersion) throw new ArgumentException("Must provide a new version identifier.");

        CurrentVersion = newVersion;
        Calculation = newCalculation;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Deprecate()
    {
        IsDeprecated = true;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
