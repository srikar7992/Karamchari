// -----------------------------------------------------------------------
// <copyright file="CQRSReadModelConfigurations.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Performance.Domain.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karamchari.Performance.Persistence.Configurations;

internal sealed class ManagerDashboardProjectionConfiguration : IEntityTypeConfiguration<ManagerDashboardProjection>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<ManagerDashboardProjection> b)
    {
        b.ToTable("ManagerDashboardProjections");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        b.Property(x => x.ManagerEmployeeId).IsRequired();
        b.Property(x => x.ReviewCycleId).IsRequired();
        b.Property(x => x.CycleName).IsRequired().HasMaxLength(200);
        b.Property(x => x.GoalCompletionRate).HasPrecision(5, 2);
        b.Property(x => x.LastRefreshedUtc).IsRequired();

        b.HasIndex(x => new { x.TenantId, x.ManagerEmployeeId, x.ReviewCycleId })
            .IsUnique()
            .HasDatabaseName("UIX_ManagerDashboardProjections_Manager_Cycle");
    }
}

internal sealed class ReviewTaskInboxItemConfiguration : IEntityTypeConfiguration<ReviewTaskInboxItem>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<ReviewTaskInboxItem> b)
    {
        b.ToTable("ReviewTaskInboxItems");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        b.Property(x => x.ReviewerEmployeeId).IsRequired();
        b.Property(x => x.ReviewAssignmentId).IsRequired();
        b.Property(x => x.ReviewCycleId).IsRequired();
        b.Property(x => x.CycleName).IsRequired().HasMaxLength(200);
        b.Property(x => x.RevieweeDisplayName).IsRequired().HasMaxLength(200);
        b.Property(x => x.ReviewerRole).IsRequired().HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.LastRefreshedUtc).IsRequired();

        // Primary inbox query: all pending tasks for a reviewer
        b.HasIndex(x => new { x.TenantId, x.ReviewerEmployeeId, x.Priority })
            .HasDatabaseName("IX_ReviewTaskInboxItems_Reviewer_Priority");

        b.HasIndex(x => new { x.TenantId, x.ReviewAssignmentId })
            .IsUnique()
            .HasDatabaseName("UIX_ReviewTaskInboxItems_Assignment");
    }
}

internal sealed class CalibrationBoardProjectionConfiguration : IEntityTypeConfiguration<CalibrationBoardProjection>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<CalibrationBoardProjection> b)
    {
        b.ToTable("CalibrationBoardProjections");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        b.Property(x => x.CalibrationSessionId).IsRequired();
        b.Property(x => x.ReviewCycleId).IsRequired();
        b.Property(x => x.SessionName).IsRequired().HasMaxLength(200);
        b.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.LastRefreshedUtc).IsRequired();

        b.HasIndex(x => new { x.TenantId, x.CalibrationSessionId })
            .IsUnique()
            .HasDatabaseName("UIX_CalibrationBoardProjections_Session");

        b.HasIndex(x => new { x.TenantId, x.ReviewCycleId })
            .HasDatabaseName("IX_CalibrationBoardProjections_Cycle");
    }
}

internal sealed class PromotionPipelineItemConfiguration : IEntityTypeConfiguration<PromotionPipelineItem>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<PromotionPipelineItem> b)
    {
        b.ToTable("PromotionPipelineItems");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        b.Property(x => x.PromotionRecommendationId).IsRequired();
        b.Property(x => x.EmployeeId).IsRequired();
        b.Property(x => x.EmployeeDisplayName).IsRequired().HasMaxLength(200);
        b.Property(x => x.CurrentCareerLevel).IsRequired().HasMaxLength(100);
        b.Property(x => x.ProposedCareerLevel).IsRequired().HasMaxLength(100);
        b.Property(x => x.Department).IsRequired().HasMaxLength(200);
        b.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.CurrentStage).IsRequired().HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.ReadinessScore).HasPrecision(5, 2);
        b.Property(x => x.LastRefreshedUtc).IsRequired();

        b.HasIndex(x => new { x.TenantId, x.PromotionRecommendationId })
            .IsUnique()
            .HasDatabaseName("UIX_PromotionPipelineItems_Recommendation");

        b.HasIndex(x => new { x.TenantId, x.ReviewCycleId, x.Status })
            .HasDatabaseName("IX_PromotionPipelineItems_Cycle_Status");
    }
}

internal sealed class TalentHeatmapEntryConfiguration : IEntityTypeConfiguration<TalentHeatmapEntry>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<TalentHeatmapEntry> b)
    {
        b.ToTable("TalentHeatmapEntries");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        b.Property(x => x.EmployeeId).IsRequired();
        b.Property(x => x.EmployeeDisplayName).IsRequired().HasMaxLength(200);
        b.Property(x => x.Department).IsRequired().HasMaxLength(200);
        b.Property(x => x.CareerLevel).IsRequired().HasMaxLength(100);
        b.Property(x => x.ReviewCycleId).IsRequired();
        b.Property(x => x.CycleName).IsRequired().HasMaxLength(200);
        b.Property(x => x.CompositeScore).HasPrecision(5, 2);
        b.Property(x => x.PerformanceBucket).IsRequired().HasMaxLength(50);
        b.Property(x => x.NineBoxPosition).IsRequired().HasMaxLength(20);
        b.Property(x => x.MaterializedOnUtc).IsRequired();
        b.Property(x => x.LastRefreshedUtc).IsRequired();

        b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.ReviewCycleId })
            .IsUnique()
            .HasDatabaseName("UIX_TalentHeatmapEntries_Employee_Cycle");

        b.HasIndex(x => new { x.TenantId, x.NineBoxPosition, x.ReviewCycleId })
            .HasDatabaseName("IX_TalentHeatmapEntries_NineBox_Cycle");

        b.HasIndex(x => new { x.TenantId, x.Department, x.ReviewCycleId })
            .HasDatabaseName("IX_TalentHeatmapEntries_Dept_Cycle");
    }
}

internal sealed class TeamGoalSummaryConfiguration : IEntityTypeConfiguration<TeamGoalSummary>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<TeamGoalSummary> b)
    {
        b.ToTable("TeamGoalSummaries");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        b.Property(x => x.ManagerEmployeeId).IsRequired();
        b.Property(x => x.GoalCycleId).IsRequired();
        b.Property(x => x.CycleName).IsRequired().HasMaxLength(200);
        b.Property(x => x.Department).IsRequired().HasMaxLength(200);
        b.Property(x => x.CompletionRate).HasPrecision(5, 2);
        b.Property(x => x.AverageProgress).HasPrecision(5, 2);
        b.Property(x => x.LastRefreshedUtc).IsRequired();

        b.HasIndex(x => new { x.TenantId, x.ManagerEmployeeId, x.GoalCycleId })
            .IsUnique()
            .HasDatabaseName("UIX_TeamGoalSummaries_Manager_Cycle");
    }
}

internal sealed class EmployeeSkillInventoryItemConfiguration : IEntityTypeConfiguration<EmployeeSkillInventoryItem>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<EmployeeSkillInventoryItem> b)
    {
        b.ToTable("EmployeeSkillInventoryItems");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        b.Property(x => x.EmployeeId).IsRequired();
        b.Property(x => x.EmployeeDisplayName).IsRequired().HasMaxLength(200);
        b.Property(x => x.Department).IsRequired().HasMaxLength(200);
        b.Property(x => x.CareerLevel).IsRequired().HasMaxLength(100);
        b.Property(x => x.SkillId).IsRequired();
        b.Property(x => x.SkillName).IsRequired().HasMaxLength(200);
        b.Property(x => x.SkillCategoryName).IsRequired().HasMaxLength(200);
        b.Property(x => x.Domain).IsRequired().HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.CurrentProficiency).IsRequired().HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.TargetProficiency).HasConversion<string?>().HasMaxLength(30);
        b.Property(x => x.LastAssessmentSource).IsRequired().HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.LastAssessedOnUtc).IsRequired();
        b.Property(x => x.LastRefreshedUtc).IsRequired();

        b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.SkillId })
            .IsUnique()
            .HasDatabaseName("UIX_EmployeeSkillInventoryItems_Employee_Skill");

        // Talent discovery: find employees with skill X at proficiency Y
        b.HasIndex(x => new { x.TenantId, x.SkillId, x.CurrentProficiency })
            .HasDatabaseName("IX_EmployeeSkillInventoryItems_Skill_Proficiency");

        b.HasIndex(x => new { x.TenantId, x.Department, x.Domain })
            .HasDatabaseName("IX_EmployeeSkillInventoryItems_Dept_Domain");
    }
}
