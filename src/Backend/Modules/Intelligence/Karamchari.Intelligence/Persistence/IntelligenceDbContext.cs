using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Intelligence.Domain.Metrics;
using Karamchari.Intelligence.Domain.Primitives;
using Karamchari.Intelligence.Domain.Signals;
using Karamchari.Intelligence.Domain.Workforce;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Intelligence.Persistence;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public class IntelligenceDbContext : KaramchariDbContext
{
    /// <inheritdoc/>
    public IntelligenceDbContext(DbContextOptions<IntelligenceDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<IntelligenceSignal> IntelligenceSignals => Set<IntelligenceSignal>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<MetricDefinition> MetricDefinitions => Set<MetricDefinition>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<OrganizationalHealthSignal> OrganizationalHealthSignals => Set<OrganizationalHealthSignal>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<WorkforceRiskSignal> WorkforceRiskSignals => Set<WorkforceRiskSignal>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<ExecutiveInsight> ExecutiveInsights => Set<ExecutiveInsight>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<StrategicWorkforceScenario> StrategicWorkforceScenarios => Set<StrategicWorkforceScenario>();

    // ── Phase 6: Workforce Intelligence ─────────────────────────────────────

    /// <summary>Raw workforce signal measurements per employee.</summary>
    public DbSet<WorkforceSignalRecord> WorkforceSignalRecords => Set<WorkforceSignalRecord>();

    /// <summary>Latest burnout score per employee.</summary>
    public DbSet<WorkforceBurnoutScore> WorkforceBurnoutScores => Set<WorkforceBurnoutScore>();

    /// <summary>Latest attrition risk score per employee.</summary>
    public DbSet<WorkforceAttritionScore> WorkforceAttritionScores => Set<WorkforceAttritionScore>();

    /// <summary>Org/site-level workforce health scores.</summary>
    public DbSet<WorkforceHealthScore> WorkforceHealthScores => Set<WorkforceHealthScore>();

    /// <summary>Manager effectiveness scores.</summary>
    public DbSet<ManagerEffectivenessScore> ManagerEffectivenessScores => Set<ManagerEffectivenessScore>();

    /// <summary>Open and resolved workforce recommendations.</summary>
    public DbSet<WorkforceRecommendation> WorkforceRecommendations => Set<WorkforceRecommendation>();

    // ── Phase 6.1: Intelligence Maturity ────────────────────────────────────

    /// <summary>Append-only score history for velocity and trend computation.</summary>
    public DbSet<WorkforceScoreSnapshot> WorkforceScoreSnapshots => Set<WorkforceScoreSnapshot>();

    /// <summary>Linear-extrapolation forecasts per employee per score type.</summary>
    public DbSet<WorkforceForecast> WorkforceForecasts => Set<WorkforceForecast>();

    /// <summary>Composite talent risk scores (burnout + attrition + dependency).</summary>
    public DbSet<TalentRiskScore> TalentRiskScores => Set<TalentRiskScore>();

    /// <summary>Team-level workload fairness scores.</summary>
    public DbSet<WorkloadFairnessScore> WorkloadFairnessScores => Set<WorkloadFairnessScore>();

    /// <summary>Team-level absence contagion detection scores.</summary>
    public DbSet<AbsenceContagionScore> AbsenceContagionScores => Set<AbsenceContagionScore>();

    /// <summary>Daily feature snapshots for ML training data collection.</summary>
    public DbSet<WorkforceFeatureSnapshot> WorkforceFeatureSnapshots => Set<WorkforceFeatureSnapshot>();

    /// <summary>Ground-truth outcome labels for supervised ML training.</summary>
    public DbSet<WorkforceOutcomeLabel> WorkforceOutcomeLabels => Set<WorkforceOutcomeLabel>();

    /// <inheritdoc/>
    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.Entity<IntelligenceSignal>(b =>
        {
            b.ToTable("Intelligence_Signals");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.SignalType, x.SubjectId });
            b.Property(x => x.RowVersion).IsRowVersion();

            b.Property(x => x.Confidence)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<SignalConfidence>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                .HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<MetricDefinition>(b =>
        {
            b.ToTable("Intelligence_MetricDefinitions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Name, x.CurrentVersion }).IsUnique();
            b.Property(x => x.RowVersion).IsRowVersion();

            b.Property(x => x.Calculation)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<CalculationDefinition>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                .HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<OrganizationalHealthSignal>(b =>
        {
            b.ToTable("Strategy_OrgHealthSignals");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.OrgUnitId });
            b.Property(x => x.OverallHealthScore).HasPrecision(5, 2);
            b.Property(x => x.RowVersion).IsRowVersion();

            b.Property(x => x.BurnoutRisk)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<WorkforcePressureIndex>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                .HasColumnType("nvarchar(max)");

            b.Property(x => x.StaffingStress)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<WorkforcePressureIndex>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                .HasColumnType("nvarchar(max)");

            b.Property(x => x.Confidence)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<SignalConfidence>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                .HasColumnType("nvarchar(max)");

            b.Property(x => x.Explanation)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<ScoreExplanation>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                .HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<WorkforceRiskSignal>(b =>
        {
            b.ToTable("Strategy_WorkforceRiskSignals");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Category, x.SubjectId });
            b.Property(x => x.Category).HasConversion<string>();
            b.Property(x => x.RowVersion).IsRowVersion();

            b.Property(x => x.Severity)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<WorkforcePressureIndex>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                .HasColumnType("nvarchar(max)");

            b.Property(x => x.Confidence)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<SignalConfidence>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                .HasColumnType("nvarchar(max)");

            b.Property(x => x.Explanation)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<ScoreExplanation>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                .HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<ExecutiveInsight>(b =>
        {
            b.ToTable("Strategy_ExecutiveInsights");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Category });
            b.Property(x => x.ContributingSignalIds).HasConversion(
                v => string.Join(',', v),
                v => (IReadOnlyCollection<Guid>)v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToList()
            ).Metadata.SetValueComparer(Karamchari.Core.Persistence.ValueComparers.ReadOnlyCollectionComparer<Guid>());


            b.Property(x => x.AggregateConfidence)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<SignalConfidence>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                .HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<StrategicWorkforceScenario>(b =>
        {
            b.ToTable("Strategy_StrategicScenarios");
            b.HasKey(x => x.Id);
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        // ── Phase 6: Workforce Intelligence ─────────────────────────────────

        modelBuilder.Entity<WorkforceSignalRecord>(b =>
        {
            b.ToTable("Intel_WorkforceSignals");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.SignalType, x.SignalDate });
            b.HasIndex(x => new { x.TenantId, x.EmployeeId });
            b.Property(x => x.SignalType).HasConversion<string>();
            b.Property(x => x.Value).HasPrecision(18, 4);
        });

        modelBuilder.Entity<WorkforceBurnoutScore>(b =>
        {
            b.ToTable("Intel_BurnoutScores");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId }).IsUnique();
            b.Property(x => x.Score).HasPrecision(5, 1);
            b.Property(x => x.RiskLevel).HasConversion<string>();
            b.Property(x => x.Confidence).HasConversion<string>();
            b.Property(x => x.RowVersion).IsRowVersion();
            b.Property(x => x.Explanation)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Domain.Signals.ScoreExplanation>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                .HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<WorkforceAttritionScore>(b =>
        {
            b.ToTable("Intel_AttritionScores");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId }).IsUnique();
            b.Property(x => x.Score).HasPrecision(5, 1);
            b.Property(x => x.RiskLevel).HasConversion<string>();
            b.Property(x => x.Confidence).HasConversion<string>();
            b.Property(x => x.RowVersion).IsRowVersion();
            b.Property(x => x.Explanation)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Domain.Signals.ScoreExplanation>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                .HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<WorkforceHealthScore>(b =>
        {
            b.ToTable("Intel_WorkforceHealthScores");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.SiteCode }).IsUnique();
            b.Property(x => x.Score).HasPrecision(5, 1);
            b.Property(x => x.AttendanceHealth).HasPrecision(5, 1);
            b.Property(x => x.BurnoutHealth).HasPrecision(5, 1);
            b.Property(x => x.StaffingHealth).HasPrecision(5, 1);
            b.Property(x => x.LeaveHealth).HasPrecision(5, 1);
            b.Property(x => x.PayrollHealth).HasPrecision(5, 1);
            b.Property(x => x.StabilityHealth).HasPrecision(5, 1);
            b.Property(x => x.RowVersion).IsRowVersion();
            b.Property(x => x.Explanation)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Domain.Signals.ScoreExplanation>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                .HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<ManagerEffectivenessScore>(b =>
        {
            b.ToTable("Intel_ManagerScores");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.ManagerId }).IsUnique();
            b.Property(x => x.Score).HasPrecision(5, 1);
            b.Property(x => x.AttendanceDimension).HasPrecision(5, 1);
            b.Property(x => x.BurnoutDimension).HasPrecision(5, 1);
            b.Property(x => x.AttritionDimension).HasPrecision(5, 1);
            b.Property(x => x.LeaveHealthDimension).HasPrecision(5, 1);
            b.Property(x => x.StabilityDimension).HasPrecision(5, 1);
            b.Property(x => x.OvertimeDimension).HasPrecision(5, 1);
            b.Property(x => x.RowVersion).IsRowVersion();
            b.Property(x => x.Explanation)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Domain.Signals.ScoreExplanation>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                .HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<WorkforceRecommendation>(b =>
        {
            b.ToTable("Intel_WorkforceRecommendations");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId });
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Type, x.ResolvedAt });
            b.Property(x => x.Type).HasConversion<string>();
            b.Property(x => x.Priority).HasConversion<string>();
            b.Property(x => x.TriggerScore).HasPrecision(5, 1);
            b.Property(x => x.Rationale).HasMaxLength(1000);
        });

        // ── Phase 6.1: Intelligence Maturity ────────────────────────────────

        modelBuilder.Entity<WorkforceScoreSnapshot>(b =>
        {
            b.ToTable("Intel_ScoreSnapshots");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.ScoreType, x.CalculatedAt });
            b.Property(x => x.ScoreType).HasMaxLength(20);
            b.Property(x => x.Score).HasPrecision(5, 1);
            b.Property(x => x.RiskLevel).HasConversion<string>();
        });

        modelBuilder.Entity<WorkforceForecast>(b =>
        {
            b.ToTable("Intel_Forecasts");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.ScoreType }).IsUnique();
            b.Property(x => x.ScoreType).HasMaxLength(20);
            b.Property(x => x.CurrentScore).HasPrecision(5, 1);
            b.Property(x => x.ProjectedScore).HasPrecision(5, 1);
            b.Property(x => x.PointsPerDay).HasPrecision(8, 3);
            b.Property(x => x.Trend).HasConversion<string>();
            b.Property(x => x.Confidence).HasConversion<string>();
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<TalentRiskScore>(b =>
        {
            b.ToTable("Intel_TalentRiskScores");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId }).IsUnique();
            b.Property(x => x.Score).HasPrecision(5, 1);
            b.Property(x => x.BurnoutComponent).HasPrecision(5, 1);
            b.Property(x => x.AttritionComponent).HasPrecision(5, 1);
            b.Property(x => x.DependencyComponent).HasPrecision(5, 1);
            b.Property(x => x.CompoundPenalty).HasPrecision(5, 1);
            b.Property(x => x.RiskLevel).HasConversion<string>();
            b.Property(x => x.RowVersion).IsRowVersion();
            b.Property(x => x.Explanation)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Domain.Signals.ScoreExplanation>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                .HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<WorkloadFairnessScore>(b =>
        {
            b.ToTable("Intel_WorkloadFairness");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.SiteCode, x.TeamId }).IsUnique();
            b.Property(x => x.SiteCode).HasMaxLength(50);
            b.Property(x => x.OverallScore).HasPrecision(5, 1);
            b.Property(x => x.OtGiniCoefficient).HasPrecision(8, 4);
            b.Property(x => x.OtConcentrationTop10Pct).HasPrecision(8, 4);
            b.Property(x => x.AvgOtHoursPerMember).HasPrecision(8, 2);
            b.Property(x => x.MaxOtHoursAnyMember).HasPrecision(8, 2);
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<AbsenceContagionScore>(b =>
        {
            b.ToTable("Intel_AbsenceContagion");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.SiteCode, x.TeamId }).IsUnique();
            b.Property(x => x.SiteCode).HasMaxLength(50);
            b.Property(x => x.CurrentAbsenceRate).HasPrecision(8, 4);
            b.Property(x => x.BaselineAbsenceRate).HasPrecision(8, 4);
            b.Property(x => x.ZScore).HasPrecision(8, 2);
            b.Property(x => x.RiskLevel).HasConversion<string>();
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<WorkforceFeatureSnapshot>(b =>
        {
            b.ToTable("Intel_FeatureSnapshots");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.SnapshotDate }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.SnapshotDate });
            b.Property(x => x.BurnoutScore).HasPrecision(5, 1);
            b.Property(x => x.AttritionScore).HasPrecision(5, 1);
            b.Property(x => x.TalentRiskScore).HasPrecision(5, 1);
            b.Property(x => x.OvertimeHours28d).HasPrecision(8, 2);
            b.Property(x => x.HighIntensityShiftRatio).HasPrecision(5, 4);
            b.Property(x => x.LateArrivalSlope).HasPrecision(8, 3);
            b.Property(x => x.LeaveFrequencyRatio).HasPrecision(8, 3);
            b.Property(x => x.SickLeaveDays30d).HasPrecision(8, 2);
            b.Property(x => x.PeerAttendanceGap).HasPrecision(8, 2);
            b.Property(x => x.ManagerFrictionScore).HasPrecision(5, 4);
            b.Property(x => x.BurnoutRiskLevel).HasConversion<string>();
            b.Property(x => x.AttritionRiskLevel).HasConversion<string>();
        });

        modelBuilder.Entity<WorkforceOutcomeLabel>(b =>
        {
            b.ToTable("Intel_OutcomeLabels");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Outcome, x.OutcomeDate }).IsUnique();
            b.Property(x => x.Outcome).HasConversion<string>();
            b.Property(x => x.BurnoutScoreAtOutcome).HasPrecision(5, 1);
            b.Property(x => x.AttritionScoreAtOutcome).HasPrecision(5, 1);
            b.Property(x => x.Notes).HasMaxLength(500);
        });
    }
}
