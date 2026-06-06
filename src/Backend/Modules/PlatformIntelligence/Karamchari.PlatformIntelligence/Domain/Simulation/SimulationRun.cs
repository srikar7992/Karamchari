using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.PlatformIntelligence.Domain.Simulation;

public sealed class SimulationRun : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string ParametersJson { get; private set; } = string.Empty;
    public string? OutcomeJson { get; private set; }
    public SimulationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string RequestedBy { get; private set; } = string.Empty;

    private SimulationRun() { }

    public static SimulationRun Create(
        string tenantId,
        string name,
        string parametersJson,
        string requestedBy)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            ParametersJson = parametersJson,
            Status = SimulationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            RequestedBy = requestedBy
        };

    public void MarkRunning() => Status = SimulationStatus.Running;

    public void MarkCompleted(string outcomeJson)
    {
        OutcomeJson = outcomeJson;
        Status = SimulationStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        Status = SimulationStatus.Failed;
        CompletedAt = DateTime.UtcNow;
    }
}
