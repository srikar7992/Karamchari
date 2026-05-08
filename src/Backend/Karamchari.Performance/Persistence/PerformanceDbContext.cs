using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Performance.Domain.Career;
using Karamchari.Performance.Domain.Calibration;
using Karamchari.Performance.Domain.Feedback;
using Karamchari.Performance.Domain.Goals;
using Karamchari.Performance.Domain.KPIs;
using Karamchari.Performance.Domain.OKRs;
using Karamchari.Performance.Domain.Promotions;
using Karamchari.Performance.Domain.ReadModels;
using Karamchari.Performance.Domain.Reviews;
using Karamchari.Performance.Domain.Skills;
using Karamchari.Performance.Persistence.Configurations;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Performance.Persistence;

/// <summary>
/// Database context for the Performance bounded context.
///
/// Split roadmap: See ADR-0007. Before year 2, consider splitting into:
///   Performance.Core  (goals, OKRs, KPIs, review cycles, submissions)
///   Performance.Reviews (calibration, promotions, snapshots)
///   Performance.Career (skills, career framework, growth plans)
/// Do NOT split prematurely — wait for migration or deployment velocity pain.
/// </summary>
public sealed class PerformanceDbContext : KaramchariDbContext
{
    private const string MessagingSchema = "dbo";

    public PerformanceDbContext(DbContextOptions<PerformanceDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    // Goals
    public DbSet<GoalCycle> GoalCycles => Set<GoalCycle>();
    public DbSet<Goal> Goals => Set<Goal>();

    // OKRs
    public DbSet<OKRCycle> OKRCycles => Set<OKRCycle>();
    public DbSet<Objective> Objectives => Set<Objective>();

    // KPIs
    public DbSet<KPIDefinition> KPIDefinitions => Set<KPIDefinition>();
    public DbSet<KPIResult> KPIResults => Set<KPIResult>();
    public DbSet<KPISnapshot> KPISnapshots => Set<KPISnapshot>();

    // Reviews
    public DbSet<ReviewTemplate> ReviewTemplates => Set<ReviewTemplate>();
    public DbSet<ReviewCycle> ReviewCycles => Set<ReviewCycle>();
    public DbSet<ReviewAssignment> ReviewAssignments => Set<ReviewAssignment>();
    public DbSet<ReviewSubmission> ReviewSubmissions => Set<ReviewSubmission>();

    // Feedback
    public DbSet<FeedbackRequest> FeedbackRequests => Set<FeedbackRequest>();
    public DbSet<FeedbackSubmission> FeedbackSubmissions => Set<FeedbackSubmission>();

    // Calibration
    public DbSet<CalibrationSession> CalibrationSessions => Set<CalibrationSession>();

    // Promotions
    public DbSet<PromotionRecommendation> PromotionRecommendations => Set<PromotionRecommendation>();

    // Skills
    public DbSet<SkillTaxonomy> SkillTaxonomies => Set<SkillTaxonomy>();
    public DbSet<EmployeeSkillProfile> EmployeeSkillProfiles => Set<EmployeeSkillProfile>();

    // Career
    public DbSet<CareerFramework> CareerFrameworks => Set<CareerFramework>();
    public DbSet<EmployeeGrowthPlan> EmployeeGrowthPlans => Set<EmployeeGrowthPlan>();

    // Analytics read models + CQRS projections (ADR-0009)
    public DbSet<PerformanceSnapshot> PerformanceSnapshots => Set<PerformanceSnapshot>();
    public DbSet<ManagerDashboardProjection> ManagerDashboardProjections => Set<ManagerDashboardProjection>();
    public DbSet<ReviewTaskInboxItem> ReviewTaskInboxItems => Set<ReviewTaskInboxItem>();
    public DbSet<CalibrationBoardProjection> CalibrationBoardProjections => Set<CalibrationBoardProjection>();
    public DbSet<PromotionPipelineItem> PromotionPipelineItems => Set<PromotionPipelineItem>();
    public DbSet<TalentHeatmapEntry> TalentHeatmapEntries => Set<TalentHeatmapEntry>();
    public DbSet<TeamGoalSummary> TeamGoalSummaries => Set<TeamGoalSummary>();
    public DbSet<EmployeeSkillInventoryItem> EmployeeSkillInventoryItems => Set<EmployeeSkillInventoryItem>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

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

        // CQRS projections (ADR-0009)
        modelBuilder.ApplyConfiguration(new ManagerDashboardProjectionConfiguration());
        modelBuilder.ApplyConfiguration(new ReviewTaskInboxItemConfiguration());
        modelBuilder.ApplyConfiguration(new CalibrationBoardProjectionConfiguration());
        modelBuilder.ApplyConfiguration(new PromotionPipelineItemConfiguration());
        modelBuilder.ApplyConfiguration(new TalentHeatmapEntryConfiguration());
        modelBuilder.ApplyConfiguration(new TeamGoalSummaryConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeSkillInventoryItemConfiguration());

        // MassTransit transactional outbox — shared infrastructure, pinned to dbo
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<InboxState>(b => b.ToTable("InboxState", MessagingSchema));
        modelBuilder.Entity<OutboxMessage>(b => b.ToTable("OutboxMessage", MessagingSchema));
        modelBuilder.Entity<OutboxState>(b => b.ToTable("OutboxState", MessagingSchema));
    }
}
