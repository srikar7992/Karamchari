using Polly;
using Polly.Extensions.Http;
using System.Net;

namespace Karamchari.Api.DependencyInjection;

public static class ResilienceExtensions
{
    public static IServiceCollection AddKaramchariResilience(this IServiceCollection services)
    {
        // Standard HTTP Retry Policy
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound) // Example: some APIs return 404 for transient states
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        // Circuit Breaker Policy
        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

        // Timeout Policy
        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));

        services.AddSingleton(Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy));

        return services;
    }
}
