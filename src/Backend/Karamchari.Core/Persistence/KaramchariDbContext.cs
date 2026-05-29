using Karamchari.Core.Multitenancy;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Core.Persistence;

/// <summary>
/// Base <see cref="DbContext"/> for every bounded-context DbContext in
/// Karamchari. Owns the multi-tenancy contract:
///
/// <list type="bullet">
///   <item>All tables map to the placeholder schema <see cref="PlaceholderSchema"/>.</item>
///   <item><see cref="Interceptors.TenantSchemaCommandInterceptor"/> rewrites
///         that placeholder to the active tenant's schema at command execution time.</item>
///   <item>Derived contexts add MassTransit's transactional outbox entities
///         (<c>InboxState</c>, <c>OutboxMessage</c>, <c>OutboxState</c>) and
///         pin them to <c>dbo</c> Ã¢â‚¬â€ the bus outbox is shared infrastructure,
///         not tenant-owned.</item>
/// </list>
///
/// Derived contexts (e.g. <c>HRDbContext</c>) override <see cref="OnDomainModelCreating"/>
/// Ã¢â‚¬â€ they must <b>not</b> override <see cref="OnModelCreating"/> directly, since
/// it's sealed to guarantee the multi-tenant defaults are always applied first.
/// </summary>
public abstract class KaramchariDbContext : DbContext
{
    /// <summary>
    /// The schema name baked into the EF model. The
    /// <c>TenantSchemaCommandInterceptor</c> replaces it with the active
    /// tenant's schema right before the SQL command executes.
    ///
    /// Chosen to be visually distinct from any plausible real schema name so
    /// rewriting and logging are unambiguous.
    /// </summary>
    public const string PlaceholderSchema = "__tenant__";

    private readonly ITenantProvider _tenantProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="KaramchariDbContext"/> class.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    /// <param name="tenantProvider">The tenant provider to resolve the active tenant.</param>
    protected KaramchariDbContext(DbContextOptions options, ITenantProvider tenantProvider)
        : base(options)
    {
        ArgumentNullException.ThrowIfNull(tenantProvider);
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Exposed to interceptors (and to derived contexts that need to log under tenant scope)
    /// without leaking the provider as a public surface.
    /// </summary>
    internal ITenantProvider TenantProvider => _tenantProvider;

    /// <summary>
    /// Gets the current tenant execution envelope.
    /// </summary>
    protected TenantExecutionEnvelope TenantEnvelope => _tenantProvider.GetTenant();

    /// <summary>
    /// Gets the current tenant ID. Used by global query filters.
    /// </summary>
    public string CurrentTenantId => _tenantProvider.GetCurrentTenantId();

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Suppress pending model changes warning, because we intentionally
        // exclude outbox entities at design-time but include them at runtime.
        optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        base.ConfigureConventions(configurationBuilder);

        // FINANCIAL DATA INTEGRITY: give every decimal an explicit precision/scale so SQL Server
        // never silently truncates payroll/billing/financial values. (18,2) matches the existing
        // SQL Server default column type — making it explicit removes the "no store type specified"
        // warnings without changing physical storage. Properties that declare their own
        // HasPrecision/HasColumnType still win (explicit configuration overrides conventions).
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        configurationBuilder.Properties<decimal?>().HavePrecision(18, 2);
    }

    /// <summary>
    /// Configures the default schema and applies domain model configurations.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Every entity in this context lives under the placeholder schema by default.
        // Derived contexts can still override on a per-entity basis (e.g. pinning
        // MassTransit's bus outbox tables to dbo), but must keep PlaceholderSchema
        // as the default so the interceptor's rewrite remains consistent.
        modelBuilder.HasDefaultSchema(PlaceholderSchema);

        OnDomainModelCreating(modelBuilder);

        // Apply global query filters to all ITenantOwned entities.
        // This is defence-in-depth on top of schema isolation and RLS.
        // We do this AFTER OnDomainModelCreating so that owned types are already configured.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantOwned).IsAssignableFrom(entityType.ClrType))
            {
                // Skip owned types - the owner already has the filter applied, 
                // and EF doesn't support query filters on owned types in some configurations.
                if (entityType.IsOwned())
                {
                    continue;
                }

                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var body = System.Linq.Expressions.Expression.Equal(
                    System.Linq.Expressions.Expression.Property(parameter, nameof(ITenantOwned.TenantId)),
                    System.Linq.Expressions.Expression.Property(System.Linq.Expressions.Expression.Constant(this), nameof(CurrentTenantId))
                );
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(System.Linq.Expressions.Expression.Lambda(body, parameter));
            }
        }
    }

    /// <summary>
    /// Bounded-context hook for entity configuration. Derived contexts override
    /// this instead of <see cref="OnModelCreating"/> so the multi-tenant defaults
    /// are guaranteed to be applied first.
    /// </summary>
    protected virtual void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
    }
}
