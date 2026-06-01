using System.Text.Json;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Recruitment.Application;
using Karamchari.Recruitment.Domain;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Recruitment.Persistence;

public sealed class RecruitmentDbContext : KaramchariDbContext, IRecruitmentDbContext
{
    public RecruitmentDbContext(DbContextOptions<RecruitmentDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    public DbSet<JobRequisition> Requisitions => Set<JobRequisition>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<Karamchari.Recruitment.Domain.Application> Applications => Set<Karamchari.Recruitment.Domain.Application>();
    public DbSet<Interview> Interviews => Set<Interview>();
    public DbSet<InterviewFeedback> InterviewFeedbacks => Set<InterviewFeedback>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<RecruitmentAuditEntry> AuditStream => Set<RecruitmentAuditEntry>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.Entity<JobRequisition>(b =>
        {
            b.ToTable("Recruitment_Requisitions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasConversion(id => id.Value, value => new RequisitionId(value));
            b.Property(x => x.Title).HasMaxLength(256).IsRequired();
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.HasIndex(x => x.TenantId);
        });

        modelBuilder.Entity<Candidate>(b =>
        {
            b.ToTable("Recruitment_Candidates");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasConversion(id => id.Value, value => new CandidateId(value));
            b.Property(x => x.FirstName).HasMaxLength(128).IsRequired();
            b.Property(x => x.LastName).HasMaxLength(128).IsRequired();
            b.Property(x => x.Email).HasMaxLength(256).IsRequired();
            b.Property(x => x.PhoneNumber).HasMaxLength(64);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.HasIndex(x => x.TenantId);
        });

        modelBuilder.Entity<Karamchari.Recruitment.Domain.Application>(b =>
        {
            b.ToTable("Recruitment_Applications");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasConversion(id => id.Value, value => new Karamchari.Recruitment.Domain.ApplicationId(value));
            b.Property(x => x.CandidateId).HasConversion(id => id.Value, value => new CandidateId(value)).IsRequired();
            b.Property(x => x.RequisitionId).HasConversion(id => id.Value, value => new RequisitionId(value)).IsRequired();
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.HiredBy).HasMaxLength(256);

            b.Property(x => x.CandidateSnapshot)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<CandidateSnapshot>(v, (JsonSerializerOptions?)null)!)
                .HasColumnType("nvarchar(max)")
                .Metadata.SetValueComparer(ValueComparers.ObjectComparer<CandidateSnapshot>());
            
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => new { x.TenantId, x.CandidateId, x.RequisitionId }).IsUnique();
        });

        modelBuilder.Entity<Interview>(b =>
        {
            b.ToTable("Recruitment_Interviews");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasConversion(id => id.Value, value => new InterviewId(value));
            b.Property(x => x.ApplicationId).HasConversion(id => id.Value, value => new Karamchari.Recruitment.Domain.ApplicationId(value)).IsRequired();
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();

            b.Property(x => x.InterviewerIds)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>())
                .HasColumnType("nvarchar(max)")
                .Metadata.SetValueComparer(ValueComparers.ListComparer<Guid>());

            b.HasIndex(x => x.TenantId);
        });

        modelBuilder.Entity<InterviewFeedback>(b =>
        {
            b.ToTable("Recruitment_InterviewFeedback");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasConversion(id => id.Value, value => new InterviewFeedbackId(value));
            b.Property(x => x.InterviewId).HasConversion(id => id.Value, value => new InterviewId(value)).IsRequired();
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.HasIndex(x => x.TenantId);
        });

        modelBuilder.Entity<Offer>(b =>
        {
            b.ToTable("Recruitment_Offers");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasConversion(id => id.Value, value => new OfferId(value));
            b.Property(x => x.ApplicationId).HasConversion(id => id.Value, value => new Karamchari.Recruitment.Domain.ApplicationId(value)).IsRequired();
            b.Property(x => x.BaseSalary).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.HasIndex(x => x.TenantId);
        });

        modelBuilder.Entity<RecruitmentAuditEntry>(b =>
        {
            b.ToTable("Recruitment_AuditStream");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
            b.Property(x => x.Action).HasMaxLength(128).IsRequired();
            b.Property(x => x.UserId).HasMaxLength(256).IsRequired();
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => x.EntityId);
        });
    }
}
