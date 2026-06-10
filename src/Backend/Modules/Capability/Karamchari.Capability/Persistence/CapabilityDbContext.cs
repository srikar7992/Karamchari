using Karamchari.Capability.Domain.Entitlements;
using Karamchari.Capability.Domain.Growth;
using Karamchari.Capability.Domain.Learning;
using Karamchari.Capability.Domain.Marketplace;
using Karamchari.Capability.Domain.Mobility;
using Karamchari.Capability.Domain.Pathing;
using Karamchari.Capability.Domain.Skills;
using Karamchari.Capability.Domain.Succession;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Capability.Persistence;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public class CapabilityDbContext : KaramchariDbContext
{
    /// <inheritdoc/>
    public CapabilityDbContext(DbContextOptions<CapabilityDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<SkillDefinition> SkillDefinitions => Set<SkillDefinition>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<CapabilityProfile> CapabilityProfiles => Set<CapabilityProfile>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<LearningModule> LearningModules => Set<LearningModule>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<LearningEnrollment> LearningEnrollments => Set<LearningEnrollment>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<CertificationAchievement> CertificationAchievements => Set<CertificationAchievement>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<GrowthPlan> GrowthPlans => Set<GrowthPlan>();

    public DbSet<CapabilityDefinition> CapabilityDefinitions => Set<CapabilityDefinition>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<TenantCapability> TenantCapabilities => Set<TenantCapability>();
    public DbSet<RoleSkillRequirement> RoleSkillRequirements => Set<RoleSkillRequirement>();
    public DbSet<SkillCategory> SkillCategories => Set<SkillCategory>();
    public DbSet<CertificationDefinition> CertificationDefinitions => Set<CertificationDefinition>();
    public DbSet<SkillEvidence> SkillEvidences => Set<SkillEvidence>();
    public DbSet<SkillNode> SkillNodes => Set<SkillNode>();
    public DbSet<SkillGraphRelationship> SkillGraphRelationships => Set<SkillGraphRelationship>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<OpportunityParticipant> OpportunityParticipants => Set<OpportunityParticipant>();
    public DbSet<EmployeeCapabilityProjection> EmployeeCapabilityProjections => Set<EmployeeCapabilityProjection>();

    /// <summary>Pre-computed skill coverage scores per employee per role requirement.</summary>
    public DbSet<EmployeeSkillCoverageProjection> EmployeeSkillCoverageProjections => Set<EmployeeSkillCoverageProjection>();

    /// <summary>Per-skill gap rows for employees below a required level. Row absent = gap closed.</summary>
    public DbSet<EmployeeSkillGapProjection> EmployeeSkillGapProjections => Set<EmployeeSkillGapProjection>();

    /// <summary>Career readiness ranking rows combining coverage and critical-gap weighting.</summary>
    public DbSet<CareerReadinessProjection> CareerReadinessProjections => Set<CareerReadinessProjection>();

    /// <summary>Lightweight index of open vacancies that have a linked role skill requirement.</summary>
    public DbSet<MobilityVacancy> MobilityVacancies => Set<MobilityVacancy>();

    /// <summary>Pre-computed mobility scores for employees against open vacancies. Row absent = vacancy closed.</summary>
    public DbSet<InternalMobilityProjection> InternalMobilityProjections => Set<InternalMobilityProjection>();

    /// <summary>Directed role progression edges used for BFS career path traversal.</summary>
    public DbSet<CareerProgressionEdge> CareerProgressionEdges => Set<CareerProgressionEdge>();

    /// <summary>Positions designated as critical for succession planning.</summary>
    public DbSet<CriticalPosition> CriticalPositions => Set<CriticalPosition>();

    /// <summary>Pre-computed successor readiness per employee per critical position.</summary>
    public DbSet<SuccessorCandidateProjection> SuccessorCandidateProjections => Set<SuccessorCandidateProjection>();

    /// <inheritdoc/>
    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.Entity<SkillDefinition>(b =>
        {
            b.ToTable("Capability_SkillDefinitions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            b.Property(x => x.RowVersion).IsRowVersion();
            b.Property(x => x.CategoryId);
        });

        modelBuilder.Entity<CapabilityProfile>(b =>
        {
            b.ToTable("Capability_Profiles");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId }).IsUnique();
            b.Property(x => x.RowVersion).IsRowVersion();

            b.OwnsMany(x => x.Skills, s =>
            {
                s.ToTable("Capability_VerifiedSkills");
                s.WithOwner().HasForeignKey("ProfileId");
                s.HasKey(x => x.Id);
                s.HasIndex(x => new { x.ProfileId, x.SkillId }).IsUnique();
                s.Property(x => x.Level).HasConversion<string>();
                s.Property(x => x.ValidUntil);
            });
        });

        modelBuilder.Entity<LearningModule>(b =>
        {
            b.ToTable("Capability_LearningModules");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Title });
            b.Property(x => x.RowVersion).IsRowVersion();

            b.OwnsMany(x => x.SkillTargets, t =>
            {
                t.ToTable("Capability_ModuleSkillTargets");
                t.WithOwner().HasForeignKey("ModuleId");
                t.HasKey(x => x.Id);
                t.HasIndex(x => new { x.ModuleId, x.SkillId }).IsUnique();
                t.Property(x => x.GrantedLevel).HasConversion<string>();
            });
        });

        modelBuilder.Entity<LearningEnrollment>(b =>
        {
            b.ToTable("Capability_LearningEnrollments");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.ModuleId }).IsUnique();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.RowVersion).IsRowVersion();
            b.Property(x => x.TargetSkillId);
        });

        modelBuilder.Entity<CertificationAchievement>(b =>
        {
            b.ToTable("Capability_CertificationAchievements");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId });
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.RowVersion).IsRowVersion();
            b.Property(x => x.CertificationDefinitionId);
        });

        modelBuilder.Entity<GrowthPlan>(b =>
        {
            b.ToTable("Capability_GrowthPlans");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId });
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.RowVersion).IsRowVersion();

            b.OwnsMany(x => x.Milestones, m =>
            {
                m.ToTable("Capability_GrowthMilestones");
                m.WithOwner().HasForeignKey("GrowthPlanId");
                m.HasKey(x => x.Id);
            });
        });

        modelBuilder.Entity<CapabilityDefinition>(b =>
        {
            b.ToTable("Capability_Definitions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.CapabilityId).IsUnique();
            b.Property(x => x.Tier).HasConversion<string>();
        });

        modelBuilder.Entity<TenantCapability>(b =>
        {
            b.ToTable("Capability_TenantEntitlements");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.CapabilityId }).IsUnique();
            b.Property(x => x.Status).HasConversion<string>();
        });

        modelBuilder.Entity<RoleSkillRequirement>(b =>
        {
            b.ToTable("Capability_RoleSkillRequirements");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.RoleTitle });
            b.Property(x => x.RoleTitle).IsRequired().HasMaxLength(200);
            b.Property(x => x.JobFamily).HasMaxLength(100);
            b.Property(x => x.Department).HasMaxLength(100);

            b.OwnsMany(x => x.Skills, s =>
            {
                s.ToTable("Capability_RequiredSkills");
                s.WithOwner().HasForeignKey("RequirementId");
                s.HasKey(x => x.Id);
                s.Property(x => x.MinimumLevel).HasConversion<string>();
                s.HasIndex(x => new { x.RequirementId, x.SkillId }).IsUnique();
            });
        });

        modelBuilder.Entity<SkillCategory>(b =>
        {
            b.ToTable("Capability_SkillCategories");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            b.Property(x => x.Name).IsRequired().HasMaxLength(100);
            b.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<CertificationDefinition>(b =>
        {
            b.ToTable("Capability_CertificationDefinitions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Name });
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.Issuer).IsRequired().HasMaxLength(200);
            b.Property(x => x.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<SkillEvidence>(b =>
        {
            b.ToTable("Capability_SkillEvidences");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.EvidenceType).HasMaxLength(128).IsRequired();
            b.Property(x => x.Description).HasMaxLength(1000);
            b.Property(x => x.MetadataJson).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.EmployeeId }).HasDatabaseName("IX_SkillEvidence_EmployeeId");
            b.HasIndex(x => new { x.TenantId, x.SkillId }).HasDatabaseName("IX_SkillEvidence_SkillId");
        });

        modelBuilder.Entity<SkillNode>(b =>
        {
            b.ToTable("Capability_SkillNodes");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.SkillName).HasMaxLength(200).IsRequired();
            b.Property(x => x.SkillCluster).HasMaxLength(200).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.SkillName }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.SkillCluster });
        });

        modelBuilder.Entity<SkillGraphRelationship>(b =>
        {
            b.ToTable("Capability_SkillGraphRelationships");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.RelationshipType).HasMaxLength(64).IsRequired();
            b.Property(x => x.Weight).HasPrecision(5, 2);
            b.HasIndex(x => new { x.TenantId, x.SourceSkillId });
            b.HasIndex(x => new { x.TenantId, x.TargetSkillId });
        });

        modelBuilder.Entity<Opportunity>(b =>
        {
            b.ToTable("Capability_Opportunities");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.Title).HasMaxLength(256).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.ApprovalWorkflow).HasMaxLength(128);
            b.Property(x => x.Type).HasConversion<string>();
            b.Property(x => x.Status).HasConversion<string>();
            b.HasIndex(x => new { x.TenantId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.ManagerId });
            b.HasIndex(x => new { x.TenantId, x.Type });

            b.OwnsMany(x => x.Requirements, r =>
            {
                r.ToTable("Capability_OpportunityRequirements");
                r.WithOwner().HasForeignKey("OpportunityId");
                r.HasKey(x => x.Id);
                r.HasIndex(x => new { x.OpportunityId, x.SkillId });
            });
        });

        modelBuilder.Entity<OpportunityParticipant>(b =>
        {
            b.ToTable("Capability_OpportunityParticipants");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.Status).HasConversion<string>();
            b.HasIndex(x => new { x.TenantId, x.OpportunityId, x.EmployeeId }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.EmployeeId });
        });

        modelBuilder.Entity<EmployeeCapabilityProjection>(b =>
        {
            b.ToTable("Capability_EmployeeCapabilityProjections");
            b.HasKey(x => new { x.TenantId, x.EmployeeId });
            b.Property(x => x.EmployeeName).HasMaxLength(256).IsRequired();
            b.Property(x => x.VerifiedSkills).HasConversion(
                v => string.Join("|||", v),
                v => string.IsNullOrEmpty(v) ? new List<string>() : v.Split("|||", StringSplitOptions.None).ToList());
        });

        modelBuilder.Entity<EmployeeSkillCoverageProjection>(b =>
        {
            b.ToTable("Capability_EmployeeSkillCoverageProjections");
            b.HasKey(x => new { x.TenantId, x.EmployeeId, x.RoleRequirementId });
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.RoleTitle).HasMaxLength(200).IsRequired();
            b.Property(x => x.CoveragePercent).HasPrecision(9, 4);
            // Mobility lookup: top N employees by coverage for a role
            b.HasIndex(x => new { x.TenantId, x.RoleRequirementId, x.CoveragePercent })
                .HasDatabaseName("IX_SkillCoverage_RoleRanking");
            // Gap analysis: all role gaps for a specific employee
            b.HasIndex(x => new { x.TenantId, x.EmployeeId })
                .HasDatabaseName("IX_SkillCoverage_EmployeeGaps");
        });

        modelBuilder.Entity<EmployeeSkillGapProjection>(b =>
        {
            b.ToTable("Capability_EmployeeSkillGapProjections");
            b.HasKey(x => new { x.TenantId, x.EmployeeId, x.RoleRequirementId, x.SkillId });
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.RoleTitle).HasMaxLength(200).IsRequired();
            b.Property(x => x.RequiredLevel).HasConversion<string>().HasMaxLength(32).IsRequired();
            b.Property(x => x.CurrentLevel).HasConversion<string>().HasMaxLength(32);
            // Career dashboard: all active gaps for an employee
            b.HasIndex(x => new { x.TenantId, x.EmployeeId })
                .HasDatabaseName("IX_SkillGap_EmployeeGaps");
            // Role gap analysis: which skills are commonly missing for a role
            b.HasIndex(x => new { x.TenantId, x.RoleRequirementId, x.SkillId })
                .HasDatabaseName("IX_SkillGap_RoleSkillGap");
        });

        modelBuilder.Entity<CareerReadinessProjection>(b =>
        {
            b.ToTable("Capability_CareerReadinessProjections");
            b.HasKey(x => new { x.TenantId, x.EmployeeId, x.RoleRequirementId });
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.RoleTitle).HasMaxLength(200).IsRequired();
            b.Property(x => x.CoveragePercent).HasPrecision(9, 4);
            b.Property(x => x.ReadinessScore).HasPrecision(9, 4);
            b.Property(x => x.Band).HasConversion<string>().HasMaxLength(32).IsRequired();
            // Mobility / succession ranking: find ReadyNow employees for a role, ordered by score
            b.HasIndex(x => new { x.TenantId, x.RoleRequirementId, x.Band, x.ReadinessScore })
                .HasDatabaseName("IX_CareerReadiness_RoleRanking");
            // Employee career dashboard: all roles with readiness for one employee
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Band })
                .HasDatabaseName("IX_CareerReadiness_EmployeeBand");
        });

        modelBuilder.Entity<MobilityVacancy>(b =>
        {
            b.ToTable("Capability_MobilityVacancies");
            b.HasKey(x => new { x.TenantId, x.VacancyId });
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            // Lookup: which open vacancies target a given role requirement?
            b.HasIndex(x => new { x.TenantId, x.RoleRequirementId })
                .HasDatabaseName("IX_MobilityVacancy_RoleRequirement");
        });

        modelBuilder.Entity<InternalMobilityProjection>(b =>
        {
            b.ToTable("Capability_InternalMobilityProjections");
            b.HasKey(x => new { x.TenantId, x.EmployeeId, x.VacancyId });
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.CoveragePercent).HasPrecision(9, 4);
            b.Property(x => x.ReadinessScore).HasPrecision(9, 4);
            b.Property(x => x.Band).HasConversion<string>().HasMaxLength(32).IsRequired();
            // Hiring manager: top ReadyNow candidates for a vacancy, ordered by score
            b.HasIndex(x => new { x.TenantId, x.VacancyId, x.Band, x.ReadinessScore })
                .HasDatabaseName("IX_Mobility_VacancyRanking");
            // Employee marketplace: all vacancies the employee is eligible for
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Eligible, x.ReadinessScore })
                .HasDatabaseName("IX_Mobility_EmployeeOpportunities");
        });

        modelBuilder.Entity<CareerProgressionEdge>(b =>
        {
            b.ToTable("Capability_CareerProgressionEdges");
            b.HasKey(x => new { x.TenantId, x.FromRoleRequirementId, x.ToRoleRequirementId });
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.Weight).HasPrecision(9, 4);
            // Graph traversal: outbound edges from a role
            b.HasIndex(x => new { x.TenantId, x.FromRoleRequirementId })
                .HasDatabaseName("IX_CareerPath_FromRole");
            // Reverse lookup: inbound edges to a role (useful for "what leads here?")
            b.HasIndex(x => new { x.TenantId, x.ToRoleRequirementId })
                .HasDatabaseName("IX_CareerPath_ToRole");
        });

        modelBuilder.Entity<CriticalPosition>(b =>
        {
            b.ToTable("Capability_CriticalPositions");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.Criticality).HasConversion<string>().HasMaxLength(32).IsRequired();
            // Uniqueness: one critical-position record per position per tenant
            b.HasIndex(x => new { x.TenantId, x.PositionId })
                .IsUnique()
                .HasDatabaseName("IX_CriticalPosition_Tenant_Position");
            // Succession handler lookup: positions linked to a given role requirement
            b.HasIndex(x => new { x.TenantId, x.RoleRequirementId })
                .HasDatabaseName("IX_CriticalPosition_RoleRequirement");
        });

        modelBuilder.Entity<SuccessorCandidateProjection>(b =>
        {
            b.ToTable("Capability_SuccessorCandidateProjections");
            b.HasKey(x => new { x.TenantId, x.PositionId, x.EmployeeId });
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.ReadinessScore).HasPrecision(9, 4);
            b.Property(x => x.Band).HasConversion<string>().HasMaxLength(32).IsRequired();
            // Executive bench view: ordered candidate list for a position
            b.HasIndex(x => new { x.TenantId, x.PositionId, x.Band, x.Rank })
                .HasDatabaseName("IX_SuccessorCandidate_PositionRanking");
            // Employee succession pool view: all positions this employee is tracked for
            b.HasIndex(x => new { x.TenantId, x.EmployeeId })
                .HasDatabaseName("IX_SuccessorCandidate_EmployeePools");
        });
    }
}
