using Karamchari.Core.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Karamchari.Api.DependencyInjection;

/// <summary>
/// Centralized health check registration for the platform.
/// </summary>
public static class HealthCheckExtensions
{
    private static readonly string[] DbTags = new[] { "ready", "db" };
    private static readonly string[] MessagingTags = new[] { "ready", "messaging" };
    private static readonly string[] CacheTags = new[] { "ready", "cache" };

    /// <summary>
    /// Registers all platform health checks including databases, messaging, and caching.
    /// </summary>
    public static IServiceCollection AddKaramchariHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var healthBuilder = services.AddHealthChecks();

        // 1. Database connectivity.
        // Every bounded-context DbContext targets the SAME physical database (the single
        // "KaramchariDb" connection). Probing all 16 separately means 16x redundant round
        // trips per health request and, under concurrent probes, SQL connection-pool
        // exhaustion. A single connectivity check is sufficient to answer "is the database
        // reachable"; per-context schema correctness is covered by provisioning + tests.
        healthBuilder.AddDbContextCheck<CoreDbContext>("Database", tags: DbTags);

        // 2. Messaging Checks
        var rabbitMqConnection = configuration.GetConnectionString("RabbitMQ");
        if (!string.IsNullOrEmpty(rabbitMqConnection))
        {
            healthBuilder.AddRabbitMQ(
                rabbitMqConnection,
                name: "Messaging:RabbitMQ",
                tags: MessagingTags);
        }

        var sbConnection = configuration.GetConnectionString("AzureServiceBus");
        if (!string.IsNullOrEmpty(sbConnection))
        {
            healthBuilder.AddAzureServiceBusTopic(sbConnection, topicName: "integration-events", name: "Messaging:ServiceBus", tags: MessagingTags);
        }

        // 3. Caching Checks
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            healthBuilder.AddRedis(redisConnectionString: redisConnection, name: "Caching:Redis", tags: CacheTags);
        }

        // Single-flight, short-TTL cache so frequent/concurrent probes (k8s, load balancers,
        // monitoring) do not each re-open live dependency connections. Prevents the readiness
        // endpoint from becoming a latency hotspot and an unauthenticated amplification vector.
        services.AddSingleton<CachedHealthReportProvider>();

        return services;
    }

    /// <summary>
    /// Maps health check endpoints to the request pipeline.
    /// </summary>
    public static WebApplication MapKaramchariHealthChecks(this WebApplication app)
    {
        // Readiness ("ready"-tagged: DB, messaging, cache) — served from a 5s single-flight
        // cache so a burst of probes triggers at most one real dependency evaluation.
        static bool ReadyPredicate(HealthCheckRegistration r) => r.Tags.Contains("ready");

        app.MapGet("/health", (CachedHealthReportProvider cache, CancellationToken ct) =>
            cache.WriteAsync(ReadyPredicate, degradedIsHealthy: true, ct));

        app.MapGet("/health/ready", (CachedHealthReportProvider cache, CancellationToken ct) =>
            cache.WriteAsync(ReadyPredicate, degradedIsHealthy: false, ct));

        app.MapGet("/health/startup", (CachedHealthReportProvider cache, CancellationToken ct) =>
            cache.WriteAsync(ReadyPredicate, degradedIsHealthy: false, ct));

        // Liveness — never touches dependencies; 200 as long as the process serves requests.
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteResponse
        });

        return app;
    }

    internal static Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration,
            results = report.Entries.Select(e => new
            {
                key = e.Key,
                value = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration,
                data = e.Value.Data
            })
        });
        return context.Response.WriteAsync(json);
    }
}

/// <summary>
/// Caches the readiness <see cref="HealthReport"/> for a short TTL and coalesces concurrent
/// evaluations behind a single in-flight check (single-flight), so a burst of probes does not
/// each re-open SQL/Redis/RabbitMQ connections.
/// </summary>
internal sealed class CachedHealthReportProvider : IDisposable
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(5);
    private readonly HealthCheckService _service;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTimeOffset _stampUtc = DateTimeOffset.MinValue;
    private HealthReport? _cached;

    public CachedHealthReportProvider(HealthCheckService service) => _service = service;

    public void Dispose() => _gate.Dispose();

    public async Task<HealthReport> GetAsync(Func<HealthCheckRegistration, bool> predicate, CancellationToken ct)
    {
        if (_cached is { } fresh && DateTimeOffset.UtcNow - _stampUtc < Ttl)
        {
            return fresh;
        }

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_cached is { } stillFresh && DateTimeOffset.UtcNow - _stampUtc < Ttl)
            {
                return stillFresh;
            }

            var report = await _service.CheckHealthAsync(predicate, ct).ConfigureAwait(false);
            _cached = report;
            _stampUtc = DateTimeOffset.UtcNow;
            return report;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task WriteAsync(Func<HealthCheckRegistration, bool> predicate, bool degradedIsHealthy, HttpContext context, CancellationToken ct)
    {
        var report = await GetAsync(predicate, ct).ConfigureAwait(false);
        context.Response.StatusCode = report.Status switch
        {
            HealthStatus.Healthy => StatusCodes.Status200OK,
            HealthStatus.Degraded => degradedIsHealthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status503ServiceUnavailable
        };
        await HealthCheckExtensions.WriteResponse(context, report).ConfigureAwait(false);
    }

    // Minimal-API friendly overload: resolves HttpContext via IResult.
    public IResult WriteAsync(Func<HealthCheckRegistration, bool> predicate, bool degradedIsHealthy, CancellationToken ct)
        => new HealthReportResult(this, predicate, degradedIsHealthy, ct);

    private sealed class HealthReportResult(
        CachedHealthReportProvider provider,
        Func<HealthCheckRegistration, bool> predicate,
        bool degradedIsHealthy,
        CancellationToken ct) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
            => provider.WriteAsync(predicate, degradedIsHealthy, httpContext, ct);
    }
}
