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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyCollection<PipelineStage> Stages => _stages.OrderBy(s => s.Sequence).ToList().AsReadOnly();

    private PipelineDefinition() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static PipelineDefinition Create(string tenantId, string name)
    {
        return new PipelineDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name
        };
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void AddStage(string name, int sequence, bool requiresInterview = false, bool requiresApproval = false)
    {
        if (_stages.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Stage {name} already exists.");

        if (_stages.Any(s => s.Sequence == sequence))
            throw new InvalidOperationException($"Sequence {sequence} is already in use.");

        _stages.Add(PipelineStage.Create(Id, name, sequence, requiresInterview, requiresApproval));
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public PipelineStage GetNextStage(string currentStageName)
    {
        var current = _stages.FirstOrDefault(s => s.Name == currentStageName)
            ?? throw new InvalidOperationException($"Stage {currentStageName} not found in pipeline.");

        return _stages.OrderBy(s => s.Sequence).FirstOrDefault(s => s.Sequence > current.Sequence)
            ?? throw new InvalidOperationException("No further stages available.");
    }
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class PipelineStage : Entity<Guid>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid PipelineId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int Sequence { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool RequiresInterview { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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
