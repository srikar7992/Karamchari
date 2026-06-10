// -----------------------------------------------------------------------
// <copyright file="IdempotencyCleanupWorker.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Middleware;

/// <summary>
/// Background worker to clean up expired idempotent requests.
/// Prevents table bloat and performance degradation.
/// </summary>
public sealed class IdempotencyCleanupWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IdempotencyCleanupWorker> _logger;
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(12);

    public IdempotencyCleanupWorker(IServiceProvider serviceProvider, ILogger<IdempotencyCleanupWorker> logger)
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
                await CleanupRequestsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up idempotent requests.");
            }

            await Task.Delay(CleanupInterval, stoppingToken);
        }
    }

    private async Task CleanupRequestsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var now = DateTimeOffset.UtcNow;

        // Use ExecuteDeleteAsync for efficiency if using EF Core 7+
        // Fallback to traditional remove range for compatibility
        var toDelete = await db.IdempotentRequests
            .Where(r => r.ExpiresAtUtc < now)
            .Take(1000) // Batching to avoid long locks
            .ToListAsync(ct);

        if (toDelete.Count > 0)
        {
            _logger.LogInformation("Cleaning up {Count} expired idempotent requests.", toDelete.Count);
            db.IdempotentRequests.RemoveRange(toDelete);
            await db.SaveChangesAsync(ct);
        }
    }
}
