using System.Text.Json;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Forecasting.Domain;
using Karamchari.Forecasting.Domain.Workforce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Karamchari.Forecasting.Persistence;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class ForecastingDbContext : KaramchariDbContext
{
    public ForecastingDbContext(DbContextOptions<ForecastingDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider) { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<ForecastMetrics> ForecastMetrics => Set<ForecastMetrics>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<ClientPaymentProfile> ClientPaymentProfiles => Set<ClientPaymentProfile>();

    // ── Phase 5: Workforce Planning ─────────────────────────────────────────
    public DbSet<WorkforceEmployeeIndex> WorkforceEmployeeIndex => Set<WorkforceEmployeeIndex>();
    public DbSet<WfpSkillExpiry> WfpSkillExpiries => Set<WfpSkillExpiry>();
    public DbSet<WfpAttendanceSnapshot> WfpAttendanceSnapshots => Set<WfpAttendanceSnapshot>();
    public DbSet<WfpAttritionSnapshot> WfpAttritionSnapshots => Set<WfpAttritionSnapshot>();
    public DbSet<WorkforcePlanningProjection> WorkforcePlanningProjections => Set<WorkforcePlanningProjection>();
    public DbSet<WfpApprovedLeaveRecord> WfpApprovedLeaveRecords => Set<WfpApprovedLeaveRecord>();
    public DbSet<DemandForecast> DemandForecasts => Set<DemandForecast>();
    public DbSet<SupplyForecast> SupplyForecasts => Set<SupplyForecast>();
    public DbSet<CoverageRisk> CoverageRisks => Set<CoverageRisk>();
    public DbSet<HiringGap> HiringGaps => Set<HiringGap>();
    public DbSet<WfpScenarioForecast> WfpScenarioForecasts => Set<WfpScenarioForecast>();
    public DbSet<WfpScenarioResult> WfpScenarioResults => Set<WfpScenarioResult>();

    // Phase 5.1 Hardening
    public DbSet<WfpSkillSubstitution> WfpSkillSubstitutions => Set<WfpSkillSubstitution>();
    public DbSet<WfpOvertimePolicy> WfpOvertimePolicies => Set<WfpOvertimePolicy>();
    public DbSet<WfpForecastAccuracy> WfpForecastAccuracies => Set<WfpForecastAccuracy>();
    public DbSet<WfpRecomputeEntry> WfpRecomputeEntry => Set<WfpRecomputeEntry>();

    // Phase 5.x Enterprise Enhancements
    public DbSet<WfpFutureHire> WfpFutureHires => Set<WfpFutureHire>();
    public DbSet<WfpContractExpiry> WfpContractExpiries => Set<WfpContractExpiry>();
    public DbSet<WfpRetirementRisk> WfpRetirementRisks => Set<WfpRetirementRisk>();
    // Phase 5.2
    public DbSet<WfpRetirementPolicy> WfpRetirementPolicies => Set<WfpRetirementPolicy>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.Entity<ForecastMetrics>(b =>
        {
            b.ToTable("Forecast_Metrics");
            b.HasKey(x => x.Id);
            b.Property(x => x.ProjectedRevenue).HasPrecision(18, 2);
            b.Property(x => x.ProjectedCash).HasPrecision(18, 2);
            b.Property(x => x.OutstandingAmount).HasPrecision(18, 2);
            b.Property(x => x.RiskAmount).HasPrecision(18, 2);

            b.HasIndex(x => new { x.TenantId, x.Date });
        });

        modelBuilder.Entity<ClientPaymentProfile>(b =>
        {
            b.ToTable("Forecast_ClientPaymentProfiles");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.ClientId }).IsUnique();
        });

        // ── Phase 5: Workforce Planning ─────────────────────────────────────

        modelBuilder.Entity<WfpSkillExpiry>(b =>
        {
            b.ToTable("WFP_SkillExpiries");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.EmployeeId);
            b.HasIndex(x => new { x.TenantId, x.LocationCode, x.SkillCode, x.ExpiryDate });
            b.Property(x => x.LocationCode).HasMaxLength(64);
            b.Property(x => x.SkillCode).HasMaxLength(64);
        });

        modelBuilder.Entity<WfpAttendanceSnapshot>(b =>
        {
            b.ToTable("WFP_AttendanceSnapshots");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.LocationCode, x.SkillCode, x.Date }).IsUnique();
            b.Property(x => x.LocationCode).HasMaxLength(64);
            b.Property(x => x.SkillCode).HasMaxLength(64);
        });

        modelBuilder.Entity<WfpAttritionSnapshot>(b =>
        {
            b.ToTable("WFP_AttritionSnapshots");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.LocationCode, x.SkillCode, x.Year, x.Month }).IsUnique();
            b.Property(x => x.LocationCode).HasMaxLength(64);
            b.Property(x => x.SkillCode).HasMaxLength(64);
        });

        modelBuilder.Entity<WfpScenarioForecast>(b =>
        {
            b.ToTable("WFP_ScenarioForecasts");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.TenantId);
            b.Property(x => x.Name).HasMaxLength(256);
            b.Property(x => x.DemandMultiplier).HasPrecision(8, 4);
            b.Property(x => x.AbsenteeismDeltaPercent).HasPrecision(5, 2);
            b.Property(x => x.AttritionDeltaPercent).HasPrecision(5, 2);
        });

        modelBuilder.Entity<WfpScenarioResult>(b =>
        {
            b.ToTable("WFP_ScenarioResults");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.ScenarioId);
            b.HasIndex(x => new { x.TenantId, x.LocationCode, x.SkillCode, x.Horizon });
            b.Property(x => x.LocationCode).HasMaxLength(64);
            b.Property(x => x.SkillCode).HasMaxLength(64);
            b.Property(x => x.ScenarioRiskLevel).HasConversion<string>().HasMaxLength(16);
            b.Property(x => x.Horizon).HasConversion<int>();
        });

        modelBuilder.Entity<WorkforceEmployeeIndex>(b =>
        {
            b.ToTable("WFP_EmployeeIndex");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.TenantId);
            b.Property(x => x.LocationCode).HasMaxLength(64);
            b.Property(x => x.SkillCodes)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnType("nvarchar(max)")
                // ValueComparer required: without it, EF Core uses reference equality for List<string>
                // and cannot detect in-place mutations (e.g. Add/Remove on the same list instance).
                // The comparer snapshots a copy so mutations are detected at SaveChanges time.
                .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                    (a, b) => a != null && b != null && a.SequenceEqual(b),
                    c => c.Aggregate(0, (hash, v) => HashCode.Combine(hash, v.GetHashCode(StringComparison.Ordinal))),
                    c => new List<string>(c)));
            // Phase 5.x: nullable employee profile fields for WFP enrichment
            b.Property(x => x.DateOfBirth).HasColumnType("date");
            b.Property(x => x.ContractEndDate).HasColumnType("date");
        });

        modelBuilder.Entity<WorkforcePlanningProjection>(b =>
        {
            b.ToTable("WFP_Projections");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.LocationCode, x.SkillCode }).IsUnique();
            b.Property(x => x.LocationCode).HasMaxLength(64);
            b.Property(x => x.SkillCode).HasMaxLength(64);
            b.Property(x => x.HistoricalAbsenteeismRate).HasPrecision(5, 2);
            b.Property(x => x.AttendanceReliabilityScore).HasPrecision(5, 4);
            b.Property(x => x.HistoricalAttritionRatePercent).HasPrecision(5, 2);
            b.Property(x => x.DayOfWeekAbsenceRatesJson).HasColumnType("nvarchar(2048)");
        });

        modelBuilder.Entity<WfpApprovedLeaveRecord>(b =>
        {
            b.ToTable("WFP_ApprovedLeaves");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.RequestId);
            b.HasIndex(x => new { x.TenantId, x.LocationCode, x.SkillCode, x.StartDate, x.EndDate });
            b.HasIndex(x => x.EmployeeId);
            b.Property(x => x.LocationCode).HasMaxLength(64);
            b.Property(x => x.SkillCode).HasMaxLength(64);
        });

        modelBuilder.Entity<DemandForecast>(b =>
        {
            b.ToTable("WFP_DemandForecasts");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.LocationCode, x.SkillCode, x.ForecastDate, x.Horizon });
            b.Property(x => x.LocationCode).HasMaxLength(64);
            b.Property(x => x.SkillCode).HasMaxLength(64);
            b.Property(x => x.RequiredHours).HasPrecision(10, 2);
            b.Property(x => x.Source).HasConversion<string>().HasMaxLength(32);
            b.Property(x => x.Confidence).HasConversion<string>().HasMaxLength(16);
            b.Property(x => x.Horizon).HasConversion<int>();
        });

        modelBuilder.Entity<SupplyForecast>(b =>
        {
            b.ToTable("WFP_SupplyForecasts");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.LocationCode, x.SkillCode, x.ForecastDate, x.Horizon });
            b.Property(x => x.LocationCode).HasMaxLength(64);
            b.Property(x => x.SkillCode).HasMaxLength(64);
            b.Property(x => x.ExpectedHours).HasPrecision(10, 2);
            b.Property(x => x.ReliabilityScore).HasPrecision(5, 4);
            b.Property(x => x.Horizon).HasConversion<int>();
            // New Phase 5v2 breakdown columns
            b.Property(x => x.SkillExpiryCount).HasDefaultValue(0);
            b.Property(x => x.ExpectedAttrition).HasDefaultValue(0);
            // Phase 5.x breakdown columns
            b.Property(x => x.FutureHireAddition).HasDefaultValue(0);
            b.Property(x => x.ContractExpiryDeduction).HasDefaultValue(0);
            b.Property(x => x.RetirementDeduction).HasDefaultValue(0);
        });

        modelBuilder.Entity<CoverageRisk>(b =>
        {
            b.ToTable("WFP_CoverageRisks");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.LocationCode, x.SkillCode, x.RiskDate, x.Horizon });
            b.HasIndex(x => new { x.TenantId, x.RiskLevel });
            b.Property(x => x.LocationCode).HasMaxLength(64);
            b.Property(x => x.SkillCode).HasMaxLength(64);
            b.Property(x => x.RiskLevel).HasConversion<string>().HasMaxLength(16);
            b.Property(x => x.Horizon).HasConversion<int>();
        });

        modelBuilder.Entity<HiringGap>(b =>
        {
            b.ToTable("WFP_HiringGaps");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.LocationCode, x.SkillCode, x.PlanningPeriod, x.Horizon });
            b.Property(x => x.LocationCode).HasMaxLength(64);
            b.Property(x => x.SkillCode).HasMaxLength(64);
            b.Property(x => x.Horizon).HasConversion<int>();
            b.Property(x => x.OvertimeCapacityHeadcount).HasDefaultValue(0);
            b.Property(x => x.CrossSkillCapacityHeadcount).HasDefaultValue(0);
            b.Property(x => x.NetHiringNeeded).HasDefaultValue(0);
        });

        // ── Phase 5.1: Hardening Models ────────────────────────────────────────

        modelBuilder.Entity<WfpSkillSubstitution>(b =>
        {
            b.ToTable("WFP_SkillSubstitutions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.SkillCode, x.SubstituteSkillCode }).IsUnique();
            b.Property(x => x.SkillCode).HasMaxLength(64);
            b.Property(x => x.SubstituteSkillCode).HasMaxLength(64);
            b.Property(x => x.CapacityFraction).HasPrecision(4, 3);
        });

        modelBuilder.Entity<WfpOvertimePolicy>(b =>
        {
            b.ToTable("WFP_OvertimePolicies");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.LocationCode }).IsUnique();
            b.Property(x => x.LocationCode).HasMaxLength(64);
            b.Property(x => x.StandardWeeklyHours).HasPrecision(5, 2);
            b.Property(x => x.MaxLegalWeeklyHours).HasPrecision(5, 2);
            b.Ignore(x => x.OvertimeFactor); // computed property, not stored
        });

        modelBuilder.Entity<WfpForecastAccuracy>(b =>
        {
            b.ToTable("WFP_ForecastAccuracies");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.LocationCode, x.SkillCode, x.ForecastDate, x.Horizon });
            b.HasIndex(x => new { x.TenantId, x.ForecastDate });
            b.Property(x => x.LocationCode).HasMaxLength(64);
            b.Property(x => x.SkillCode).HasMaxLength(64);
            b.Property(x => x.AbsolutePercentError).HasPrecision(7, 2);
            b.Property(x => x.SignedError);
            b.Property(x => x.SquaredError).HasPrecision(12, 2);
            b.Property(x => x.Horizon).HasConversion<int>();
        });

        modelBuilder.Entity<WfpRecomputeEntry>(b =>
        {
            b.ToTable("WFP_RecomputeQueue");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.LocationCode, x.SkillCode }).IsUnique();
            b.Property(x => x.LocationCode).HasMaxLength(64);
            b.Property(x => x.SkillCode).HasMaxLength(64);
        });

        // ── Phase 5.x Enterprise Enhancements ─────────────────────────────────

        modelBuilder.Entity<WfpFutureHire>(b =>
        {
            b.ToTable("WFP_FutureHires");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.OfferId).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.LocationCode, x.SkillCode, x.ExpectedStartDate, x.IsOnboarded });
            b.Property(x => x.LocationCode).HasMaxLength(64);
            b.Property(x => x.SkillCode).HasMaxLength(64);
            b.Property(x => x.JoiningProbability).HasPrecision(4, 3).HasDefaultValue(1.0m);
        });

        modelBuilder.Entity<WfpContractExpiry>(b =>
        {
            b.ToTable("WFP_ContractExpiries");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.EmployeeId);
            b.HasIndex(x => new { x.TenantId, x.LocationCode, x.SkillCode, x.ContractEndDate, x.IsTerminated });
            b.Property(x => x.LocationCode).HasMaxLength(64);
            b.Property(x => x.SkillCode).HasMaxLength(64);
        });

        modelBuilder.Entity<WfpRetirementRisk>(b =>
        {
            b.ToTable("WFP_RetirementRisks");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.EmployeeId);
            b.HasIndex(x => new { x.TenantId, x.LocationCode, x.SkillCode, x.ExpectedRetirementDate, x.IsRetired });
            b.Property(x => x.LocationCode).HasMaxLength(64);
            b.Property(x => x.SkillCode).HasMaxLength(64);
        });

        // ── Phase 5.2: Retirement Policy + Join Probability ───────────────────

        modelBuilder.Entity<WfpRetirementPolicy>(b =>
        {
            b.ToTable("WFP_RetirementPolicies");
            b.HasKey(x => x.Id);
            // Unique per (TenantId, RoleCode) — RoleCode IS NULL is a valid unique value in SQL Server
            b.HasIndex(x => new { x.TenantId, x.RoleCode }).IsUnique();
            b.Property(x => x.RoleCode).HasMaxLength(64);
        });

    }
}
