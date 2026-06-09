using System;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Capability.Domain.Skills;

/// <summary>
/// Represents a node within the dynamic skill graph ontology, defining categories and adjacency boundaries.
/// </summary>
public sealed class SkillNode : AggregateRoot<Guid>, ITenantOwned
{
    private SkillNode()
    {
        TenantId = string.Empty;
        SkillName = string.Empty;
        SkillCluster = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SkillNode"/> class.
    /// </summary>
    public SkillNode(Guid id, string tenantId, string name, string cluster) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        TenantId = tenantId;
        SkillName = name;
        SkillCluster = cluster ?? string.Empty;
    }

    /// <inheritdoc />
    public string TenantId { get; private set; }

    /// <summary>
    /// Gets the skill identifier display name.
    /// </summary>
    public string SkillName { get; private set; }

    /// <summary>
    /// Gets the cluster classification (e.g. Frontend Engineering, Web Development, Cloud DevOps).
    /// </summary>
    public string SkillCluster { get; private set; }
}
