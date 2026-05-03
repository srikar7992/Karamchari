using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.HR.Domain.Employees;
using Karamchari.HR.Persistence.Configurations;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.HR.Persistence;

public sealed class HRDbContext : KaramchariDbContext
{
    public HRDbContext(DbContextOptions<HRDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    /// <summary>
    /// Schema where MassTransit's transactional outbox tables live. They're
    /// shared infrastructure — one queue serves every tenant and the relay
    /// process drains them globally, so they must NOT be subject to per-tenant
    /// schema rewriting.
    /// </summary>
    private const string MessagingSchema = "dbo";

    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new EmployeeConfiguration());

        // MassTransit's transactional outbox entities. Adding them registers the
        // EF model types; pinning them to dbo keeps them out of the tenant
        // schema rewrite path (the rewriter only substitutes KaramchariDbContext
        // .PlaceholderSchema, so [dbo].[OutboxMessage] passes through untouched).
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<InboxState>(b => b.ToTable("InboxState", MessagingSchema));
        modelBuilder.Entity<OutboxMessage>(b => b.ToTable("OutboxMessage", MessagingSchema));
        modelBuilder.Entity<OutboxState>(b => b.ToTable("OutboxState", MessagingSchema));
    }
}
