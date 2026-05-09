using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.HR.Domain.Departments;
using Karamchari.HR.Domain.Employees;
using Karamchari.HR.Domain.Relationships;
using Karamchari.HR.Persistence.Configurations;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.HR.Persistence;

/// <summary>
/// Database context for the Human Resources bounded context.
/// Inherits from <see cref="KaramchariDbContext"/> to leverage multi-tenancy and RLS.
/// </summary>
public sealed class HRDbContext : KaramchariDbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HRDbContext"/> class.
    /// </summary>
    /// <param name="options">The context options.</param>
    /// <param name="tenantProvider">The tenant provider.</param>
    public HRDbContext(DbContextOptions<HRDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    /// <summary>
    /// Schema where MassTransit's transactional outbox tables live. They're
    /// shared infrastructure Ã¢â‚¬â€ one queue serves every tenant and the relay
    /// process drains them globally, so they must NOT be subject to per-tenant
    /// schema rewriting.
    /// </summary>
    private const string MessagingSchema = "dbo";

    /// <summary>
    /// Gets the departments set.
    /// </summary>
    public DbSet<Department> Departments => Set<Department>();

    /// <summary>
    /// Gets the employees set.
    /// </summary>
    public DbSet<Employee> Employees => Set<Employee>();

    /// <summary>Gets the employee relationships set (org graph: dotted-line, project, mentor, etc.).</summary>
    public DbSet<EmployeeRelationship> EmployeeRelationships => Set<EmployeeRelationship>();

    /// <summary>
    /// Configures the domain model for the HR context.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfiguration(new DepartmentConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeRelationshipConfiguration());

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
