using System;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Capability.Domain.Skills;

/// <summary>
/// Domain model for validation evidence associated with employee skills (e.g. GitHub projects, Certifications).
/// </summary>
public sealed class SkillEvidence : AggregateRoot<Guid>, ITenantOwned
{
    private SkillEvidence()
    {
        TenantId = string.Empty;
        EvidenceType = string.Empty;
        Description = string.Empty;
        MetadataJson = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SkillEvidence"/> class.
    /// </summary>
    public SkillEvidence(
        Guid id,
        string tenantId,
        Guid employeeId,
        Guid skillId,
        string evidenceType,
        string description,
        string metadataJson) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceType);

        TenantId = tenantId;
        EmployeeId = employeeId;
        SkillId = skillId;
        EvidenceType = evidenceType;
        Description = description ?? string.Empty;
        MetadataJson = metadataJson ?? string.Empty;
        VerifiedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public string TenantId { get; private set; }

    /// <summary>
    /// Gets the employee identifier who owns the skill.
    /// </summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>
    /// Gets the associated skill definition identifier.
    /// </summary>
    public Guid SkillId { get; private set; }

    /// <summary>
    /// Gets the type of evidence (e.g., GitHub, Certification, ManagerEndorsement, ProjectParticipation).
    /// </summary>
    public string EvidenceType { get; private set; }

    /// <summary>
    /// Gets the description or title of the validation source.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets any additional context stored as JSON metadata.
    /// </summary>
    public string MetadataJson { get; private set; }

    /// <summary>
    /// Gets the timestamp when this evidence was registered/verified.
    /// </summary>
    public DateTimeOffset VerifiedAtUtc { get; private set; }

    /// <summary>
    /// Gets the manager or authorizer employee ID who verified this evidence, if applicable.
    /// </summary>
    public Guid? VerifiedByEmployeeId { get; private set; }

    /// <summary>
    /// Confirms and endorses this evidence by a manager.
    /// </summary>
    public void Verify(Guid managerId)
    {
        VerifiedByEmployeeId = managerId;
        VerifiedAtUtc = DateTimeOffset.UtcNow;
    }
}
