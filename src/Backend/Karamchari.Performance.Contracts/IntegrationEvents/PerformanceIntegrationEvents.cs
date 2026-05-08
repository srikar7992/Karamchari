// Integration event contracts for the Performance Management bounded context.
// Pure records — no domain logic, no EF entities, no framework dependencies.
// Versioned via nested namespaces so consumers can pin to a stable version.

namespace Karamchari.Performance.Contracts.IntegrationEvents;
    /// <summary>
    /// Published when a review cycle is fully completed (calibration approved + locked).
    /// Consumed by: Compensation BC (triggers comp review window), HR BC (records cycle in employee history).
    /// </summary>
    public record ReviewCycleCompletedIntegrationEvent(
        Guid CycleId,
        string TenantId,
        string CycleName,
        DateOnly ReviewPeriodStart,
        DateOnly ReviewPeriodEnd,
        int TotalEmployeesReviewed,
        DateTimeOffset OccurredOnUtc);

    /// <summary>
    /// Published per-employee after calibration session is approved and locked.
    /// Consumed by: Promotion BC (feeds readiness engine), Analytics BC (materializes snapshot).
    /// </summary>
    public record EmployeeCalibrationFinalizedIntegrationEvent(
        Guid SessionId,
        string TenantId,
        Guid EmployeeId,
        Guid CycleId,
        decimal PreCalibrationScore,
        decimal CalibratedScore,
        string PerformanceBucket,
        DateTimeOffset OccurredOnUtc);

    /// <summary>
    /// Published when a promotion is fully approved through all workflow stages.
    /// Consumed by: HR BC (updates grade/title on Employee aggregate), Payroll BC (triggers comp change).
    /// </summary>
    public record PromotionApprovedIntegrationEvent(
        Guid RecommendationId,
        string TenantId,
        Guid EmployeeId,
        string PreviousGrade,
        string NewGrade,
        string PreviousTitle,
        string NewTitle,
        DateOnly EffectiveDate,
        DateTimeOffset OccurredOnUtc);

    /// <summary>
    /// Published when a compensation recommendation is approved in the promotion pipeline.
    /// Consumed by: Payroll BC (schedules comp change effective from EffectiveDate).
    /// Compensation.Contracts will eventually supersede this with a richer event.
    /// </summary>
    public record CompensationRecommendationApprovedIntegrationEvent(
        Guid RecommendationId,
        string TenantId,
        Guid EmployeeId,
        decimal CurrentCompensation,
        decimal ApprovedCompensation,
        decimal IncrementPercent,
        string ChangeReason,
        DateOnly EffectiveDate,
        DateTimeOffset OccurredOnUtc);

    /// <summary>
    /// Published when a goal cycle is activated (status Draft → Active).
    /// Consumed by: Notification BC (sends enrollment reminders to employees).
    /// </summary>
    public record GoalCycleActivatedIntegrationEvent(
        Guid CycleId,
        string TenantId,
        string CycleName,
        DateOnly StartDate,
        DateOnly EndDate,
        DateTimeOffset OccurredOnUtc);

    /// <summary>
    /// Published when a goal cycle is locked (no more progress updates accepted).
    /// Consumed by: Analytics BC (triggers final score materialization).
    /// </summary>
    public record GoalCycleLockedIntegrationEvent(
        Guid CycleId,
        string TenantId,
        DateTimeOffset OccurredOnUtc);

    /// <summary>
    /// Published when an employee's performance snapshot is materialized after a review cycle.
    /// Consumed by: Retention risk analytics, succession planning.
    /// </summary>
    public record EmployeePerformanceSnapshotMaterializedIntegrationEvent(
        Guid SnapshotId,
        string TenantId,
        Guid EmployeeId,
        Guid CycleId,
        decimal CompositeScore,
        string PerformanceBucket,
        bool IsHighPerformer,
        bool IsAtRetentionRisk,
        DateTimeOffset OccurredOnUtc);
