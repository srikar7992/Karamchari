using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Succession.Domain;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Succession.Persistence;

public sealed class SuccessionDbContext : KaramchariDbContext
{
    public SuccessionDbContext(DbContextOptions<SuccessionDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider) { }

    public DbSet<CriticalRole> CriticalRoles => Set<CriticalRole>();
    public DbSet<SuccessionPlan> SuccessionPlans => Set<SuccessionPlan>();
    public DbSet<TalentPool> TalentPools => Set<TalentPool>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.Entity<CriticalRole>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(id => id.Value, v => new CriticalRoleId(v));
            e.Property(x => x.RoleTitle).HasMaxLength(300).IsRequired();
            e.Property(x => x.Department).HasMaxLength(200);
            e.Property(x => x.Rationale).HasMaxLength(2000);
            e.Property(x => x.Risk).HasConversion<string>();
        });

        modelBuilder.Entity<SuccessionPlan>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(id => id.Value, v => new SuccessionPlanId(v));
            e.Property(x => x.RoleId).HasConversion(id => id.Value, v => new CriticalRoleId(v));
            e.OwnsMany(x => x.Candidates, c =>
            {
                c.WithOwner().HasForeignKey("PlanId_fk");
                c.HasKey(x => x.Id);
                c.Property(x => x.Id).HasConversion(id => id.Value, v => new SuccessorCandidateId(v));
                c.Property(x => x.PlanId).HasConversion(id => id.Value, v => new SuccessionPlanId(v));
                c.Property(x => x.Readiness).HasConversion<string>();
                c.Property(x => x.Status).HasConversion<string>();
                c.Property(x => x.DevelopmentNotes).HasMaxLength(2000);
                c.Property(x => x.DevelopmentGaps).HasConversion(
                    v => string.Join("|||", v),
                    v => v.Length == 0 ? new List<string>() : v.Split("|||", StringSplitOptions.None).ToList());
            });
        });

        modelBuilder.Entity<TalentPool>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(id => id.Value, v => new TalentPoolId(v));
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.TargetJobFamily).HasMaxLength(100);
            e.Property(x => x.Status).HasConversion<string>();
            e.Ignore(x => x.RankedMembers);

            e.OwnsMany(x => x.Members, m =>
            {
                m.WithOwner().HasForeignKey("PoolId_fk");
                m.HasKey(x => x.Id);
                m.Property(x => x.Id).HasConversion(id => id.Value, v => new TalentPoolMemberId(v));
                m.Property(x => x.PoolId).HasConversion(id => id.Value, v => new TalentPoolId(v));
                m.Property(x => x.EstimatedReadiness).HasConversion<string>();
                m.Property(x => x.Status).HasConversion<string>();
                m.Property(x => x.NominationNotes).HasMaxLength(2000);
                m.Property(x => x.ExitReason).HasMaxLength(500);
                m.Ignore(x => x.NineBoxPosition());
            });
        });
    }
}
