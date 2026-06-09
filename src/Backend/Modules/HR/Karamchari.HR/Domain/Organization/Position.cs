using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.HR.Domain.Organization;

/// <summary>
/// Represents an operational workforce slot or headcount position.
/// Distinct from Designation (Title/Rank).
/// </summary>
public sealed class Position : AggregateRoot<Guid>, ITenantOwned
{
    private Position() { }

    private Position(
        Guid id,
        string tenantId,
        string code,
        Guid departmentId,
        Guid designationId,
        Guid? reportingToPositionId,
        Guid? costCenterId,
        Guid? locationId,
        Guid? businessUnitId,
        Guid? orgUnitId,
        PositionStatus status,
        bool allowMultipleOccupants,
        int approvedHeadcount) : base(id)
    {
        TenantId = tenantId;
        Code = code;
        DepartmentId = departmentId;
        DesignationId = designationId;
        ReportingToPositionId = reportingToPositionId;
        CostCenterId = costCenterId;
        LocationId = locationId;
        BusinessUnitId = businessUnitId;
        OrgUnitId = orgUnitId;
        Status = status;
        AllowMultipleOccupants = allowMultipleOccupants;
        ApprovedHeadcount = approvedHeadcount;
        IsActive = true;
    }

    public string TenantId { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public Guid DepartmentId { get; private set; }
    public Guid DesignationId { get; private set; }

    /// <summary>Position-based reporting hierarchy.</summary>
    public Guid? ReportingToPositionId { get; private set; }

    public Guid? CostCenterId { get; private set; }
    public Guid? LocationId { get; private set; }
    public Guid? BusinessUnitId { get; private set; }
    public Guid? OrgUnitId { get; private set; }

    public PositionStatus Status { get; private set; }
    public bool AllowMultipleOccupants { get; private set; }
    public int ApprovedHeadcount { get; private set; }

    /// <summary>Links this position to a Capability role skill requirement for internal mobility matching.</summary>
    public Guid? RoleSkillRequirementId { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Associates this position with a Capability role skill requirement profile.</summary>
    public void LinkToRoleRequirement(Guid roleSkillRequirementId)
    {
        RoleSkillRequirementId = roleSkillRequirementId;
    }

    public static Position Create(
        string tenantId,
        string code,
        Guid departmentId,
        Guid designationId,
        Guid? reportingToPositionId = null,
        Guid? costCenterId = null,
        Guid? locationId = null,
        Guid? businessUnitId = null,
        Guid? orgUnitId = null,
        PositionStatus status = PositionStatus.Draft,
        bool allowMultipleOccupants = false,
        int approvedHeadcount = 1)
    {
        return new Position(
            Guid.NewGuid(),
            tenantId,
            code.Trim().ToUpperInvariant(),
            departmentId,
            designationId,
            reportingToPositionId,
            costCenterId,
            locationId,
            businessUnitId,
            orgUnitId,
            status,
            allowMultipleOccupants,
            approvedHeadcount);
    }
}
