// -----------------------------------------------------------------------
// <copyright file="KaramchariDbContext.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
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
///   <item>Base context adds MassTransit's transactional outbox entities
///         (<c>InboxState</c>, <c>OutboxMessage</c>, <c>OutboxState</c>) and
///         pins them to <c>dbo</c> — the bus outbox is shared infrastructure,
///         not tenant-owned. Migrations are excluded by default; one context
///         (typically HR) should override this to own the physical tables.</item>
/// </list>
///
/// Derived contexts (e.g. <c>HRDbContext</c>) override <see cref="OnDomainModelCreating"/>
/// — they must <b>not</b> override <see cref="OnModelCreating"/> directly, since
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

    /// <summary>
    /// Schema used in <see cref="OnModelCreating"/>. Returns <see cref="PlaceholderSchema"/>
    /// at runtime. Design-time factories override this to <c>"dbo"</c> so that
    /// <c>dotnet ef migrations add</c> generates migrations against the physical schema —
    /// EF 10 does not support JSON columns on owned entities mapped to non-<c>dbo</c> schemas
    /// at design time.
    /// </summary>
    protected virtual string ModelSchema => PlaceholderSchema;

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

    /// <summary>
    /// Gets the bi-temporal stored events.
    /// </summary>
    public DbSet<EventSourcing.StoredEvent> StoredEvents => Set<EventSourcing.StoredEvent>();

    /// <summary>
    /// Gets the platform approval definitions.
    /// </summary>
    public DbSet<Approvals.ApprovalDefinition> ApprovalDefinitions => Set<Approvals.ApprovalDefinition>();

    /// <summary>
    /// Gets the platform approval instances.
    /// </summary>
    public DbSet<Approvals.ApprovalInstance> ApprovalInstances => Set<Approvals.ApprovalInstance>();

    /// <summary>
    /// Gets the platform approval delegations.
    /// </summary>
    public DbSet<Approvals.ApprovalDelegation> PlatformApprovalDelegations => Set<Approvals.ApprovalDelegation>();

    /// <summary>
    /// Gets the platform projection checkpoints.
    /// </summary>
    public DbSet<Projections.ProjectionCheckpoint> ProjectionCheckpoints => Set<Projections.ProjectionCheckpoint>();

    /// <summary>
    /// Gets the platform projection dead letters.
    /// </summary>
    public DbSet<Projections.ProjectionDeadLetter> ProjectionDeadLetters => Set<Projections.ProjectionDeadLetter>();

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
        modelBuilder.HasDefaultSchema(ModelSchema);

        // Register bi-temporal events under the tenant schema
        modelBuilder.Entity<EventSourcing.StoredEvent>(builder =>
        {
            builder.ToTable("StoredEvents");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(64);
            builder.Property(x => x.AggregateType).IsRequired().HasMaxLength(128);
            builder.Property(x => x.EventType).IsRequired().HasMaxLength(128);
            builder.Property(x => x.EventDataJson).IsRequired();
            builder.Property(x => x.MetadataJson).IsRequired().HasMaxLength(4000);
            builder.Property(x => x.Position).ValueGeneratedOnAdd();
            builder.HasIndex(x => new { x.TenantId, x.AggregateId });
            builder.HasIndex(x => new { x.TenantId, x.AggregateId, x.EffectiveUtc });
            builder.HasIndex(x => new { x.TenantId, x.AggregateId, x.Version }).IsUnique().HasDatabaseName("IX_StoredEvents_AggregateId_Version");
            builder.HasIndex(x => new { x.TenantId, x.CorrelationId }).HasDatabaseName("IX_StoredEvents_CorrelationId");
            builder.HasIndex(x => x.Position).IsUnique().HasDatabaseName("IX_StoredEvents_Position");
        });

        // Register approval entities
        modelBuilder.Entity<Approvals.ApprovalDefinition>(builder =>
        {
            builder.ToTable("ApprovalDefinitions");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(64);
            builder.Property(x => x.Domain).IsRequired().HasMaxLength(128);
            builder.Property(x => x.ResourceType).IsRequired().HasMaxLength(128);
            builder.HasMany(x => x.Stages).WithOne().HasForeignKey(x => x.ApprovalDefinitionId);
        });

        modelBuilder.Entity<Approvals.ApprovalStage>(builder =>
        {
            builder.ToTable("ApprovalStages");
            builder.HasKey(x => x.Id);
        });

        modelBuilder.Entity<Approvals.ApprovalInstance>(builder =>
        {
            builder.ToTable("ApprovalInstances");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(64);
            builder.HasIndex(x => new { x.TenantId, x.ResourceId }).HasDatabaseName("IX_ApprovalInstance_ResourceId");
            builder.HasIndex(x => new { x.TenantId, x.Status }).HasDatabaseName("IX_ApprovalInstance_Status");
            builder.HasIndex(x => new { x.TenantId, x.Status, x.DueUtc }).HasDatabaseName("IX_ApprovalInstance_Queue");
            builder.HasMany(x => x.Decisions).WithOne().HasForeignKey(x => x.ApprovalInstanceId);
        });

        modelBuilder.Entity<Approvals.ApprovalDecision>(builder =>
        {
            builder.ToTable("ApprovalDecisions");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Action).IsRequired().HasMaxLength(64);
        });

        modelBuilder.Entity<Approvals.ApprovalDelegation>(builder =>
        {
            builder.ToTable("ApprovalDelegations");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(64);
            builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(64);
            builder.Property(x => x.Reason).IsRequired().HasMaxLength(500);
            builder.HasIndex(x => new { x.TenantId, x.DelegatorId });
            builder.HasIndex(x => new { x.TenantId, x.DelegateId });
        });

        // Register projection checkpoints
        modelBuilder.Entity<Projections.ProjectionCheckpoint>(builder =>
        {
            builder.ToTable("ProjectionCheckpoints");
            builder.HasKey(x => new { x.ProjectionName, x.PartitionKey });
            builder.Property(x => x.ProjectionName).HasMaxLength(256);
            builder.Property(x => x.PartitionKey).HasMaxLength(64);
        });

        // Register projection dead letters
        modelBuilder.Entity<Projections.ProjectionDeadLetter>(builder =>
        {
            builder.ToTable("ProjectionDeadLetters");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(64);
            builder.Property(x => x.ProjectionName).IsRequired().HasMaxLength(256);
            builder.Property(x => x.EventType).IsRequired().HasMaxLength(128);
            builder.Property(x => x.EventPayloadJson).IsRequired();
            builder.Property(x => x.ExceptionMessage).IsRequired();
            builder.Property(x => x.ResolvedBy).HasMaxLength(64);
            builder.HasIndex(x => new { x.TenantId, x.ProjectionName, x.FailedUtc }, "IX_ProjectionDeadLetter_Queue");
        });

        // Register MassTransit outbox entities in the base model so they're available
        // to every context, preventing "type not included in model" runtime faults.
        // We pin them to dbo and exclude from migrations by default.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<InboxState>(b => b.ToTable("InboxState", "dbo", t => t.ExcludeFromMigrations()));
        modelBuilder.Entity<OutboxMessage>(b => b.ToTable("OutboxMessage", "dbo", t => t.ExcludeFromMigrations()));
        modelBuilder.Entity<OutboxState>(b => b.ToTable("OutboxState", "dbo", t => t.ExcludeFromMigrations()));

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
