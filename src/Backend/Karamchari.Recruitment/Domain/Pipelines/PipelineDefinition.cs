using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Recruitment.Domain.Pipelines;

/// <summary>
/// Aggregate root defining a standard hiring pipeline (e.g. "Engineering Pipeline").
/// Enforces stage transitions.
/// </summary>
public sealed class PipelineDefinition : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<PipelineStage> _stages = [];

    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    
    public IReadOnlyCollection<PipelineStage> Stages => _stages.OrderBy(s => s.Sequence).ToList().AsReadOnly();

    private PipelineDefinition() { }

    public static PipelineDefinition Create(string tenantId, string name)
    {
        return new PipelineDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name
        };
    }

    public void AddStage(string name, int sequence, bool requiresInterview = false, bool requiresApproval = false)
    {
        if (_stages.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Stage {name} already exists.");

        if (_stages.Any(s => s.Sequence == sequence))
            throw new InvalidOperationException($"Sequence {sequence} is already in use.");

        _stages.Add(PipelineStage.Create(Id, name, sequence, requiresInterview, requiresApproval));
    }

    public PipelineStage GetNextStage(string currentStageName)
    {
        var current = _stages.FirstOrDefault(s => s.Name == currentStageName)
            ?? throw new InvalidOperationException($"Stage {currentStageName} not found in pipeline.");

        return _stages.OrderBy(s => s.Sequence).FirstOrDefault(s => s.Sequence > current.Sequence)
            ?? throw new InvalidOperationException("No further stages available.");
    }
}

public sealed class PipelineStage : Entity<Guid>
{
    public Guid PipelineId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Sequence { get; private set; }
    public bool RequiresInterview { get; private set; }
    public bool RequiresApproval { get; private set; }

    private PipelineStage() { }

    internal static PipelineStage Create(Guid pipelineId, string name, int sequence, bool requiresInterview, bool requiresApproval)
    {
        return new PipelineStage
        {
            Id = Guid.NewGuid(),
            PipelineId = pipelineId,
            Name = name,
            Sequence = sequence,
            RequiresInterview = requiresInterview,
            RequiresApproval = requiresApproval
        };
    }
}
