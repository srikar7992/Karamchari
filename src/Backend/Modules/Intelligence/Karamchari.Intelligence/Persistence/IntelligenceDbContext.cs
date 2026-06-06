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
    }
}
