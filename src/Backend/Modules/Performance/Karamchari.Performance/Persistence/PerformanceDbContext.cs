using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Performance.Domain.Calibration;
using Karamchari.Performance.Domain.Career;
using Karamchari.Performance.Domain.Feedback;
using Karamchari.Performance.Domain.Goals;
using Karamchari.Performance.Domain.KPIs;
using Karamchari.Performance.Domain.OKRs;
using Karamchari.Performance.Domain.Promotions;
using Karamchari.Performance.Domain.ReadModels;
using Karamchari.Performance.Domain.Reporting;
using Karamchari.Performance.Domain.Reviews;
using Karamchari.Performance.Domain.Skills;
using Karamchari.Performance.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Performance.Persistence;

/// <summary>
/// Database context for the Performance bounded context.
///
/// Split roadmap: See ADR-0007. Before year 2, consider splitting into:
///   Performance.Core  (goals, OKRs, KPIs, review cycles, submissions)
///   Performance.Reviews (calibration, promotions, snapshots)
///   Performance.Career (skills, career framework, growth plans)
/// Do NOT split prematurely â€” wait for migration or deployment velocity pain.
/// </summary>
public sealed class PerformanceDbContext : KaramchariDbContext
{
    public PerformanceDbContext(DbContextOptions<PerformanceDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    // Goals
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<GoalCycle> GoalCycles => Set<GoalCycle>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<Goal> Goals => Set<Goal>();

    // OKRs
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<OKRCycle> OKRCycles => Set<OKRCycle>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<Objective> Objectives => Set<Objective>();

    // KPIs
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<KPIDefinition> KPIDefinitions => Set<KPIDefinition>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<KPIResult> KPIResults => Set<KPIResult>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<KPISnapshot> KPISnapshots => Set<KPISnapshot>();

    // Reviews
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<ReviewTemplate> ReviewTemplates => Set<ReviewTemplate>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<ReviewCycle> ReviewCycles => Set<ReviewCycle>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<ReviewAssignment> ReviewAssignments => Set<ReviewAssignment>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<ReviewSubmission> ReviewSubmissions => Set<ReviewSubmission>();

    // Feedback
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<FeedbackRequest> FeedbackRequests => Set<FeedbackRequest>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<FeedbackSubmission> FeedbackSubmissions => Set<FeedbackSubmission>();
    public DbSet<InFlowFeedback> InFlowFeedbacks => Set<InFlowFeedback>();

    // Calibration
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<CalibrationSession> CalibrationSessions => Set<CalibrationSession>();

    // Promotions
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<PromotionRecommendation> PromotionRecommendations => Set<PromotionRecommendation>();

    // Skills
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<SkillTaxonomy> SkillTaxonomies => Set<SkillTaxonomy>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<EmployeeSkillProfile> EmployeeSkillProfiles => Set<EmployeeSkillProfile>();

    // Career
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<CareerFramework> CareerFrameworks => Set<CareerFramework>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<EmployeeGrowthPlan> EmployeeGrowthPlans => Set<EmployeeGrowthPlan>();

    // Reporting (ADR-0013)
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<ExportJob> ExportJobs => Set<ExportJob>();

    // Analytics read models + CQRS projections (ADR-0009)
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<PerformanceSnapshot> PerformanceSnapshots => Set<PerformanceSnapshot>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<ManagerDashboardProjection> ManagerDashboardProjections => Set<ManagerDashboardProjection>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<ReviewTaskInboxItem> ReviewTaskInboxItems => Set<ReviewTaskInboxItem>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<CalibrationBoardProjection> CalibrationBoardProjections => Set<CalibrationBoardProjection>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<PromotionPipelineItem> PromotionPipelineItems => Set<PromotionPipelineItem>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<TalentHeatmapEntry> TalentHeatmapEntries => Set<TalentHeatmapEntry>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<TeamGoalSummary> TeamGoalSummaries => Set<TeamGoalSummary>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<EmployeeSkillInventoryItem> EmployeeSkillInventoryItems => Set<EmployeeSkillInventoryItem>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        // Goals
        modelBuilder.ApplyConfiguration(new GoalCycleConfiguration());
        modelBuilder.ApplyConfiguration(new GoalConfiguration());

        // OKRs
        modelBuilder.ApplyConfiguration(new OKRCycleConfiguration());
        modelBuilder.ApplyConfiguration(new ObjectiveConfiguration());

        // KPIs
        modelBuilder.ApplyConfiguration(new KPIDefinitionConfiguration());
        modelBuilder.ApplyConfiguration(new KPIResultConfiguration());
        modelBuilder.ApplyConfiguration(new KPISnapshotConfiguration());

        // Reviews
        modelBuilder.ApplyConfiguration(new ReviewTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new ReviewCycleConfiguration());
        modelBuilder.ApplyConfiguration(new ReviewAssignmentConfiguration());
        modelBuilder.ApplyConfiguration(new ReviewSubmissionConfiguration());

        // Feedback
        modelBuilder.ApplyConfiguration(new FeedbackRequestConfiguration());
        modelBuilder.ApplyConfiguration(new FeedbackSubmissionConfiguration());

        modelBuilder.Entity<InFlowFeedback>(b =>
        {
            b.ToTable("Performance_InFlowFeedbacks");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.SourceChannel).HasMaxLength(64).IsRequired();
            b.Property(x => x.Content).IsRequired();
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => new { x.TenantId, x.SenderEmployeeId });
            b.HasIndex(x => new { x.TenantId, x.ReceiverEmployeeId });
        });

        // Calibration
        modelBuilder.ApplyConfiguration(new CalibrationSessionConfiguration());

        // Promotions
        modelBuilder.ApplyConfiguration(new PromotionRecommendationConfiguration());

        // Skills
        modelBuilder.ApplyConfiguration(new SkillTaxonomyConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeSkillProfileConfiguration());

        // Career
        modelBuilder.ApplyConfiguration(new CareerFrameworkConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeGrowthPlanConfiguration());

        // Read models
        modelBuilder.ApplyConfiguration(new PerformanceSnapshotConfiguration());

        // Reporting (ADR-0013)
        modelBuilder.ApplyConfiguration(new ExportJobConfiguration());

        // CQRS projections (ADR-0009)
        modelBuilder.ApplyConfiguration(new ManagerDashboardProjectionConfiguration());
        modelBuilder.ApplyConfiguration(new ReviewTaskInboxItemConfiguration());
        modelBuilder.ApplyConfiguration(new CalibrationBoardProjectionConfiguration());
        modelBuilder.ApplyConfiguration(new PromotionPipelineItemConfiguration());
        modelBuilder.ApplyConfiguration(new TalentHeatmapEntryConfiguration());
        modelBuilder.ApplyConfiguration(new TeamGoalSummaryConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeSkillInventoryItemConfiguration());
    }
}
