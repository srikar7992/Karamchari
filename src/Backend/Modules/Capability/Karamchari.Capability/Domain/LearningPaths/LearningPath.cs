using Karamchari.Capability.Domain.Events;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Capability.Domain.LearningPaths;

public sealed class LearningPath : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<LearningPathStep> _steps = [];

    private LearningPath() { }

    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? TargetRole { get; private set; }
    public IReadOnlyList<LearningPathStep> Steps => _steps.AsReadOnly();

    public static LearningPath Create(string tenantId, string name, string? targetRole = null)
        => new() { Id = Guid.NewGuid(), TenantId = tenantId, Name = name, TargetRole = targetRole };

    public void AddStep(int order, Guid moduleId, bool optional = false)
        => _steps.Add(new LearningPathStep(order, moduleId, optional));
}
