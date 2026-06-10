// -----------------------------------------------------------------------
// <copyright file="PositionVacancy.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.HR.Domain.Organization;

using System;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Specifies the current state of a position vacancy.
/// </summary>
public enum VacancyStatus
{
    /// <summary>The vacancy is in draft state.</summary>
    Draft,
    /// <summary>The vacancy is active and open for applications.</summary>
    Open,
    /// <summary>The vacancy has been filled by a candidate.</summary>
    Filled,
    /// <summary>The vacancy was cancelled before being filled.</summary>
    Cancelled
}

/// <summary>
/// Represents a vacancy/opening associated with a position slot.
/// </summary>
public sealed class PositionVacancy : AggregateRoot<Guid>, ITenantOwned
{
    private PositionVacancy() { TenantId = string.Empty; Reason = string.Empty; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PositionVacancy"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the vacancy.</param>
    /// <param name="tenantId">The unique identifier of the tenant context.</param>
    /// <param name="positionId">The ID of the position that is vacant.</param>
    /// <param name="openedUtc">The date and time when the vacancy was opened.</param>
    /// <param name="status">The lifecycle status of the vacancy.</param>
    /// <param name="reason">The reason/business need for opening the vacancy.</param>
    /// <param name="recruitmentRequisitionId">An optional reference ID to a recruitment requisition.</param>
    /// <param name="externalReferenceId">An optional external reference identifier (e.g. ATS tracking code).</param>
    /// <param name="closedUtc">The date and time when the vacancy was closed, if applicable.</param>
    /// <param name="closedReason">The explanation of why the vacancy was closed, if applicable.</param>
    /// <param name="filledByEmployeeId">The ID of the employee who filled the vacancy, if applicable.</param>
    /// <param name="filledPositionAssignmentId">The ID of the position assignment that filled the vacancy, if applicable.</param>
    public PositionVacancy(
        Guid id,
        string tenantId,
        Guid positionId,
        DateTimeOffset openedUtc,
        VacancyStatus status,
        string reason,
        Guid? recruitmentRequisitionId = null,
        string? externalReferenceId = null,
        DateTimeOffset? closedUtc = null,
        string? closedReason = null,
        Guid? filledByEmployeeId = null,
        Guid? filledPositionAssignmentId = null) : base(id)
    {
        TenantId = tenantId;
        PositionId = positionId;
        OpenedUtc = openedUtc;
        Status = status;
        Reason = reason ?? string.Empty;
        RecruitmentRequisitionId = recruitmentRequisitionId;
        ExternalReferenceId = externalReferenceId;
        ClosedUtc = closedUtc;
        ClosedReason = closedReason;
        FilledByEmployeeId = filledByEmployeeId;
        FilledPositionAssignmentId = filledPositionAssignmentId;
    }

    /// <summary>Gets the tenant ID for multi-tenant isolation.</summary>
    public string TenantId { get; private set; }
    /// <summary>Gets the position ID that is vacant.</summary>
    public Guid PositionId { get; private set; }
    /// <summary>Gets the date and time when the vacancy was opened.</summary>
    public DateTimeOffset OpenedUtc { get; private set; }
    /// <summary>Gets the lifecycle status of the vacancy.</summary>
    public VacancyStatus Status { get; private set; }
    /// <summary>Gets the reason/business need for opening the vacancy.</summary>
    public string Reason { get; private set; }
    /// <summary>Gets the reference ID to a recruitment requisition, if defined.</summary>
    public Guid? RecruitmentRequisitionId { get; private set; }
    /// <summary>Gets the external reference identifier (e.g. ATS tracking code), if defined.</summary>
    public string? ExternalReferenceId { get; private set; }
    /// <summary>Gets the date and time when the vacancy was closed, if applicable.</summary>
    public DateTimeOffset? ClosedUtc { get; private set; }
    /// <summary>Gets the explanation of why the vacancy was closed, if applicable.</summary>
    public string? ClosedReason { get; private set; }
    /// <summary>Gets the ID of the employee who filled the vacancy, if applicable.</summary>
    public Guid? FilledByEmployeeId { get; private set; }
    /// <summary>Gets the ID of the position assignment that filled the vacancy, if applicable.</summary>
    public Guid? FilledPositionAssignmentId { get; private set; }
}
