using System.Net;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Api.DependencyInjection;

/// <summary>
/// Provides centralized resilience configuration using modern .NET standard resilience handlers.
/// </summary>
public static class ResilienceExtensions
{
    /// <summary>
    /// Configures global HTTP client resilience.
    /// Applies to all HttpClient instances created via IHttpClientFactory unless overridden.
    /// </summary>
    public static IServiceCollection AddKaramchariResilience(this IServiceCollection services)
    {
        // Add standardized resilience to all HttpClients globally
        services.ConfigureHttpClientDefaults(builder =>
        {
            builder.AddStandardResilienceHandler(options =>
            {
                // Customize standard resilience defaults
                // 1. Retry Strategy (Exponential backoff + Jitter)
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                options.Retry.UseJitter = true;

                // 2. Circuit Breaker
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.MinimumThroughput = 5;

                // 3. Timeout
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
            });
        });

        return services;
    }
}
