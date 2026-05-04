using Karamchari.Core.Multitenancy;
using Karamchari.Payroll.Domain;
using Karamchari.Payroll.StateMachines;
using Microsoft.EntityFrameworkCore;

using Karamchari.Core.Persistence;

namespace Karamchari.Payroll.Data;

/// <summary>
/// Database context for the Payroll bounded context.
/// </summary>
public class PayrollDbContext : KaramchariDbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PayrollDbContext"/> class.
    /// </summary>
    /// <param name="options">The context options.</param>
    /// <param name="tenantProvider">The tenant provider.</param>
    public PayrollDbContext(DbContextOptions<PayrollDbContext> options, ITenantProvider tenantProvider) 
        : base(options, tenantProvider)
    {
    }

    /// <summary>
    /// Gets the payroll profiles set.
    /// </summary>
    public DbSet<PayrollProfile> PayrollProfiles => Set<PayrollProfile>();
    /// <summary>
    /// Gets the payroll run states set (Saga state).
    /// </summary>
    public DbSet<PayrollRunState> PayrollRunStates => Set<PayrollRunState>();

    /// <summary>
    /// Configures the domain model for the Payroll context.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.Entity<PayrollProfile>(b =>
        {
            b.ToTable("PayrollProfiles");
            b.HasKey(x => x.Id);
            // In a real app, you'd apply the RLS function here via EF Core mapping
        });

        modelBuilder.Entity<PayrollRunState>(b =>
        {
            b.ToTable("PayrollRunStates");
            b.HasKey(x => x.CorrelationId);
            b.Property(x => x.CurrentState).HasMaxLength(64);
            // In a real app, you'd apply the RLS function here via EF Core mapping
        });
    }
}
