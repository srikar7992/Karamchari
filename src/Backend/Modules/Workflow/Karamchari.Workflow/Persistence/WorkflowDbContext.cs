using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Workflow.Domain;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
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

        const string MessagingSchema = "dbo";

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<InboxState>(b => b.ToTable("InboxState", MessagingSchema, t => t.ExcludeFromMigrations()));
        modelBuilder.Entity<OutboxMessage>(b => b.ToTable("OutboxMessage", MessagingSchema, t => t.ExcludeFromMigrations()));
        modelBuilder.Entity<OutboxState>(b => b.ToTable("OutboxState", MessagingSchema, t => t.ExcludeFromMigrations()));

        modelBuilder.Entity<WorkflowDefinition>(b =>
        {
            b.ToTable("Workflow_Definitions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EntityType, x.IsActive });
            b.OwnsMany(x => x.Steps, s =>
            {
                s.ToTable("Workflow_Steps");
                s.WithOwner().HasForeignKey("DefinitionId");
                s.HasKey(x => x.Id);
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
