// -----------------------------------------------------------------------
// <copyright file="EnrollmentWindow.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Benefits.Domain;

using Karamchari.Benefits.Domain.Events;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Aggregate root that governs the period during which employees may make (or change) benefit elections.
///
/// Every <see cref="BenefitEnrollment"/> must belong to a window — the window is what grants an employee
/// the right to elect coverage. This makes provenance auditable: "Why was this election allowed?" is
/// always answerable by traversing the enrollment's WindowId.
///
/// Two window types are supported:
///   NewHire        — opened automatically when an employee is onboarded; typically 30 days.
///   OpenEnrollment — scheduled by HR for an upcoming plan year; all eligible employees participate.
///
/// Lifecycle: Scheduled → Open → Closed
///   CreateForNewHire opens the window immediately.
///   CreateOpenEnrollment leaves it Scheduled until explicitly opened by HR.
/// </summary>
public sealed class EnrollmentWindow : AggregateRoot<EnrollmentWindowId>, ITenantOwned
{
    private EnrollmentWindow() { TenantId = string.Empty; }

    private EnrollmentWindow(
        EnrollmentWindowId id,
        string tenantId,
        EnrollmentWindowType windowType,
        DateOnly planYearStart,
        DateOnly planYearEnd,
        DateTimeOffset windowOpensAt,
        DateTimeOffset windowClosesAt) : base(id)
    {
        TenantId = tenantId;
        WindowType = windowType;
        PlanYearStart = planYearStart;
        PlanYearEnd = planYearEnd;
        WindowOpensAt = windowOpensAt;
        WindowClosesAt = windowClosesAt;
        Status = EnrollmentWindowStatus.Scheduled;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public string TenantId { get; private set; }
    public EnrollmentWindowType WindowType { get; private set; }
    public EnrollmentWindowStatus Status { get; private set; }

    /// <summary>The plan year this window covers.</summary>
    public DateOnly PlanYearStart { get; private set; }

    /// <summary>The plan year this window covers.</summary>
    public DateOnly PlanYearEnd { get; private set; }

    /// <summary>Scheduled start of the election window (used for display; does not auto-open).</summary>
    public DateTimeOffset WindowOpensAt { get; private set; }

    /// <summary>Scheduled end of the election window. Elections are rejected after this timestamp.</summary>
    public DateTimeOffset WindowClosesAt { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? OpenedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }

    /// <summary>
    /// Creates a new-hire enrollment window and opens it immediately.
    /// Called by <c>EmployeeOnboardedBenefitsConsumer</c> when a new employee is onboarded.
    /// The window remains open for <paramref name="windowDays"/> days (default: 30).
    /// </summary>
    public static EnrollmentWindow CreateForNewHire(
        string tenantId,
        DateOnly planYearStart,
        DateOnly planYearEnd,
        DateTimeOffset now,
        int windowDays = 30)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var window = new EnrollmentWindow(
            new EnrollmentWindowId(Guid.NewGuid()),
            tenantId,
            EnrollmentWindowType.NewHire,
            planYearStart,
            planYearEnd,
            windowOpensAt: now,
            windowClosesAt: now.AddDays(windowDays));

        window.Open();
        return window;
    }

    /// <summary>
    /// Creates an open-enrollment window in Scheduled status for an upcoming plan year.
    /// HR must call <see cref="Open"/> when the enrollment period begins.
    /// </summary>
    public static EnrollmentWindow CreateOpenEnrollment(
        string tenantId,
        DateOnly planYearStart,
        DateOnly planYearEnd,
        DateTimeOffset windowOpensAt,
        DateTimeOffset windowClosesAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        if (windowClosesAt <= windowOpensAt)
            throw new ArgumentException("Enrollment window must close after it opens.");

        return new EnrollmentWindow(
            new EnrollmentWindowId(Guid.NewGuid()),
            tenantId,
            EnrollmentWindowType.OpenEnrollment,
            planYearStart,
            planYearEnd,
            windowOpensAt,
            windowClosesAt);
    }

    /// <summary>
    /// Transitions the window to Open, allowing employees to make elections.
    /// </summary>
    public void Open()
    {
        if (Status != EnrollmentWindowStatus.Scheduled)
            throw new InvalidOperationException(
                $"Only Scheduled windows can be opened. Current status: {Status}.");

        Status = EnrollmentWindowStatus.Open;
        OpenedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new EnrollmentWindowOpenedEvent(
            TenantId, Id, WindowType, PlanYearStart, PlanYearEnd, WindowClosesAt));
    }

    /// <summary>
    /// Closes the window, preventing any further elections against it.
    /// </summary>
    public void Close()
    {
        if (Status != EnrollmentWindowStatus.Open)
            throw new InvalidOperationException(
                $"Only Open windows can be closed. Current status: {Status}.");

        Status = EnrollmentWindowStatus.Closed;
        ClosedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new EnrollmentWindowClosedEvent(TenantId, Id, WindowType, PlanYearStart));
    }
}
