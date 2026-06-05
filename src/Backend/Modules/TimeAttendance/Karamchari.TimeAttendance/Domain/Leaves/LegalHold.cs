using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Leaves;

public enum LegalHoldScope
{
    Employee = 1,
    Department = 2,
    Tenant = 3,
}

public enum LegalHoldStatus
{
    Active = 1,
    Lifted = 2,
}

/// <summary>
/// Compliance freeze that blocks all write operations (create/modify/backdate/delete)
/// on leave records for a given scope while preserving read access.
/// Ordered by HR/Legal during labour disputes, court proceedings, or internal investigations.
/// </summary>
public sealed class LegalHold : AggregateRoot<Guid>, ITenantOwned
{
    private LegalHold() { TenantId = string.Empty; }

    public string TenantId { get; private set; }
    public LegalHoldScope Scope { get; private set; }
    /// <summary>EmployeeId, DepartmentId, or null (Tenant-wide).</summary>
    public Guid? ScopeEntityId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string CaseReference { get; private set; } = string.Empty;
    public string InitiatedBy { get; private set; } = string.Empty;
    public DateTimeOffset InitiatedAt { get; private set; }
    public DateTimeOffset? LiftedAt { get; private set; }
    public string? LiftedBy { get; private set; }
    public string? LiftReason { get; private set; }
    public LegalHoldStatus Status { get; private set; }

    public static LegalHold Create(
        string tenantId,
        LegalHoldScope scope,
        Guid? scopeEntityId,
        string reason,
        string caseReference,
        string initiatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(initiatedBy);

        return new LegalHold
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Scope = scope,
            ScopeEntityId = scopeEntityId,
            Reason = reason,
            CaseReference = caseReference,
            InitiatedBy = initiatedBy,
            InitiatedAt = DateTimeOffset.UtcNow,
            Status = LegalHoldStatus.Active,
        };
    }

    public void Lift(string liftedBy, string liftReason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(liftedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(liftReason);

        if (Status == LegalHoldStatus.Lifted)
            throw new InvalidOperationException("Legal hold already lifted.");

        LiftedBy = liftedBy;
        LiftReason = liftReason;
        LiftedAt = DateTimeOffset.UtcNow;
        Status = LegalHoldStatus.Lifted;
    }

    public bool IsActiveOn(DateTimeOffset at) =>
        Status == LegalHoldStatus.Active && InitiatedAt <= at;
}
