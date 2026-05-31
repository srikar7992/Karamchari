using Karamchari.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Karamchari.Identity.Infrastructure.BackgroundServices;

/// <summary>
/// Background worker to clean up expired revoked token entries.
/// </summary>
internal sealed class RevokedTokenCleanupWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RevokedTokenCleanupWorker> _logger;
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(12);

    public RevokedTokenCleanupWorker(IServiceProvider serviceProvider, ILogger<RevokedTokenCleanupWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupTokensAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up revoked tokens.");
            }

            await Task.Delay(CleanupInterval, stoppingToken);
        }
    }

    private async Task CleanupTokensAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var now = DateTimeOffset.UtcNow;
        var toDelete = await db.RevokedTokens
            .Where(t => t.ExpiresAtUtc < now)
            .ToListAsync(ct);

        if (toDelete.Count > 0)
        {
            _logger.LogInformation("Cleaning up {Count} expired revoked tokens.", toDelete.Count);
            db.RevokedTokens.RemoveRange(toDelete);
            await db.SaveChangesAsync(ct);
        }
    }
}
