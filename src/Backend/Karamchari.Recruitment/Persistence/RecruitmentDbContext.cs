using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Recruitment.Domain.Candidates;
using Karamchari.Recruitment.Domain.Interviews;
using Karamchari.Recruitment.Domain.Offers;
using Karamchari.Recruitment.Domain.Pipelines;
using Karamchari.Recruitment.Domain.Requisitions;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Recruitment.Persistence;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public class RecruitmentDbContext : KaramchariDbContext
{
    /// <inheritdoc/>
    public RecruitmentDbContext(DbContextOptions<RecruitmentDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<JobRequisition> JobRequisitions => Set<JobRequisition>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<CandidateApplication> CandidateApplications => Set<CandidateApplication>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<InterviewSession> InterviewSessions => Set<InterviewSession>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<OfferLetter> OfferLetters => Set<OfferLetter>();

    /// <inheritdoc/>
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

        modelBuilder.Entity<JobRequisition>(b =>
        {
            b.ToTable("Recruitment_JobRequisitions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Status });
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Priority).HasConversion<string>();
            b.Property(x => x.RowVersion).IsRowVersion();

            b.OwnsOne(x => x.Budget, cb =>
            {
                cb.Property(p => p.Min).HasPrecision(18, 2);
                cb.Property(p => p.Max).HasPrecision(18, 2);
                cb.Property(p => p.Currency).HasMaxLength(3);
            });
        });

        modelBuilder.Entity<CandidateProfile>(b =>
        {
            b.ToTable("Recruitment_CandidateProfiles");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Email }).IsUnique(); // Anti-duplication safeguard
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<CandidateApplication>(b =>
        {
            b.ToTable("Recruitment_CandidateApplications");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.RequisitionId, x.CandidateId }).IsUnique(); // Anti-duplication safeguard
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Source).HasConversion<string>();
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<PipelineDefinition>(b =>
        {
            b.ToTable("Recruitment_PipelineDefinitions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();

            b.OwnsMany(x => x.Stages, s =>
            {
                s.ToTable("Recruitment_PipelineStages");
                s.WithOwner().HasForeignKey("PipelineId");
                s.HasKey(x => x.Id);
            });
        });

        modelBuilder.Entity<InterviewSession>(b =>
        {
            b.ToTable("Recruitment_InterviewSessions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.RowVersion).IsRowVersion();

            b.OwnsMany(x => x.Feedback, fb =>
            {
                fb.ToTable("Recruitment_InterviewFeedback");
                fb.WithOwner().HasForeignKey("SessionId");
                fb.HasKey(x => x.Id);
                fb.HasIndex(x => new { x.SessionId, x.InterviewerId }).IsUnique();
            });
        });

        modelBuilder.Entity<OfferLetter>(b =>
        {
            b.ToTable("Recruitment_OfferLetters");
            b.HasKey(x => x.Id);
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.BaseSalary).HasPrecision(18, 2);
            b.Property(x => x.EquityGrant).HasPrecision(18, 2);
            b.Property(x => x.SignOnBonus).HasPrecision(18, 2);
            b.Property(x => x.RowVersion).IsRowVersion();

            b.OwnsMany(x => x.ApprovalChain, ac =>
            {
                ac.ToTable("Recruitment_OfferApprovalSteps");
                ac.WithOwner().HasForeignKey("OfferId");
                ac.HasKey("OfferId", "Sequence");
                ac.Property(x => x.Status).HasConversion<string>();
            });
        });
    }
}
