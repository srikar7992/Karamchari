// -----------------------------------------------------------------------
// <copyright file="HRDbContext.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.HR.Domain.Departments;
using Karamchari.HR.Domain.Employees;
using Karamchari.HR.Domain.Organization;
using Karamchari.HR.Domain.Organization.Projections;
using Karamchari.HR.Domain.Relationships;
using Karamchari.HR.Persistence.Configurations;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.HR.Persistence;

/// <summary>
/// Database context for the Human Resources bounded context.
/// Inherits from <see cref="KaramchariDbContext"/> to leverage multi-tenancy and RLS.
/// </summary>
public sealed class HRDbContext : KaramchariDbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HRDbContext"/> class.
    /// </summary>
    /// <param name="options">The context options.</param>
    /// <param name="tenantProvider">The tenant provider.</param>
    public HRDbContext(DbContextOptions<HRDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    /// <summary>
    /// Schema where MassTransit's transactional outbox tables live. They're
    /// shared infrastructure Ã¢â‚¬â€ one queue serves every tenant and the relay
    /// process drains them globally, so they must NOT be subject to per-tenant
    /// schema rewriting.
    /// </summary>
    private const string MessagingSchema = "dbo";

    /// <summary>
    /// Gets the departments set.
    /// </summary>
    public DbSet<Department> Departments => Set<Department>();

    public DbSet<Designation> Designations => Set<Designation>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<LegalEntity> LegalEntities => Set<LegalEntity>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<BusinessUnit> BusinessUnits => Set<BusinessUnit>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<PositionAssignment> PositionAssignments => Set<PositionAssignment>();
    public DbSet<PositionBudget> PositionBudgets => Set<PositionBudget>();
    public DbSet<PositionVacancy> PositionVacancies => Set<PositionVacancy>();
    public DbSet<OrganizationUnit> OrganizationUnits => Set<OrganizationUnit>();
    public DbSet<ReportingRelationship> ReportingRelationships => Set<ReportingRelationship>();
    public DbSet<FutureOrgScenario> FutureOrgScenarios => Set<FutureOrgScenario>();
    public DbSet<ScenarioChange> ScenarioChanges => Set<ScenarioChange>();
    public DbSet<OrganizationSnapshot> OrganizationSnapshots => Set<OrganizationSnapshot>();
    public DbSet<ProjectionVersionRegistry> ProjectionVersionRegistries => Set<ProjectionVersionRegistry>();
    public DbSet<OrganizationHierarchyProjection> OrgHierarchyProjections => Set<OrganizationHierarchyProjection>();
    public DbSet<OrganizationMetricsProjection> OrgMetricsProjections => Set<OrganizationMetricsProjection>();
    public DbSet<PositionOccupancyProjection> PositionOccupancyProjections => Set<PositionOccupancyProjection>();
    public DbSet<PositionOccupantProjection> PositionOccupantProjections => Set<PositionOccupantProjection>();
    public DbSet<VacancyPipelineProjection> VacancyPipelineProjections => Set<VacancyPipelineProjection>();
    public DbSet<SpanOfControlProjection> SpanOfControlProjections => Set<SpanOfControlProjection>();
    public DbSet<ManagerDirectReportProjection> ManagerDirectReportProjections => Set<ManagerDirectReportProjection>();
    public DbSet<WorkforceGraphNode> GraphNodes => Set<WorkforceGraphNode>();
    public DbSet<WorkforceGraphEdge> GraphEdges => Set<WorkforceGraphEdge>();
    public DbSet<EmployeeCompensationProjection> EmployeeCompProjections => Set<EmployeeCompensationProjection>();
    public DbSet<EmployeePerformanceProjection> EmployeePerfProjections => Set<EmployeePerformanceProjection>();
    public DbSet<EmployeeFlightRiskPrediction> FlightRiskPredictions => Set<EmployeeFlightRiskPrediction>();
    public DbSet<FlightRiskDriverProjection> FlightRiskDrivers => Set<FlightRiskDriverProjection>();
    public DbSet<EmployeePromotionReadiness> PromotionReadinessPredictions => Set<EmployeePromotionReadiness>();
    public DbSet<PromotionReadinessDriverProjection> PromotionReadinessDrivers => Set<PromotionReadinessDriverProjection>();

    /// <summary>
    /// Gets the employees set.
    /// </summary>
    public DbSet<Employee> Employees => Set<Employee>();

    /// <summary>Gets the employee relationships set (org graph: dotted-line, project, mentor, etc.).</summary>
    public DbSet<EmployeeRelationship> EmployeeRelationships => Set<EmployeeRelationship>();

    /// <summary>
    /// Configures the domain model for the HR context.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new DepartmentConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeRelationshipConfiguration());

        modelBuilder.Entity<Designation>(b =>
        {
            b.ToTable("Designations");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<Branch>(b =>
        {
            b.ToTable("Branches");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<LegalEntity>(b =>
        {
            b.ToTable("LegalEntities");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.TaxId }).IsUnique();
        });

        modelBuilder.Entity<CostCenter>(b =>
        {
            b.ToTable("CostCenters");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<BusinessUnit>(b =>
        {
            b.ToTable("BusinessUnits");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<Position>(b =>
        {
            b.ToTable("Positions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            b.Property(x => x.CostCenterId);
            b.Property(x => x.LocationId);
            b.Property(x => x.BusinessUnitId);
            b.Property(x => x.OrgUnitId);
            b.Property(x => x.AllowMultipleOccupants).IsRequired();
            b.Property(x => x.ApprovedHeadcount).IsRequired();
        });

        modelBuilder.Entity<Location>(b =>
        {
            b.ToTable("Locations");
            b.HasKey(x => x.Id);
        });

        modelBuilder.Entity<PositionAssignment>(b =>
        {
            b.ToTable("HR_PositionAssignments");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.PositionId });
            b.HasIndex(x => new { x.TenantId, x.EmployeeId });
            b.HasIndex(x => new { x.TenantId, x.PositionId, x.EndUtc }).HasDatabaseName("IX_PositionAssignment_Active");
        });

        modelBuilder.Entity<PositionBudget>(b =>
        {
            b.ToTable("HR_PositionBudgets");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            b.Property(x => x.AnnualBudgetAmount).HasPrecision(18, 2);
            b.HasIndex(x => new { x.TenantId, x.PositionId });
        });

        modelBuilder.Entity<PositionVacancy>(b =>
        {
            b.ToTable("HR_PositionVacancies");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            b.Property(x => x.Reason).HasMaxLength(512);
            b.Property(x => x.RecruitmentRequisitionId);
            b.Property(x => x.ExternalReferenceId).HasMaxLength(128);
            b.Property(x => x.ClosedReason).HasMaxLength(512);
            b.HasIndex(x => new { x.TenantId, x.PositionId });
            b.HasIndex(x => new { x.TenantId, x.Status }).HasDatabaseName("IX_PositionVacancy_Status");
        });

        modelBuilder.Entity<OrganizationUnit>(b =>
        {
            b.ToTable("HR_OrganizationUnits");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.Name).HasMaxLength(256).IsRequired();
            b.Property(x => x.Code).HasMaxLength(64).IsRequired();
            b.Property(x => x.Type).HasConversion<string>().HasMaxLength(64).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.ParentUnitId });
        });

        modelBuilder.Entity<ReportingRelationship>(b =>
        {
            b.ToTable("HR_ReportingRelationships");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.ManagerPositionId }).HasDatabaseName("IX_ReportingRelationship_Manager");
            b.HasIndex(x => new { x.TenantId, x.DirectReportPositionId }).HasDatabaseName("IX_ReportingRelationship_DirectReport");
            b.HasIndex(x => new { x.TenantId, x.DirectReportPositionId, x.EffectiveFrom }).IsUnique().HasDatabaseName("IX_ReportingRelationship_Unique");
        });

        modelBuilder.Entity<FutureOrgScenario>(b =>
        {
            b.ToTable("HR_FutureOrgScenarios");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.ScenarioName).HasMaxLength(256).IsRequired();
            b.Property(x => x.Status).HasMaxLength(64).IsRequired();
            b.HasMany(x => x.Changes).WithOne().HasForeignKey(x => x.ScenarioId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.TenantId);
        });

        modelBuilder.Entity<ScenarioChange>(b =>
        {
            b.ToTable("HR_ScenarioChanges");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.Action).HasConversion<string>().HasMaxLength(64).IsRequired();
            b.Property(x => x.TargetId).HasMaxLength(64).IsRequired();
            b.Property(x => x.Name).HasMaxLength(256);
            b.Property(x => x.Code).HasMaxLength(64);
            b.Property(x => x.UnitType).HasConversion<string>().HasMaxLength(64);
            b.HasIndex(x => new { x.TenantId, x.ScenarioId });
        });

        modelBuilder.Entity<OrganizationSnapshot>(b =>
        {
            b.ToTable("HR_OrganizationSnapshots");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.Name).HasMaxLength(256).IsRequired();
            b.Property(x => x.HierarchyDataJson).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.SnapshotDate });
        });
        modelBuilder.Entity<ProjectionVersionRegistry>(b =>
        {
            b.ToTable("HR_ProjectionVersionRegistry");
            b.HasKey(x => x.ProjectionName);
            b.Property(x => x.ProjectionName).HasMaxLength(128);
            b.Property(x => x.ActiveVersion).HasMaxLength(8).IsRequired();
        });

        modelBuilder.Entity<OrganizationHierarchyProjection>(b =>
        {
            b.ToTable("HR_OrgHierarchyProjections");
            b.HasKey(x => new { x.TenantId, x.OrganizationUnitId });
            b.Property(x => x.TenantId).HasMaxLength(64);
            b.Property(x => x.Name).HasMaxLength(256).IsRequired();
            b.Property(x => x.Code).HasMaxLength(64).IsRequired();
            b.Property(x => x.Type).HasMaxLength(64).IsRequired();
            b.Property(x => x.HierarchyPath).HasMaxLength(2048).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.ParentUnitId });
        });

        modelBuilder.Entity<OrganizationMetricsProjection>(b =>
        {
            b.ToTable("HR_OrgMetricsProjections");
            b.HasKey(x => new { x.TenantId, x.OrganizationUnitId });
            b.Property(x => x.TenantId).HasMaxLength(64);
            b.Property(x => x.LeadEmployeeName).HasMaxLength(256);
            b.HasIndex(x => new { x.TenantId, x.LeadEmployeeId });
        });

        modelBuilder.Entity<PositionOccupancyProjection>(b =>
        {
            b.ToTable("HR_PositionOccupancyProjections");
            b.HasKey(x => new { x.TenantId, x.PositionId });
            b.Property(x => x.TenantId).HasMaxLength(64);
            b.Property(x => x.Code).HasMaxLength(64).IsRequired();
            b.Property(x => x.DesignationName).HasMaxLength(256).IsRequired();
            b.Property(x => x.Status).HasMaxLength(64).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.OrgUnitId });
            b.HasIndex(x => new { x.TenantId, x.DesignationId });
        });

        modelBuilder.Entity<PositionOccupantProjection>(b =>
        {
            b.ToTable("HR_PositionOccupantProjections");
            b.HasKey(x => new { x.TenantId, x.PositionId, x.EmployeeId, x.AssignmentId });
            b.Property(x => x.TenantId).HasMaxLength(64);
            b.Property(x => x.EmployeeName).HasMaxLength(256).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.EmployeeId });
        });

        modelBuilder.Entity<VacancyPipelineProjection>(b =>
        {
            b.ToTable("HR_VacancyPipelineProjections");
            b.HasKey(x => new { x.TenantId, x.VacancyId });
            b.Property(x => x.TenantId).HasMaxLength(64);
            b.Property(x => x.PositionCode).HasMaxLength(64).IsRequired();
            b.Property(x => x.Status).HasMaxLength(64).IsRequired();
            b.Property(x => x.Reason).HasMaxLength(512).IsRequired();
            b.Property(x => x.ExternalReferenceId).HasMaxLength(128);
            b.Property(x => x.ClosedReason).HasMaxLength(512);
            b.HasIndex(x => new { x.TenantId, x.PositionId });
            b.HasIndex(x => new { x.TenantId, x.RecruitmentRequisitionId });
        });

        modelBuilder.Entity<SpanOfControlProjection>(b =>
        {
            b.ToTable("HR_SpanOfControlProjections");
            b.HasKey(x => new { x.TenantId, x.ManagerPositionId });
            b.Property(x => x.TenantId).HasMaxLength(64);
            b.Property(x => x.ManagerName).HasMaxLength(256).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.ManagerEmployeeId });
        });

        modelBuilder.Entity<ManagerDirectReportProjection>(b =>
        {
            b.ToTable("HR_ManagerDirectReportProjections");
            b.HasKey(x => new { x.TenantId, x.ManagerPositionId, x.DirectReportPositionId });
            b.Property(x => x.TenantId).HasMaxLength(64);
            b.Property(x => x.DirectReportEmployeeName).HasMaxLength(256);
            b.HasIndex(x => new { x.TenantId, x.DirectReportPositionId });
            b.HasIndex(x => new { x.TenantId, x.DirectReportEmployeeId });
        });

        modelBuilder.Entity<WorkforceGraphNode>(b =>
        {
            b.ToTable("HR_GraphNodes");
            b.HasKey(x => new { x.TenantId, x.Id });
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.Type).HasConversion<string>().HasMaxLength(64).IsRequired();
            b.Property(x => x.DisplayName).HasMaxLength(256).IsRequired();
            b.Property(x => x.PropertiesJson).IsRequired(false);
        });

        modelBuilder.Entity<WorkforceGraphEdge>(b =>
        {
            b.ToTable("HR_GraphEdges");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.Type).HasConversion<string>().HasMaxLength(64).IsRequired();
            b.Property(x => x.Weight).HasPrecision(9, 4);
            b.Property(x => x.IsActive).IsRequired();

            // Indexes supporting fast source/target traversal with active filtering
            b.HasIndex(x => new { x.TenantId, x.SourceNodeId, x.Type, x.IsActive }).HasDatabaseName("IX_GraphEdge_Source_Traversal");
            b.HasIndex(x => new { x.TenantId, x.TargetNodeId, x.Type, x.IsActive }).HasDatabaseName("IX_GraphEdge_Target_Traversal");

            // Indexes supporting temporal queries
            b.HasIndex(x => new { x.TenantId, x.EffectiveFrom });
            b.HasIndex(x => new { x.TenantId, x.EffectiveTo });

            // Edge uniqueness constraint to prevent duplicates during concurrent runs/replays
            b.HasIndex(x => new { x.TenantId, x.SourceNodeId, x.TargetNodeId, x.Type, x.EffectiveFrom })
                .IsUnique()
                .HasDatabaseName("UX_GraphEdge_Unique");
        });

        modelBuilder.Entity<EmployeeCompensationProjection>(b =>
        {
            b.ToTable("HR_EmployeeCompensationProjections");
            b.HasKey(x => new { x.TenantId, x.EmployeeId });
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.AnnualCTC).HasPrecision(18, 2);
            b.Property(x => x.PreviousAnnualCTC).HasPrecision(18, 2);
            b.Property(x => x.IncrementPercent).HasPrecision(9, 4);
        });

        modelBuilder.Entity<EmployeePerformanceProjection>(b =>
        {
            b.ToTable("HR_EmployeePerformanceProjections");
            b.HasKey(x => new { x.TenantId, x.EmployeeId });
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.CompositeScore).HasPrecision(9, 4);
            b.Property(x => x.PerformanceBucket).HasMaxLength(64).IsRequired();
        });

        modelBuilder.Entity<EmployeeFlightRiskPrediction>(b =>
        {
            b.ToTable("HR_FlightRiskPredictions");
            b.HasKey(x => new { x.TenantId, x.EmployeeId });
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.RiskScore).HasPrecision(9, 4);
            b.HasMany(x => x.Drivers).WithOne().HasForeignKey(x => new { x.TenantId, x.EmployeeId }).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FlightRiskDriverProjection>(b =>
        {
            b.ToTable("HR_FlightRiskDrivers");
            b.HasKey(x => new { x.TenantId, x.EmployeeId, x.DriverType });
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.DriverType).HasConversion<string>().HasMaxLength(64).IsRequired();
            b.Property(x => x.FactorScore).HasPrecision(9, 4);
            b.Property(x => x.Explanation).HasMaxLength(512).IsRequired();
        });

        modelBuilder.Entity<EmployeePromotionReadiness>(b =>
        {
            b.ToTable("HR_PromotionReadinessPredictions");
            b.HasKey(x => new { x.TenantId, x.EmployeeId });
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.ReadinessScore).HasPrecision(9, 4);
            b.HasMany(x => x.Drivers).WithOne().HasForeignKey(x => new { x.TenantId, x.EmployeeId }).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PromotionReadinessDriverProjection>(b =>
        {
            b.ToTable("HR_PromotionReadinessDrivers");
            b.HasKey(x => new { x.TenantId, x.EmployeeId, x.DriverType });
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.DriverType).HasConversion<string>().HasMaxLength(64).IsRequired();
            b.Property(x => x.FactorScore).HasPrecision(9, 4);
            b.Property(x => x.Explanation).HasMaxLength(512).IsRequired();
        });
        // MassTransit's transactional outbox entities. They are registered in the
        // base KaramchariDbContext (pinned to dbo and excluded from migrations).
        // HRDbContext is the designated authority for these tables, so we
        // override the exclusion here so they are physically managed by this context.
        modelBuilder.Entity<InboxState>(b => b.ToTable("InboxState", MessagingSchema));
        modelBuilder.Entity<OutboxMessage>(b => b.ToTable("OutboxMessage", MessagingSchema));
        modelBuilder.Entity<OutboxState>(b => b.ToTable("OutboxState", MessagingSchema));
    }
}
