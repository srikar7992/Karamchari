using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Intelligence.Domain.Primitives;

namespace Karamchari.Intelligence.Domain.Signals;

/// <summary>
/// Aggregate root for a strategic workforce simulation (Sandbox).
/// Allows executives to model the impact of attrition, hiring freezes, or rapid expansion.
/// </summary>
public sealed class StrategicWorkforceScenario : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    
    // The "What-If" parameters stored as JSON
    public string ScenarioParametersJson { get; private set; } = string.Empty;
    
    public bool IsConfidential { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private StrategicWorkforceScenario() { }

    public static StrategicWorkforceScenario Create(
        string tenantId,
        string name,
        string description,
        string parametersJson,
        string createdBy,
        bool isConfidential = true)
    {
        return new StrategicWorkforceScenario
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Description = description,
            ScenarioParametersJson = parametersJson,
            CreatedBy = createdBy,
            IsConfidential = isConfidential,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
