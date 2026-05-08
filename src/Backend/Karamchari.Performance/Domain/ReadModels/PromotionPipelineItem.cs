using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Performance.Domain.Promotions;

namespace Karamchari.Performance.Domain.ReadModels;

/// <summary>
/// One row per promotion recommendation. Drives the promotion pipeline board.
/// Includes readiness signals, current approval stage, and days-in-stage.
/// </summary>
public sealed class PromotionPipelineItem : Entity<Guid>, ITenantOwned
{
    private PromotionPipelineItem() { /* EF materialization */ }

    public string TenantId { get; private set; } = string.Empty;
    public Guid PromotionRecommendationId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string EmployeeDisplayName { get; private set; } = string.Empty;
    public string CurrentCareerLevel { get; private set; } = string.Empty;
    public string ProposedCareerLevel { get; private set; } = string.Empty;
    public string Department { get; private set; } = string.Empty;
    public PromotionStatus Status { get; private set; }
    public PromotionApprovalStage CurrentStage { get; private set; }

    /// <summary>0–100 composite readiness score from the promotion engine.</summary>
    public decimal ReadinessScore { get; private set; }

    /// <summary>Number of calendar days in the current approval stage.</summary>
    public int DaysInCurrentStage { get; private set; }

    public bool IsStale => DaysInCurrentStage > 14;

    public Guid ReviewCycleId { get; private set; }
    public DateTimeOffset LastRefreshedUtc { get; private set; }

    public static PromotionPipelineItem Create(
        string tenantId,
        Guid promotionRecommendationId,
        Guid employeeId,
        string employeeDisplayName,
        string currentCareerLevel,
        string proposedCareerLevel,
        string department,
        Guid reviewCycleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeDisplayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentCareerLevel);
        ArgumentException.ThrowIfNullOrWhiteSpace(proposedCareerLevel);
        ArgumentException.ThrowIfNullOrWhiteSpace(department);

        return new PromotionPipelineItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PromotionRecommendationId = promotionRecommendationId,
            EmployeeId = employeeId,
            EmployeeDisplayName = employeeDisplayName,
            CurrentCareerLevel = currentCareerLevel,
            ProposedCareerLevel = proposedCareerLevel,
            Department = department,
            ReviewCycleId = reviewCycleId,
            Status = PromotionStatus.Draft,
            CurrentStage = PromotionApprovalStage.Manager,
            LastRefreshedUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Refresh(
        PromotionStatus status,
        PromotionApprovalStage currentStage,
        decimal readinessScore,
        int daysInCurrentStage)
    {
        Status = status;
        CurrentStage = currentStage;
        ReadinessScore = readinessScore;
        DaysInCurrentStage = daysInCurrentStage;
        LastRefreshedUtc = DateTimeOffset.UtcNow;
    }
}
