// -----------------------------------------------------------------------
// <copyright file="PromotionPipelineItem.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid PromotionRecommendationId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string EmployeeDisplayName { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string CurrentCareerLevel { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string ProposedCareerLevel { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Department { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public PromotionStatus Status { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public PromotionApprovalStage CurrentStage { get; private set; }

    /// <summary>0â€“100 composite readiness score from the promotion engine.</summary>
    public decimal ReadinessScore { get; private set; }

    /// <summary>Number of calendar days in the current approval stage.</summary>
    public int DaysInCurrentStage { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsStale => DaysInCurrentStage > 14;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ReviewCycleId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset LastRefreshedUtc { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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
