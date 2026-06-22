// -----------------------------------------------------------------------
// <copyright file="BenefitEnrollment.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Benefits.Domain;

using Karamchari.Benefits.Domain.Events;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Aggregate root for an employee's benefit elections for a single plan year.
/// One enrollment per employee per plan year is enforced at the application layer.
///
/// The lifecycle is:
///   PendingSubmission → (employee elects) → Submitted → (HR confirms) → Active → Terminated
///
/// Election changes mid-year follow a separate path:
///   Active + LifeEvent(PendingVerification) → (admin verifies) → election change applied → domain event
///
/// Elections are never deleted — superseded elections are marked inactive for audit.
/// Dependents removed mid-year are soft-deleted (IsRemoved = true) for the same reason.
/// </summary>
public sealed class BenefitEnrollment : AggregateRoot<BenefitEnrollmentId>, ITenantOwned
{
    private readonly List<BenefitElection> _elections = [];
    private readonly List<BenefitDependent> _dependents = [];
    private readonly List<LifeEvent> _lifeEvents = [];

    private BenefitEnrollment() { TenantId = string.Empty; }

    private BenefitEnrollment(
        BenefitEnrollmentId id,
        string tenantId,
        Guid employeeId,
        string employeeName,
        DateOnly planYearStart,
        DateOnly planYearEnd,
        DateOnly hireDate,
        EnrollmentWindowId windowId) : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        EmployeeName = employeeName;
        PlanYearStart = planYearStart;
        PlanYearEnd = planYearEnd;
        HireDate = hireDate;
        WindowId = windowId;
        Status = EnrollmentStatus.PendingSubmission;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public string TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string EmployeeName { get; private set; } = string.Empty;
    public DateOnly PlanYearStart { get; private set; }
    public DateOnly PlanYearEnd { get; private set; }
    public DateOnly HireDate { get; private set; }

    /// <summary>
    /// The enrollment window under which this enrollment was opened.
    /// Every enrollment must be traceable to the window that authorised its creation —
    /// this is the provenance anchor for audit ("why was this election allowed?").
    /// </summary>
    public EnrollmentWindowId WindowId { get; private set; }

    public EnrollmentStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public DateTimeOffset? ActivatedAtUtc { get; private set; }
    public DateTimeOffset? TerminatedAtUtc { get; private set; }

    public IReadOnlyList<BenefitElection> Elections => _elections.AsReadOnly();
    public IReadOnlyList<BenefitDependent> Dependents => _dependents.AsReadOnly();
    public IReadOnlyList<LifeEvent> LifeEvents => _lifeEvents.AsReadOnly();

    /// <summary>Only elections that are currently active (not superseded by a life-event change).</summary>
    public IEnumerable<BenefitElection> ActiveElections => _elections.Where(e => e.IsActive);

    /// <summary>Dependents that have not been removed.</summary>
    public IEnumerable<BenefitDependent> ActiveDependents => _dependents.Where(d => !d.IsRemoved);

    /// <summary>
    /// Opens a new pending enrollment for an employee under a specific enrollment window.
    /// The <paramref name="windowId"/> records which window authorised this enrollment,
    /// making the election provenance auditable for the lifetime of the enrollment record.
    /// </summary>
    public static BenefitEnrollment Open(
        string tenantId,
        Guid employeeId,
        string employeeName,
        DateOnly planYearStart,
        DateOnly planYearEnd,
        DateOnly hireDate,
        EnrollmentWindowId windowId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeName);

        if (planYearEnd <= planYearStart)
            throw new ArgumentException("Plan year end must be after plan year start.");

        return new BenefitEnrollment(
            new BenefitEnrollmentId(Guid.NewGuid()),
            tenantId, employeeId, employeeName,
            planYearStart, planYearEnd, hireDate, windowId);
    }

    // ── Elections ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Records the employee's election for a specific category.
    /// Replaces any existing pending election for the same category (last-write-wins before submission).
    /// Throws if the enrollment is not in a state that accepts changes (Active, Submitted require
    /// a verified life event window to be open first — use <see cref="ApplyLifeEventElectionChange"/>).
    /// </summary>
    public BenefitElection Elect(
        BenefitPlanId planId,
        BenefitCategory category,
        CoverageTier tier,
        decimal employeeMonthlyPremium,
        decimal employerMonthlyPremium,
        DateOnly effectiveDate)
    {
        if (Status != EnrollmentStatus.PendingSubmission)
            throw new InvalidOperationException(
                $"New elections can only be added when the enrollment is {EnrollmentStatus.PendingSubmission}. " +
                $"Current status: {Status}. Use {nameof(ApplyLifeEventElectionChange)} for mid-year changes.");

        // Remove any prior pending election for the same category (employee changed their mind).
        var existing = _elections.FirstOrDefault(e => e.Category == category && e.IsActive);
        if (existing is not null) _elections.Remove(existing);

        var election = new BenefitElection(
            new BenefitElectionId(Guid.NewGuid()),
            Id, planId, category, tier,
            employeeMonthlyPremium, employerMonthlyPremium, effectiveDate);

        _elections.Add(election);
        return election;
    }

    /// <summary>
    /// Marks a category as explicitly waived by the employee (they have other coverage).
    /// Removes any existing election for the category.
    /// </summary>
    public void WaiveCategory(BenefitCategory category)
    {
        if (Status != EnrollmentStatus.PendingSubmission)
            throw new InvalidOperationException("Category waivers can only be set before submission.");

        var existing = _elections.FirstOrDefault(e => e.Category == category && e.IsActive);
        if (existing is not null) _elections.Remove(existing);
    }

    // ── Dependents ────────────────────────────────────────────────────────────

    public BenefitDependent AddDependent(
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        DependentRelationship relationship,
        bool enrollMedical = false,
        bool enrollDental = false,
        bool enrollVision = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        if (Status == EnrollmentStatus.Terminated)
            throw new InvalidOperationException("Cannot add dependents to a terminated enrollment.");

        var dependent = new BenefitDependent(
            new BenefitDependentId(Guid.NewGuid()),
            Id, firstName, lastName, dateOfBirth, relationship);
        dependent.UpdateCoverages(enrollMedical, enrollDental, enrollVision);
        _dependents.Add(dependent);
        return dependent;
    }

    public void RemoveDependent(BenefitDependentId dependentId)
    {
        var dep = _dependents.FirstOrDefault(d => d.Id == dependentId && !d.IsRemoved)
            ?? throw new InvalidOperationException("Dependent not found or already removed.");
        dep.Remove();
    }

    // ── Submission ────────────────────────────────────────────────────────────

    /// <summary>
    /// Submits the employee's elections for HR review.
    /// An empty election list is valid — the employee is waiving all coverage.
    /// </summary>
    public void Submit()
    {
        if (Status != EnrollmentStatus.PendingSubmission)
            throw new InvalidOperationException($"Enrollment is already {Status}.");

        Status = EnrollmentStatus.Submitted;
        SubmittedAtUtc = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new BenefitElectionsSubmittedEvent(
            TenantId, Id, EmployeeId, _elections.Count(e => e.IsActive)));
    }

    /// <summary>
    /// Activates the enrollment after HR review. Triggers payroll deduction setup.
    /// </summary>
    public void Activate()
    {
        if (Status != EnrollmentStatus.Submitted)
            throw new InvalidOperationException($"Only Submitted enrollments can be activated. Current: {Status}.");

        if (_elections.Count(e => e.IsActive) == 0)
            Status = EnrollmentStatus.WaivedAllCoverage;
        else
            Status = EnrollmentStatus.Active;

        ActivatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new BenefitEnrollmentActivatedEvent(TenantId, Id, EmployeeId));
    }

    // ── Life Events ───────────────────────────────────────────────────────────

    /// <summary>
    /// Submits a qualifying life event and opens a change window.
    /// The employee may then update their elections via <see cref="ApplyLifeEventElectionChange"/>,
    /// but those changes are not activated until the life event is verified by an admin.
    /// </summary>
    public LifeEvent SubmitLifeEvent(
        LifeEventType eventType,
        DateOnly eventDate,
        string? documentStorageReference,
        int windowDays = LifeEvent.DefaultWindowDays)
    {
        if (Status != EnrollmentStatus.Active)
            throw new InvalidOperationException("Life events can only be submitted on Active enrollments.");

        // Prevent duplicate open life events of the same type.
        if (_lifeEvents.Any(le => le.EventType == eventType
            && le.Status == LifeEventStatus.PendingVerification))
            throw new InvalidOperationException(
                $"A pending life event of type {eventType} already exists.");

        var le = new LifeEvent(
            new LifeEventId(Guid.NewGuid()),
            Id, eventType, eventDate, documentStorageReference, windowDays);

        _lifeEvents.Add(le);

        RaiseDomainEvent(new LifeEventSubmittedEvent(
            TenantId, Id, le.Id, EmployeeId, eventType, eventDate, le.WindowClosesAt, documentStorageReference));

        return le;
    }

    /// <summary>
    /// Admin-side: verifies a pending life event, opening the election change window.
    /// </summary>
    public void VerifyLifeEvent(LifeEventId lifeEventId, string reviewerEmployeeId)
    {
        var le = FindLifeEvent(lifeEventId);
        le.Verify(reviewerEmployeeId);
    }

    /// <summary>
    /// Admin-side: rejects a pending life event (invalid documentation).
    /// </summary>
    public void RejectLifeEvent(LifeEventId lifeEventId, string reviewerEmployeeId, string reason)
    {
        var le = FindLifeEvent(lifeEventId);
        le.Reject(reviewerEmployeeId, reason);
    }

    /// <summary>
    /// Applies an election change enabled by a verified life event.
    /// Supersedes the prior election for the category and records the new one.
    /// Raises the domain event that triggers payroll deduction updates.
    /// </summary>
    public BenefitElection ApplyLifeEventElectionChange(
        LifeEventId lifeEventId,
        BenefitPlanId planId,
        BenefitCategory category,
        CoverageTier tier,
        decimal employeeMonthlyPremium,
        decimal employerMonthlyPremium,
        DateOnly effectiveDate)
    {
        if (Status != EnrollmentStatus.Active)
            throw new InvalidOperationException("Election changes via life events require an Active enrollment.");

        var le = FindLifeEvent(lifeEventId);
        if (!le.IsWindowOpen)
            throw new InvalidOperationException(
                "The life event window is not open (event not verified, already completed, or window expired).");

        // Supersede the prior active election for this category.
        var prior = _elections.FirstOrDefault(e => e.Category == category && e.IsActive);
        prior?.Supersede();

        var updated = new BenefitElection(
            new BenefitElectionId(Guid.NewGuid()),
            Id, planId, category, tier,
            employeeMonthlyPremium, employerMonthlyPremium, effectiveDate);
        _elections.Add(updated);

        le.Complete();

        RaiseDomainEvent(new BenefitElectionChangedEvent(
            TenantId, Id, EmployeeId, lifeEventId, effectiveDate));

        return updated;
    }

    // ── Termination ───────────────────────────────────────────────────────────

    /// <summary>Terminates the enrollment (employee left, year ended, etc.).</summary>
    public void Terminate()
    {
        if (Status == EnrollmentStatus.Terminated)
            throw new InvalidOperationException("Enrollment is already terminated.");

        Status = EnrollmentStatus.Terminated;
        TerminatedAtUtc = DateTimeOffset.UtcNow;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private LifeEvent FindLifeEvent(LifeEventId id) =>
        _lifeEvents.FirstOrDefault(le => le.Id == id)
        ?? throw new InvalidOperationException($"Life event {id.Value} not found on this enrollment.");
}
