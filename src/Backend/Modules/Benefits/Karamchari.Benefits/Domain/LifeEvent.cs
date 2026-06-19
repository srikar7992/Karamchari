// -----------------------------------------------------------------------
// <copyright file="LifeEvent.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Benefits.Domain;

using Karamchari.Core.Domain.Primitives;

/// <summary>
/// A qualifying life event that opens a special enrollment window mid-year.
/// Owned by <see cref="BenefitEnrollment"/>. Until the event is verified by an admin,
/// the employee may prepare election changes but they are not activated.
///
/// The standard IRS window is 30 days from the qualifying event date (configurable per event type).
/// </summary>
public sealed class LifeEvent : Entity<LifeEventId>
{
    /// <summary>
    /// Default days after the qualifying event during which elections can be changed.
    /// Individual event types may override this (e.g., new hire uses a 60-day window).
    /// </summary>
    public const int DefaultWindowDays = 30;

    private LifeEvent() { }

    internal LifeEvent(
        LifeEventId id,
        BenefitEnrollmentId enrollmentId,
        LifeEventType eventType,
        DateOnly eventDate,
        string? documentStorageReference,
        int windowDays = DefaultWindowDays) : base(id)
    {
        EnrollmentId = enrollmentId;
        EventType = eventType;
        EventDate = eventDate;
        DocumentStorageReference = documentStorageReference;
        Status = LifeEventStatus.PendingVerification;
        SubmittedAtUtc = DateTimeOffset.UtcNow;
        WindowClosesAt = SubmittedAtUtc.AddDays(windowDays);
    }

    public BenefitEnrollmentId EnrollmentId { get; private set; }
    public LifeEventType EventType { get; private set; }

    /// <summary>The date the qualifying event actually occurred (e.g., wedding date, birth date).</summary>
    public DateOnly EventDate { get; private set; }

    /// <summary>Storage key for the supporting document uploaded by the employee.</summary>
    public string? DocumentStorageReference { get; private set; }

    public LifeEventStatus Status { get; private set; }
    public DateTimeOffset SubmittedAtUtc { get; private set; }

    /// <summary>
    /// Deadline by which the employee must complete election changes.
    /// Computed from submission time (not event date) to give a fair window
    /// even when employees submit documentation late.
    /// </summary>
    public DateTimeOffset WindowClosesAt { get; private set; }

    public string? RejectionReason { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public string? ReviewedByEmployeeId { get; private set; }

    public bool IsWindowOpen => Status == LifeEventStatus.Verified && DateTimeOffset.UtcNow <= WindowClosesAt;

    internal void Verify(string reviewerEmployeeId)
    {
        if (Status != LifeEventStatus.PendingVerification)
            throw new InvalidOperationException($"Life event is already {Status}.");

        Status = LifeEventStatus.Verified;
        ReviewedByEmployeeId = reviewerEmployeeId;
        ReviewedAtUtc = DateTimeOffset.UtcNow;
    }

    internal void Reject(string reviewerEmployeeId, string reason)
    {
        if (Status != LifeEventStatus.PendingVerification)
            throw new InvalidOperationException($"Life event is already {Status}.");

        Status = LifeEventStatus.Rejected;
        RejectionReason = reason;
        ReviewedByEmployeeId = reviewerEmployeeId;
        ReviewedAtUtc = DateTimeOffset.UtcNow;
    }

    internal void Complete()
    {
        if (Status != LifeEventStatus.Verified)
            throw new InvalidOperationException("Cannot complete a life event that has not been verified.");

        Status = LifeEventStatus.Completed;
    }
}
