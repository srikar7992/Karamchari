using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Absence;

public enum AbsenceType
{
    NoCallNoShow = 1,
    UnauthorizedAbsence = 2,
    MedicalRestriction = 3,
    Suspension = 4,
    AdministrativeLeave = 5,
    UnplannedAbsence = 6,
    AwolAbsence = 7,
}

public enum AbsenceCaseStatus
{
    Open = 1,
    InReview = 2,
    DocumentationPending = 3,
    HrIntervention = 4,
    Resolved = 5,
    Closed = 6,
    Escalated = 7,
}

public enum AbsenceResolution
{
    ExcusedWithPay = 1,
    ExcusedWithoutPay = 2,
    DisciplinaryAction = 3,
    MedicalLeaveConverted = 4,
    Dismissed = 5,
    Terminated = 6,
}

/// <summary>
/// Unplanned/unauthorized absence management — separate domain from leave.
/// Different lifecycle (incident-driven vs request-driven).
/// Different payroll: NCNS → LOP or disciplinary deduction.
/// Different compliance: AWOL triggers separate statutory process.
/// Different analytics: feeds Bradford Factor, burnout, disciplinary dashboards.
/// </summary>
public sealed class AbsenceCase : AggregateRoot<Guid>, ITenantOwned
{
    private AbsenceCase() { TenantId = string.Empty; }

    public string TenantId { get; private set; }
    public string CaseNumber { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid? ManagerId { get; private set; }
    public AbsenceType AbsenceType { get; private set; }
    public AbsenceCaseStatus Status { get; private set; }
    public DateOnly AbsenceDate { get; private set; }
    public DateOnly? AbsenceEndDate { get; private set; }
    public decimal AbsenceDays { get; private set; }
    public string? EmployeeExplanation { get; private set; }
    public bool ContactAttempted { get; private set; }
    public DateTimeOffset? ContactAttemptedAt { get; private set; }
    public string? ContactAttemptedBy { get; private set; }
    public bool DocumentationReceived { get; private set; }
    public AbsenceResolution? Resolution { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public string? ResolvedBy { get; private set; }
    public bool PayrollImpact { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static AbsenceCase Raise(
        string tenantId,
        string caseNumber,
        Guid employeeId,
        Guid? managerId,
        AbsenceType absenceType,
        DateOnly absenceDate,
        decimal absenceDays)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(absenceDays);

        return new AbsenceCase
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CaseNumber = caseNumber,
            EmployeeId = employeeId,
            ManagerId = managerId,
            AbsenceType = absenceType,
            Status = AbsenceCaseStatus.Open,
            AbsenceDate = absenceDate,
            AbsenceDays = absenceDays,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void RecordContactAttempt(string by)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(by);
        ContactAttempted = true;
        ContactAttemptedAt = DateTimeOffset.UtcNow;
        ContactAttemptedBy = by;
    }

    public void ReceiveExplanation(string explanation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(explanation);
        EmployeeExplanation = explanation;
        Status = AbsenceCaseStatus.InReview;
    }

    public void RequestDocumentation()
    {
        Status = AbsenceCaseStatus.DocumentationPending;
    }

    public void ReceiveDocumentation()
    {
        DocumentationReceived = true;
        Status = AbsenceCaseStatus.InReview;
    }

    public void EscalateToHr() => Status = AbsenceCaseStatus.HrIntervention;

    public void Escalate() => Status = AbsenceCaseStatus.Escalated;

    public void SetEndDate(DateOnly endDate, decimal totalDays)
    {
        AbsenceEndDate = endDate;
        AbsenceDays = totalDays;
    }

    public void Resolve(string resolvedBy, AbsenceResolution resolution, string notes, bool payrollImpact)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resolvedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(notes);

        Status = AbsenceCaseStatus.Resolved;
        Resolution = resolution;
        ResolutionNotes = notes;
        ResolvedBy = resolvedBy;
        ResolvedAt = DateTimeOffset.UtcNow;
        PayrollImpact = payrollImpact;
    }

    public void Close()
    {
        if (Status != AbsenceCaseStatus.Resolved)
            throw new InvalidOperationException("Absence case must be resolved before closing.");
        Status = AbsenceCaseStatus.Closed;
    }
}

/// <summary>
/// Return-to-work assessment required after absence exceeding threshold (e.g. 3+ consecutive days sick).
/// Documents fitness-for-work clearance, any work restrictions, and phased return plan.
/// </summary>
public sealed class ReturnToWorkAssessment : Entity<Guid>, ITenantOwned
{
    private ReturnToWorkAssessment() { TenantId = string.Empty; }

    public string TenantId { get; private set; }
    public Guid AbsenceCaseId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateOnly ReturnDate { get; private set; }
    public bool FitnessConfirmed { get; private set; }
    public bool PhasedReturnRequired { get; private set; }
    public string? PhasedReturnPlan { get; private set; }
    public bool WorkRestrictionsApply { get; private set; }
    public string? WorkRestrictions { get; private set; }
    public string ConductedBy { get; private set; } = string.Empty;
    public DateTimeOffset ConductedAt { get; private set; }

    private ReturnToWorkAssessment(Guid id) : base(id) { TenantId = string.Empty; }

    public static ReturnToWorkAssessment Conduct(
        string tenantId,
        Guid absenceCaseId,
        Guid employeeId,
        DateOnly returnDate,
        bool fitnessConfirmed,
        bool phasedReturnRequired,
        string? phasedReturnPlan,
        bool workRestrictionsApply,
        string? workRestrictions,
        string conductedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(conductedBy);

        return new ReturnToWorkAssessment(Guid.NewGuid())
        {
            TenantId = tenantId,
            AbsenceCaseId = absenceCaseId,
            EmployeeId = employeeId,
            ReturnDate = returnDate,
            FitnessConfirmed = fitnessConfirmed,
            PhasedReturnRequired = phasedReturnRequired,
            PhasedReturnPlan = phasedReturnPlan,
            WorkRestrictionsApply = workRestrictionsApply,
            WorkRestrictions = workRestrictions,
            ConductedBy = conductedBy,
            ConductedAt = DateTimeOffset.UtcNow,
        };
    }
}

/// <summary>
/// Medical restriction placed on an employee's role/tasks (separate from leave).
/// Example: back injury → no heavy lifting for 4 weeks. Employee still present, but restricted.
/// Feeds rostering engine (don't assign restricted shifts).
/// </summary>
public sealed class MedicalRestriction : Entity<Guid>, ITenantOwned
{
    private MedicalRestriction() { TenantId = string.Empty; }

    public string TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string RestrictionDescription { get; private set; } = string.Empty;
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; }
    public string IssuedBy { get; private set; } = string.Empty;
    public bool RosteringRestriction { get; private set; }

    private MedicalRestriction(Guid id) : base(id) { TenantId = string.Empty; }

    public static MedicalRestriction Create(
        string tenantId,
        Guid employeeId,
        string restrictionDescription,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        string issuedBy,
        bool rosteringRestriction = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(restrictionDescription);
        ArgumentException.ThrowIfNullOrWhiteSpace(issuedBy);

        return new MedicalRestriction(Guid.NewGuid())
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            RestrictionDescription = restrictionDescription,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            IssuedBy = issuedBy,
            RosteringRestriction = rosteringRestriction,
            IsActive = true,
        };
    }

    public bool IsActiveOn(DateOnly date) =>
        IsActive && EffectiveFrom <= date && (EffectiveTo == null || EffectiveTo >= date);

    public void Lift() => IsActive = false;
}
