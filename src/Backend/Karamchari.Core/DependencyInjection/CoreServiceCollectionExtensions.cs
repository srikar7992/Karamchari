using Karamchari.Core.Messaging;
using Karamchari.Core.Messaging.Outbox;
using Karamchari.Core.Middleware;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Core.Persistence.Interceptors;
using Karamchari.Core.Persistence.Provisioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Karamchari.Core.DependencyInjection;

/// <summary>
/// Composition root for <c>Karamchari.Core</c>. Bounded-context startup
/// (<c>Karamchari.Api/Program.cs</c>) calls <see cref="AddKaramchariCore"/>
/// once; each context's <c>AddDbContext</c> then attaches the registered
/// interceptors via <see cref="AddKaramchariInterceptors"/>.
/// </summary>
public static class CoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers the multi-tenancy stack, the EF Core interceptors, and a
    /// <see cref="TimeProvider"/> for deterministic timestamps.
    /// </summary>
    public static IServiceCollection AddKaramchariCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Core Infrastructure DbContext (Shared tables)
        services.AddDbContext<CoreDbContext>((sp, opts) =>
        {
            opts.UseSqlServer(configuration.GetConnectionString("KaramchariDb"));
        });

        // Tenancy options â€” strongly typed, validated on first resolution.
        services
            .AddOptions<TenantOptions>()
            .Bind(configuration.GetSection(TenantOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // HttpContextAccessor is required by HttpTenantProvider. AddHttpContextAccessor
        // is a no-op if already registered, so it's safe to call from Core.
        services.AddHttpContextAccessor();

        // Tenant provider must be Scoped Ã¢â‚¬â€ it caches the resolution on HttpContext.Items.
        services.AddScoped<ITenantProvider, HttpTenantProvider>();

        // Interceptors are stateless w.r.t. EF, but they depend on the scoped
        // ITenantProvider, so they must themselves be Scoped.
        services.AddScoped<RlsSessionContextInterceptor>();
        services.AddScoped<TenantStampingInterceptor>();
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<DomainEventDispatchInterceptor>();

        // Observability & Traceability Foundations
        services.AddSingleton<Karamchari.Core.Observability.Tenant.TenantActivitySource>();
        services.AddSingleton<Karamchari.Core.Observability.Tenant.TenantCorrelationPropagator>();
        services.AddSingleton<Karamchari.Core.Observability.Tenant.TenantMetricsCollector>();
        services.AddSingleton<Karamchari.Core.Messaging.Tenant.ReplayProtectionService>();

        // Messaging Context & Filters
        services.AddScoped<Karamchari.Core.Messaging.Tenant.TenantMessageConsumerScope>();
        services.AddScoped<Karamchari.Core.Messaging.Tenant.ITenantMessageConsumerScopeFactory, Karamchari.Core.Messaging.Tenant.TenantMessageConsumerScopeFactory>();
        services.AddSingleton<Karamchari.Core.Messaging.Tenant.TenantConsumeFilter>();
        services.AddSingleton<Karamchari.Core.Messaging.Tenant.TenantPublishFilter>();
        services.AddSingleton<Karamchari.Core.Messaging.Tenant.TenantSendFilter>();

        // Background Job Infrastructure
        services.AddSingleton<Karamchari.Core.BackgroundJobs.Tenant.ITenantJobContextSerializer, Karamchari.Core.BackgroundJobs.Tenant.TenantJobContextSerializer>();
        services.AddScoped<Karamchari.Core.BackgroundJobs.Tenant.BackgroundJobTenantMiddleware>();

        // Default no-op dispatcher: a bounded context (or the API host) MUST
        // override this with a real implementation (e.g. MassTransitDomainEventDispatcher
        // in HR) before any aggregate is saved with pending events. The default
        // is fail-closed Ã¢â‚¬â€ any save with pending events will throw.
        services.TryAddScopedDomainEventDispatcher();

        // Tenant table registry is the single source of truth for RLS coverage.
        // We register a concrete instance (not the type) so additions applied
        // during startup via RegisterTenantTable land on the same registry the
        // provisioning service eventually resolves.
        var registry = new TenantTableRegistry();
        services.AddSingleton<ITenantTableRegistry>(registry);
        services.AddSingleton<RlsScriptGenerator>();

        // Single TimeProvider instance is fine Ã¢â‚¬â€ TimeProvider.System is thread-safe.
        // Tests can swap in a FakeTimeProvider before calling AddKaramchariCore.
        services.TryAddSingletonTimeProvider();

        return services;
    }

    /// <summary>
    /// Registers a tenant-owned table so RLS policies cover it. Call once per
    /// table from each bounded context's <c>AddKaramchari{Context}</c> extension.
    /// Forgetting to call this is a security regression Ã¢â‚¬â€ RLS won't apply to
    /// the missing table.
    /// </summary>
    public static IServiceCollection RegisterTenantTable(this IServiceCollection services, string tableName)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Build the descriptor eagerly so name validation fires at startup, not at first save.
        var table = new TenantTable(tableName);

        // Resolve the singleton registry instance and add to it. AddKaramchariCore
        // must have run first; otherwise the registry doesn't exist yet.
        var registryDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITenantTableRegistry))
            ?? throw new InvalidOperationException(
                "Call AddKaramchariCore before RegisterTenantTable so the registry exists.");

        if (registryDescriptor.ImplementationInstance is not ITenantTableRegistry instance)
        {
            throw new InvalidOperationException(
                "ITenantTableRegistry must be registered as a concrete singleton instance Ã¢â‚¬â€ see AddKaramchariCore.");
        }

        instance.Register(table);
        return services;
    }

    /// <summary>
    /// Attaches the Karamchari interceptors to a <see cref="DbContextOptionsBuilder"/>.
    /// Bounded-context startup calls this from inside its <c>AddDbContext</c> lambda:
    ///
    /// <code>
    /// services.AddDbContext&lt;HRDbContext&gt;((sp, opts) =>
    /// {
    ///     opts.UseSqlServer(connectionString);
    ///     opts.AddKaramchariInterceptors(sp);
    /// });
    /// </code>
    /// </summary>
    public static DbContextOptionsBuilder AddKaramchariInterceptors(
        this DbContextOptionsBuilder builder,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        // Order matters for SaveChangesInterceptors: stamp/validate TenantId
        // before domain events are dispatched, so any cross-tenant write attempt
        // fails *before* its events would be published to the bus.
        builder.AddInterceptors(
            serviceProvider.GetRequiredService<RlsSessionContextInterceptor>(),
            serviceProvider.GetRequiredService<TenantStampingInterceptor>(),
            serviceProvider.GetRequiredService<DomainEventDispatchInterceptor>());

        return builder;
    }

    /// <summary>
    /// Registers the outbox relay BackgroundService and its dependencies.
    /// Call this ONLY in non-development environments where Azure Service Bus is
    /// configured. In development, <c>UseBusOutbox()</c> + in-memory transport
    /// delivers messages immediately without a relay.
    /// </summary>
    public static IServiceCollection AddOutboxRelay(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<OutboxRelayOptions>()
            .Bind(configuration.GetSection(OutboxRelayOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // DbContext for outbox infra tables only; no tenant interceptors.
        services.AddDbContext<OutboxRelayDbContext>((sp, opts) =>
            opts.UseSqlServer(configuration.GetConnectionString("KaramchariDb")));

        // Type registry is built once at construction from loaded assemblies.
        // Must be registered AFTER the DI container has loaded all bounded-context assemblies
        // (i.e., call AddOutboxRelay after AddKaramchariHR, AddKaramchariPayroll, etc.).
        services.AddSingleton<OutboxMessageTypeRegistry>();
        services.AddSingleton<OutboxRelayCircuitBreaker>();
        services.AddSingleton<OutboxRelayMetrics>();
        services.AddHostedService<OutboxRelayService>();

        return services;
    }

    /// <summary>
    /// Adds the tenant observability middleware to the request pipeline.
    /// Should be called after UseAuthentication and UseAuthorization.
    /// </summary>
    public static IApplicationBuilder UseKaramchariTenantObservability(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TenantObservabilityMiddleware>();
    }

    private static IServiceCollection TryAddSingletonTimeProvider(this IServiceCollection services)
    {
        if (!services.Any(d => d.ServiceType == typeof(TimeProvider)))
        {
            services.AddSingleton(TimeProvider.System);
        }

        return services;
    }

    private static IServiceCollection TryAddScopedDomainEventDispatcher(this IServiceCollection services)
    {
        if (!services.Any(d => d.ServiceType == typeof(IDomainEventDispatcher)))
        {
            services.AddScoped<IDomainEventDispatcher, NullDomainEventDispatcher>();
        }

        return services;
    }
}
