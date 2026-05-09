using Karamchari.Capability.Domain.Growth;
using Karamchari.Capability.Domain.Learning;
using Karamchari.Capability.Domain.Skills;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Capability.Persistence;

public class CapabilityDbContext : KaramchariDbContext
{
    public CapabilityDbContext(DbContextOptions<CapabilityDbContext> options, ITenantProvider tenantProvider) 
        : base(options, tenantProvider)
    {
    }

    public DbSet<SkillDefinition> SkillDefinitions => Set<SkillDefinition>();
    public DbSet<CapabilityProfile> CapabilityProfiles => Set<CapabilityProfile>();
    public DbSet<LearningModule> LearningModules => Set<LearningModule>();
    public DbSet<LearningEnrollment> LearningEnrollments => Set<LearningEnrollment>();
    public DbSet<CertificationAchievement> CertificationAchievements => Set<CertificationAchievement>();
    public DbSet<GrowthPlan> GrowthPlans => Set<GrowthPlan>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        const string MessagingSchema = "dbo";
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        modelBuilder.Entity<InboxState>(b => b.ToTable("InboxState", MessagingSchema));
        modelBuilder.Entity<OutboxMessage>(b => b.ToTable("OutboxMessage", MessagingSchema));
        modelBuilder.Entity<OutboxState>(b => b.ToTable("OutboxState", MessagingSchema));

        modelBuilder.Entity<SkillDefinition>(b =>
        {
            b.ToTable("Capability_SkillDefinitions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            b.Property(x => x.RowVersion).IsRowVersion();
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
            });
        });

        modelBuilder.Entity<LearningModule>(b =>
        {
            b.ToTable("Capability_LearningModules");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Title });
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<LearningEnrollment>(b =>
        {
            b.ToTable("Capability_LearningEnrollments");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.ModuleId }).IsUnique();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<CertificationAchievement>(b =>
        {
            b.ToTable("Capability_CertificationAchievements");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId });
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.RowVersion).IsRowVersion();
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
    }
}
