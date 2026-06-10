// -----------------------------------------------------------------------
// <copyright file="WorkflowDbContext.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Workflow.Domain;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Workflow.Persistence;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class WorkflowDbContext : KaramchariDbContext
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);


        modelBuilder.Entity<WorkflowDefinition>(b =>
        {
            b.ToTable("Workflow_Definitions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EntityType, x.IsActive });
            b.HasIndex(x => new { x.TenantId, x.Status }); // governance lifecycle queries
            b.Property(x => x.ConditionsJson)
                .HasColumnType("nvarchar(max)")
                .HasDefaultValueSql("'[]'");
            b.Property(x => x.ExpressionJson)
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);
            b.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasDefaultValueSql("'Published'");
            b.Property(x => x.EffectiveFrom)
                .HasColumnType("datetime2")
                .IsRequired(false);
            b.Property(x => x.EffectiveTo)
                .HasColumnType("datetime2")
                .IsRequired(false);
            b.Ignore(x => x.Conditions);
            b.Ignore(x => x.Expression);
            b.OwnsMany(x => x.Steps, s =>
            {
                s.ToTable("Workflow_Steps");
                s.WithOwner().HasForeignKey("DefinitionId");
                s.HasKey(x => x.Id);
            });

            b.OwnsMany(x => x.ApprovalHistory, a =>
            {
                a.ToTable("Workflow_DefinitionApprovalHistory");
                a.WithOwner().HasForeignKey("DefinitionId");
                a.HasKey(x => x.Id);
                a.Property(x => x.ActorId).HasMaxLength(200).IsRequired();
                a.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(50);
                a.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(50);
                a.Property(x => x.OccurredAt).HasColumnType("datetimeoffset");
                a.Property(x => x.Notes).HasMaxLength(1000).IsRequired(false);
                a.HasIndex("DefinitionId", "OccurredAt");
            });
        });

        modelBuilder.Entity<WorkflowInstance>(b =>
        {
            b.ToTable("Workflow_Instances");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EntityType, x.EntityId });
            b.HasIndex(x => new { x.TenantId, x.Status }); // Index for dashboard lookups
            b.Property(x => x.Status).HasConversion<string>();

            b.OwnsMany(x => x.StepInstances, s =>
            {
                s.ToTable("Workflow_StepInstances");
                s.WithOwner().HasForeignKey("WorkflowInstanceId");
                s.HasKey(x => x.Id);
                s.Property(x => x.Status).HasConversion<string>();

                // PERFORMANCE: Hotspot index for "My Approvals" query
                s.HasIndex(x => new { x.Status, x.ApproverRole });
            });

            b.OwnsMany(x => x.AuditLog, a =>
            {
                a.ToTable("Workflow_AuditLogs");
                a.WithOwner().HasForeignKey("WorkflowInstanceId");
                a.Property<Guid>("Id");
                a.HasKey("Id");
            });
        });
    }
}
