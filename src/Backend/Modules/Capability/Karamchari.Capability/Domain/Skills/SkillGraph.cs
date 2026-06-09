using System;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Capability.Domain.Skills;

/// <summary>
/// Represents a directed relationship between two skills in the dynamic skills ontology.
/// </summary>
public sealed class SkillGraphRelationship : Entity<Guid>, ITenantOwned
{
    private SkillGraphRelationship()
    {
        TenantId = string.Empty;
        RelationshipType = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SkillGraphRelationship"/> class.
    /// </summary>
    public SkillGraphRelationship(
        Guid id,
        string tenantId,
        Guid sourceSkillId,
        Guid targetSkillId,
        decimal weight,
        string relationshipType) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(relationshipType);

        TenantId = tenantId;
        SourceSkillId = sourceSkillId;
        TargetSkillId = targetSkillId;
        Weight = weight;
        RelationshipType = relationshipType;
    }

    /// <inheritdoc />
    public string TenantId { get; private set; }

    /// <summary>
    /// Gets the source skill identifier.
    /// </summary>
    public Guid SourceSkillId { get; private set; }

    /// <summary>
    /// Gets the target skill identifier.
    /// </summary>
    public Guid TargetSkillId { get; private set; }

    /// <summary>
    /// Gets the overlap weight (e.g., 0.80 if SourceSkill overlaps TargetSkill by 80%).
    /// </summary>
    public decimal Weight { get; private set; }

    /// <summary>
    /// Gets the type of relationship (e.g., ParentOf, Synonym, RelatedTo).
    /// </summary>
    public string RelationshipType { get; private set; }
}
