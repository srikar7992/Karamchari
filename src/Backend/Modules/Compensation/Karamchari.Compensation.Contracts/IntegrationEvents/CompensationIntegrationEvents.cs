// Compensation Planning bounded context â€” integration event contracts.
// PLACEHOLDER: BC not yet implemented. Contracts defined here to allow
// Performance BC to publish toward this namespace and avoid coupling.

namespace Karamchari.Compensation.Contracts.IntegrationEvents;

/// <summary>
/// Published when an employee's compensation is revised through any channel
/// (promotion approval, merit cycle, off-cycle adjustment).
/// Consumed by: Payroll BC (schedules salary change), HR BC (records comp history).
/// </summary>
public record EmployeeCompensationRevisedIntegrationEvent(
    Guid RevisionId,
    string TenantId,
    Guid EmployeeId,
    decimal PreviousAnnualCTC,
    decimal NewAnnualCTC,
    decimal IncrementPercent,
    CompensationRevisionReason Reason,
    string? ReferenceId,
    DateOnly EffectiveDate,
    DateTimeOffset OccurredOnUtc);

/// <summary>Reason for a compensation revision.</summary>
public enum CompensationRevisionReason
{
    /// <summary>Promotion-linked increment.</summary>
    Promotion = 1,
    /// <summary>Annual merit cycle increase.</summary>
    MeritIncrease = 2,
    /// <summary>Market benchmarking adjustment.</summary>
    MarketAdjustment = 3,
    /// <summary>Retention-driven bonus conversion.</summary>
    RetentionBonus = 4,
    /// <summary>Off-cycle adjustment outside merit cycle.</summary>
    OffCycleAdjustment = 5,
    /// <summary>Data correction for a prior error.</summary>
    Correction = 6
}
