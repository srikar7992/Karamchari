// -----------------------------------------------------------------------
// <copyright file="OpportunityParticipant.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Capability.Domain.Marketplace;

/// <summary>
/// Status state of an employee's enrollment in an opportunity.
/// </summary>
public enum EnrollmentStatus
{
    /// <summary>Application submitted</summary>
    Applied,
    /// <summary>Approved but not started</summary>
    Approved,
    /// <summary>Currently active</summary>
    Active,
    /// <summary>Withdrawn by applicant</summary>
    Withdrawn,
    /// <summary>Completed and closed out</summary>
    Completed
}

/// <summary>
/// Represents an employee enrollment in an internal project or gig.
/// </summary>
public sealed class OpportunityParticipant : Entity<Guid>, ITenantOwned
{
    private OpportunityParticipant() { TenantId = string.Empty; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpportunityParticipant"/> class.
    /// </summary>
    public OpportunityParticipant(
        Guid id,
        string tenantId,
        Guid opportunityId,
        Guid employeeId,
        int allocatedHours,
        EnrollmentStatus status) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        TenantId = tenantId;
        OpportunityId = opportunityId;
        EmployeeId = employeeId;
        AllocatedHours = allocatedHours;
        Status = status;
        JoinedUtc = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public string TenantId { get; private set; }

    /// <summary>
    /// Gets the opportunity ID reference.
    /// </summary>
    public Guid OpportunityId { get; private set; }

    /// <summary>
    /// Gets the enrolled employee ID reference.
    /// </summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>
    /// Gets the timestamp when applicant joined or applied.
    /// </summary>
    public DateTimeOffset JoinedUtc { get; private set; }

    /// <summary>
    /// Gets the committed weekly working hours.
    /// </summary>
    public int AllocatedHours { get; private set; }

    /// <summary>
    /// Gets the current status state of the enrollment.
    /// </summary>
    public EnrollmentStatus Status { get; private set; }

    /// <summary>
    /// Updates the enrollment status.
    /// </summary>
    public void UpdateStatus(EnrollmentStatus newStatus)
    {
        Status = newStatus;
    }
}
