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
        Guid? reportingToPositionId) : base(id)
    {
        TenantId = tenantId;
        Code = code;
        DepartmentId = departmentId;
        DesignationId = designationId;
        ReportingToPositionId = reportingToPositionId;
        IsActive = true;
    }

    public string TenantId { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public Guid DepartmentId { get; private set; }
    public Guid DesignationId { get; private set; }

    /// <summary>Position-based reporting hierarchy.</summary>
    public Guid? ReportingToPositionId { get; private set; }

    public bool IsActive { get; private set; }

    public static Position Create(
        string tenantId,
        string code,
        Guid departmentId,
        Guid designationId,
        Guid? reportingToPositionId = null)
    {
        return new Position(Guid.NewGuid(), tenantId, code.Trim().ToUpperInvariant(),
            departmentId, designationId, reportingToPositionId);
    }
}
