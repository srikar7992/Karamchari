using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Engagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Engagement.Persistence;

public sealed class EngagementDbContext : KaramchariDbContext
{
    public EngagementDbContext(DbContextOptions<EngagementDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider) { }

    public DbSet<PulseSurvey> PulseSurveys => Set<PulseSurvey>();
    public DbSet<EmployeeRecognition> Recognitions => Set<EmployeeRecognition>();
    public DbSet<RecognitionBadge> Badges => Set<RecognitionBadge>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<Poll> Polls => Set<Poll>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.Entity<RecognitionBadge>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(id => id.Value, v => new BadgeId(v));
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.IconUrl).HasMaxLength(500);
            e.Property(x => x.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<EmployeeRecognition>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(id => id.Value, v => new RecognitionId(v));
            e.Property(x => x.BadgeId).HasConversion(id => id != null ? id.Value : (Guid?)null, v => v.HasValue ? new BadgeId(v.Value) : null);
            e.Property(x => x.Message).HasMaxLength(2000).IsRequired();
            e.OwnsMany(x => x.Reactions, r =>
            {
                r.WithOwner().HasForeignKey("RecognitionId");
                r.HasKey(x => x.EmployeeId);
                r.Property(x => x.Emoji).HasMaxLength(10);
            });
        });

        modelBuilder.Entity<Announcement>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(id => id.Value, v => new AnnouncementId(v));
            e.Property(x => x.Title).HasMaxLength(500).IsRequired();
            e.Property(x => x.Body).HasMaxLength(10000).IsRequired();
            e.Property(x => x.Audience).HasConversion<string>();
            e.Ignore(x => x.IsExpired);
        });

        modelBuilder.Entity<PulseSurvey>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(id => id.Value, v => new PulseSurveyId(v));
            e.Property(x => x.Title).HasMaxLength(500).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Status).HasConversion<string>();
            e.Ignore(x => x.ResponseCount);
            e.OwnsMany(x => x.Questions, q =>
            {
                q.WithOwner().HasForeignKey("SurveyId");
                q.HasKey(x => x.Id);
                q.Property(x => x.SurveyId).HasConversion(id => id.Value, v => new PulseSurveyId(v));
                q.Property(x => x.Text).HasMaxLength(1000).IsRequired();
                q.Property(x => x.Type).HasConversion<string>();
                q.Property(x => x.Choices).HasConversion(
                    v => string.Join("|||", v),
                    v => v.Split("|||", StringSplitOptions.None));
            });
            e.OwnsMany(x => x.Responses, r =>
            {
                r.WithOwner().HasForeignKey("SurveyRootId");
                r.HasKey(x => x.Id);
                r.Property(x => x.SurveyId).HasConversion(id => id.Value, v => new PulseSurveyId(v));
                r.Property(x => x.Answers).HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<Guid, string>>(v, (System.Text.Json.JsonSerializerOptions?)null)!);
            });
        });

        modelBuilder.Entity<Poll>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(id => id.Value, v => new PollId(v));
            e.Property(x => x.Question).HasMaxLength(1000).IsRequired();
            e.Property(x => x.Choices).HasConversion(
                v => string.Join("|||", v),
                v => v.Split("|||", StringSplitOptions.None));
            e.Ignore(x => x.IsClosed);
            e.OwnsMany(x => x.Votes, v =>
            {
                v.WithOwner().HasForeignKey("PollId");
                v.HasKey(x => x.EmployeeId);
            });
        });
    }
}
