using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Helpdesk.Domain;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Helpdesk.Persistence;

public sealed class HelpdeskDbContext : KaramchariDbContext
{
    public HelpdeskDbContext(DbContextOptions<HelpdeskDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider) { }

    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<TicketCategory> TicketCategories => Set<TicketCategory>();
    public DbSet<KbArticle> KbArticles => Set<KbArticle>();
    public DbSet<EscalationRule> EscalationRules => Set<EscalationRule>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.Entity<TicketCategory>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(id => id.Value, v => new TicketCategoryId(v));
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.DefaultAssigneeTeam).HasMaxLength(200);
        });

        modelBuilder.Entity<SupportTicket>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(id => id.Value, v => new SupportTicketId(v));
            e.Property(x => x.CategoryId).HasConversion(id => id.Value, v => new TicketCategoryId(v));
            e.Property(x => x.Subject).HasMaxLength(500).IsRequired();
            e.Property(x => x.Description).HasMaxLength(4000);
            e.Property(x => x.Priority).HasConversion<string>();
            e.Property(x => x.Status).HasConversion<string>();
            e.OwnsMany(x => x.Comments, c =>
            {
                c.WithOwner().HasForeignKey("TicketId");
                c.HasKey(x => x.Id);
                c.Property(x => x.Body).HasMaxLength(4000);
            });
            e.Ignore(x => x.IsSlaBreached);
        });

        modelBuilder.Entity<KbArticle>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(id => id.Value, v => new KbArticleId(v));
            e.Property(x => x.CategoryId).HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                v => v.HasValue ? new TicketCategoryId(v.Value) : null);
            e.Property(x => x.Title).HasMaxLength(500).IsRequired();
            e.Property(x => x.Body).HasMaxLength(50000).IsRequired();
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.Tags).HasConversion(
                v => string.Join("|||", v),
                v => v.Length == 0 ? Array.Empty<string>() : v.Split("|||", StringSplitOptions.None));
            e.Ignore(x => x.HelpfulnessRatio);
        });

        modelBuilder.Entity<EscalationRule>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(id => id.Value, v => new EscalationRuleId(v));
            e.Property(x => x.RuleName).HasMaxLength(300).IsRequired();
            e.Property(x => x.AppliesToPriority).HasConversion<string>();
            e.Property(x => x.Target).HasConversion<string>();
        });
    }
}
