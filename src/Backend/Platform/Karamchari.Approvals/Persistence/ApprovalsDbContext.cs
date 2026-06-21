using Karamchari.Approvals.Domain;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Approvals.Persistence;

public sealed class ApprovalsDbContext : KaramchariDbContext
{
    public ApprovalsDbContext(DbContextOptions<ApprovalsDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<ManagerDelegation> ManagerDelegations => Set<ManagerDelegation>();
    public DbSet<ApprovalPolicy> ApprovalPolicies => Set<ApprovalPolicy>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.Entity<ApprovalRequest>(b =>
        {
            b.ToTable("Approvals_Requests");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.RequestType).HasConversion<string>().HasMaxLength(64).IsRequired();
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            b.Property(x => x.SubjectDescription).HasMaxLength(512);
            b.HasIndex(x => new { x.TenantId, x.ApproverId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.SubmittedBy });

            b.OwnsMany(x => x.Actions, a =>
            {
                a.ToTable("Approvals_Actions");
                a.WithOwner().HasForeignKey("ApprovalRequestId");
                a.HasKey("ApprovalRequestId", "ActedAtUtc");
                a.Property(x => x.ActorName).HasMaxLength(256).IsRequired();
                a.Property(x => x.Decision).HasConversion<string>().HasMaxLength(32).IsRequired();
                a.Property(x => x.Comment).HasMaxLength(1024);
            });
        });

        modelBuilder.Entity<ManagerDelegation>(b =>
        {
            b.ToTable("Approvals_Delegations");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.EffectiveFrom).HasColumnType("date");
            b.Property(x => x.EffectiveTo).HasColumnType("date");
            b.HasIndex(x => new { x.TenantId, x.DelegatingManagerId, x.IsActive });
        });

        modelBuilder.Entity<ApprovalPolicy>(b =>
        {
            b.ToTable("Approvals_Policies");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.Name).HasMaxLength(256).IsRequired();
            b.Property(x => x.Mode).HasConversion<string>().HasMaxLength(32).IsRequired();
            b.Property(x => x.RequestType).HasConversion<string>().HasMaxLength(64).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.RequestType, x.IsDefault });

            b.OwnsMany(x => x.Steps, s =>
            {
                s.ToTable("Approvals_PolicySteps");
                s.WithOwner().HasForeignKey("ApprovalPolicyId");
                s.HasKey("ApprovalPolicyId", "StepOrder");
                s.Property(x => x.ResolverType).HasConversion<string>().HasMaxLength(64).IsRequired();
                s.Property(x => x.RoleName).HasMaxLength(256);
            });
        });
    }
}
