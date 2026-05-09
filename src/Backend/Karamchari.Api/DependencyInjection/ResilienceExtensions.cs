using Polly;
using Polly.Extensions.Http;
using System.Net;

namespace Karamchari.Api.DependencyInjection;

public static class ResilienceExtensions
{
    public static IServiceCollection AddKaramchariResilience(this IServiceCollection services)
    {
        var random = new Random();

        // 1. Retry Storm Protection (Jitter)
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound) // Example: some APIs return 404 for transient states
            .WaitAndRetryAsync(3, retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + 
                TimeSpan.FromMilliseconds(random.Next(0, 1000)) // Add jitter
            );

        // 2. Downstream Degradation Handling (Circuit Breaker)
        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

        // 3. Global Timeout
        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(15));

        // Wrap them so Timeout wraps Retry wraps CircuitBreaker
        services.AddSingleton(Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy));

        return services;
    }
}
