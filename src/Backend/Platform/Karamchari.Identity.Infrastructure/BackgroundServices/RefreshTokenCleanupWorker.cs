using Karamchari.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Karamchari.Identity.Infrastructure.BackgroundServices;

/// <summary>
/// Background worker to clean up expired and revoked refresh tokens.
/// Prevent table bloat.
/// </summary>
internal sealed class RefreshTokenCleanupWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RefreshTokenCleanupWorker> _logger;
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(6);

    public RefreshTokenCleanupWorker(IServiceProvider serviceProvider, ILogger<RefreshTokenCleanupWorker> logger)
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
                _logger.LogError(ex, "Error occurred while cleaning up refresh tokens.");
            }

            await Task.Delay(CleanupInterval, stoppingToken);
        }
    }

    private async Task CleanupTokensAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        // Retention policy: Keep revoked/expired tokens for 7 days for audit/replay detection, then delete.
        var cutoff = DateTimeOffset.UtcNow.AddDays(-7);

        var toDelete = await db.RefreshTokens
            .Where(t => (t.RevokedAtUtc != null && t.RevokedAtUtc < cutoff) ||
                        (t.ExpiresAtUtc < cutoff))
            .ToListAsync(ct);

        if (toDelete.Count > 0)
        {
            _logger.LogInformation("Cleaning up {Count} old refresh tokens.", toDelete.Count);
            db.RefreshTokens.RemoveRange(toDelete);
            await db.SaveChangesAsync(ct);
        }
    }
}
