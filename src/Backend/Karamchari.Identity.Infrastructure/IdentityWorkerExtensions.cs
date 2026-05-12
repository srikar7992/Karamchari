using Karamchari.Identity.Infrastructure.BackgroundServices;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Identity.Infrastructure;

/// <summary>
/// Provides extension methods for registering Identity-related background workers.
/// </summary>
public static class IdentityWorkerExtensions
{
    /// <summary>
    /// Adds background workers for token cleanup and revocation to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddKaramchariIdentityWorkers(this IServiceCollection services)
    {
        services.AddHostedService<RefreshTokenCleanupWorker>();
        services.AddHostedService<RevokedTokenCleanupWorker>();
        return services;
    }
}
