using Karamchari.Compliance.Domain.LegalHold;
using Karamchari.Compliance.Domain.Regulatory;
using Karamchari.Compliance.Domain.Reporting;
using Karamchari.Compliance.Domain.Retention;
using Karamchari.Compliance.Domain.Scoring;
using Karamchari.Compliance.Domain.Violations;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Compliance.Persistence;

public class ComplianceDbContext : KaramchariDbContext
{
    public ComplianceDbContext(DbContextOptions<ComplianceDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider) { }

    public DbSet<RetentionPolicy> RetentionPolicies => Set<RetentionPolicy>();
    public DbSet<RetentionAction> RetentionActions => Set<RetentionAction>();
    public DbSet<LegalHold> LegalHolds => Set<LegalHold>();
    public DbSet<LegalHoldScope> LegalHoldScopes => Set<LegalHoldScope>();
    public DbSet<RegulatoryRegister> RegulatoryRegisters => Set<RegulatoryRegister>();
    public DbSet<RegulatoryRule> RegulatoryRules => Set<RegulatoryRule>();
    public DbSet<PolicyViolation> PolicyViolations => Set<PolicyViolation>();
    public DbSet<ComplianceScore> ComplianceScores => Set<ComplianceScore>();
    public DbSet<ComplianceSnapshot> ComplianceSnapshots => Set<ComplianceSnapshot>();
    public DbSet<ComplianceScoreHistory> ComplianceScoreHistory => Set<ComplianceScoreHistory>();
    public DbSet<AuditPackage> AuditPackages => Set<AuditPackage>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.Entity<RetentionPolicy>(b =>
        {
            b.ToTable("Compliance_RetentionPolicies");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.RecordType }).IsUnique();
            b.Property(x => x.RecordType).HasConversion<string>();
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<RetentionAction>(b =>
        {
            b.ToTable("Compliance_RetentionActions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.RecordType, x.RecordId, x.Action });
            b.HasIndex(x => new { x.TenantId, x.Status });
            b.Property(x => x.RecordType).HasConversion<string>();
            b.Property(x => x.Action).HasConversion<string>();
            b.Property(x => x.Status).HasConversion<string>();
        });

        modelBuilder.Entity<LegalHold>(b =>
        {
            b.ToTable("Compliance_LegalHolds");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.CaseReference });
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.CaseReference).HasMaxLength(200);
            b.Property(x => x.CreatedBy).HasMaxLength(200);
            b.Property(x => x.RowVersion).IsRowVersion();
            b.HasMany(x => x.Scopes).WithOne().HasForeignKey(x => x.LegalHoldId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LegalHoldScope>(b =>
        {
            b.ToTable("Compliance_LegalHoldScopes");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.LegalHoldId, x.ScopeType, x.ScopeValue });
            b.Property(x => x.ScopeType).HasConversion<string>();
            b.Property(x => x.ScopeValue).HasMaxLength(500);
        });

        modelBuilder.Entity<RegulatoryRegister>(b =>
        {
            b.ToTable("Compliance_RegulatoryRegisters");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Country, x.RegulationCode }).IsUnique();
            b.Property(x => x.RegulationCode).HasMaxLength(100);
            b.Property(x => x.Country).HasMaxLength(100);
            b.Property(x => x.State).HasMaxLength(100);
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<RegulatoryRule>(b =>
        {
            b.ToTable("Compliance_RegulatoryRules");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.RegisterId, x.RuleCode }).IsUnique();
            b.Property(x => x.Severity).HasConversion<string>();
            b.Property(x => x.RuleCode).HasMaxLength(100);
            b.Property(x => x.ConditionJson).HasColumnType("nvarchar(max)");
            b.Property(x => x.ViolationThreshold).HasPrecision(18, 4);
        });

        modelBuilder.Entity<PolicyViolation>(b =>
        {
            b.ToTable("Compliance_PolicyViolations");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.PolicyCode, x.Status });
            b.HasIndex(x => new { x.TenantId, x.Status, x.DetectedAt });
            b.Property(x => x.Severity).HasConversion<string>();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.PolicyCode).HasMaxLength(100);
            b.Property(x => x.EvidenceJson).HasColumnType("nvarchar(max)");
            b.Property(x => x.Notes).HasMaxLength(2000);
        });

        modelBuilder.Entity<ComplianceScore>(b =>
        {
            b.ToTable("Compliance_ComplianceScores");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.ScopeKey }).IsUnique();
            b.Property(x => x.ScopeKey).HasMaxLength(200);
            b.Property(x => x.Score).HasPrecision(5, 1);
            b.Property(x => x.Level).HasConversion<string>();
            b.Property(x => x.ViolationPenalty).HasPrecision(5, 2);
            b.Property(x => x.OvertimePenalty).HasPrecision(5, 2);
            b.Property(x => x.AuditFindingPenalty).HasPrecision(5, 2);
            b.Property(x => x.PolicyExceptionPenalty).HasPrecision(5, 2);
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<ComplianceScoreHistory>(b =>
        {
            b.ToTable("Compliance_ScoreHistory");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.ScopeKey, x.RecordedAt });
            b.Property(x => x.ScopeKey).HasMaxLength(200);
            b.Property(x => x.Score).HasPrecision(5, 1);
            b.Property(x => x.Level).HasConversion<string>();
        });

        modelBuilder.Entity<ComplianceSnapshot>(b =>
        {
            b.ToTable("Compliance_ComplianceSnapshots");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.SnapshotType, x.PeriodStart, x.PeriodEnd });
            b.Property(x => x.SnapshotType).HasConversion<string>();
            b.Property(x => x.DataJson).HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<AuditPackage>(b =>
        {
            b.ToTable("Compliance_AuditPackages");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.EmployeeId });
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.RequestedBy).HasMaxLength(200);
            b.Property(x => x.PackageUri).HasMaxLength(2000);
            b.Property(x => x.FailureReason).HasMaxLength(1000);
        });
    }
}
