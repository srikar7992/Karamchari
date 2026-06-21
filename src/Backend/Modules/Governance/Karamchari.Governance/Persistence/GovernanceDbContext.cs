// -----------------------------------------------------------------------
// <copyright file="GovernanceDbContext.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Governance.Domain.AuditTrail;
using Karamchari.Governance.Domain.Contracts;
using Karamchari.Governance.Domain.Controls;
using Karamchari.Governance.Domain.Incidents;
using Karamchari.Governance.Domain.Reliability;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Governance.Persistence;

public class GovernanceDbContext : KaramchariDbContext
{
    public GovernanceDbContext(DbContextOptions<GovernanceDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider) { }

    public DbSet<ServiceLevelObjective> ServiceLevelObjectives => Set<ServiceLevelObjective>();
    public DbSet<OperationalIncident> OperationalIncidents => Set<OperationalIncident>();
    public DbSet<SchemaDefinition> SchemaDefinitions => Set<SchemaDefinition>();
    public DbSet<FinancialAuditEntry> FinancialAuditEntries => Set<FinancialAuditEntry>();
    public DbSet<SodMatrix> SodMatrices => Set<SodMatrix>();
    public DbSet<SodViolation> SodViolations => Set<SodViolation>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.Entity<ServiceLevelObjective>(b =>
        {
            b.ToTable("Governance_ServiceLevelObjectives");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.ComponentName }).IsUnique();
            b.Property(x => x.Tier).HasConversion<string>();
            b.Property(x => x.TargetSuccessRate).HasPrecision(5, 2);
            b.Property(x => x.CurrentSuccessRate).HasPrecision(5, 2);
            b.Property(x => x.ErrorBudgetRemainingPercent).HasPrecision(5, 2);
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<OperationalIncident>(b =>
        {
            b.ToTable("Governance_OperationalIncidents");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Status });
            b.Property(x => x.Severity).HasConversion<string>();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<SchemaDefinition>(b =>
        {
            b.ToTable("Governance_SchemaDefinitions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.ContractName, x.Version }).IsUnique();
            b.Property(x => x.CompatibilityRule).HasConversion<string>();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        // Audit trail — no global query filter, admin queries across tenants
        modelBuilder.Entity<FinancialAuditEntry>(b =>
        {
            b.ToTable("financial_audit_entries", "audit");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.EntityType).HasMaxLength(256).IsRequired();
            b.Property(x => x.EntityId).HasMaxLength(128).IsRequired();
            b.Property(x => x.Operation).HasConversion<string>().HasMaxLength(32);
            b.Property(x => x.ActorName).HasMaxLength(256).IsRequired();
            b.Property(x => x.CorrelationId).HasMaxLength(256);
            b.HasIndex(x => new { x.TenantId, x.EntityType, x.EntityId });
            b.HasIndex(x => new { x.TenantId, x.ActorId, x.TimestampUtc });
        });

        modelBuilder.Entity<SodMatrix>(b =>
        {
            b.ToTable("Governance_SodMatrix");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.HasIndex(x => x.TenantId).IsUnique();
            b.OwnsMany(x => x.Rules, r =>
            {
                r.ToTable("Governance_SodRules");
                r.WithOwner().HasForeignKey("SodMatrixId");
                r.HasKey("SodMatrixId", "Role1", "Role2");
                r.Property(x => x.Role1).HasMaxLength(128);
                r.Property(x => x.Role2).HasMaxLength(128);
                r.Property(x => x.Reason).HasMaxLength(512);
            });
        });

        modelBuilder.Entity<SodViolation>(b =>
        {
            b.ToTable("Governance_SodViolations");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.Role1).HasMaxLength(128).IsRequired();
            b.Property(x => x.Role2).HasMaxLength(128).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.DetectedAt });
        });
    }
}
