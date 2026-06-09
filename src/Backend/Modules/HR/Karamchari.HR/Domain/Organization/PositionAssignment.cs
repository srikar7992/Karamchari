namespace Karamchari.HR.Domain.Organization;

using System;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Represents the assignment of an employee to a specific position over a given time duration.
/// </summary>
public sealed class PositionAssignment : AggregateRoot<Guid>, ITenantOwned
{
    private PositionAssignment() { TenantId = string.Empty; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PositionAssignment"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the assignment.</param>
    /// <param name="tenantId">The unique identifier of the tenant context.</param>
    /// <param name="positionId">The ID of the position to assign.</param>
    /// <param name="employeeId">The ID of the employee to assign to the position.</param>
    /// <param name="startUtc">The starting date and time of the assignment.</param>
    /// <param name="endUtc">The ending date and time of the assignment, if defined.</param>
    public PositionAssignment(
        Guid id,
        string tenantId,
        Guid positionId,
        Guid employeeId,
        DateTimeOffset startUtc,
        DateTimeOffset? endUtc) : base(id)
    {
        TenantId = tenantId;
        PositionId = positionId;
        EmployeeId = employeeId;
        StartUtc = startUtc;
        EndUtc = endUtc;
    }

    /// <summary>Gets the tenant ID for multi-tenant isolation.</summary>
    public string TenantId { get; private set; }
    /// <summary>Gets the position ID.</summary>
    public Guid PositionId { get; private set; }
    /// <summary>Gets the employee ID.</summary>
    public Guid EmployeeId { get; private set; }
    /// <summary>Gets the starting date and time of the assignment.</summary>
    public DateTimeOffset StartUtc { get; private set; }
    /// <summary>Gets the ending date and time of the assignment, if defined.</summary>
    public DateTimeOffset? EndUtc { get; private set; }

    /// <summary>Gets a value indicating whether the assignment is currently active.</summary>
    public bool IsActive => EndUtc == null || EndUtc > DateTimeOffset.UtcNow;
}
