// -----------------------------------------------------------------------
// <copyright file="ProjectionLagPollerService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Karamchari.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Observability;

/// <summary>
/// Background service that polls projection checkpoints every 30 seconds and updates
/// lag gauges in <see cref="KaramchariProjectionMetrics"/>.
/// Uses max checkpoint position across all projections as a proxy for event store head.
/// </summary>
public sealed class ProjectionLagPollerService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KaramchariProjectionMetrics _metrics;
    private readonly ILogger<ProjectionLagPollerService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionLagPollerService"/> class.
    /// </summary>
    public ProjectionLagPollerService(
        IServiceScopeFactory scopeFactory,
        KaramchariProjectionMetrics metrics,
        ILogger<ProjectionLagPollerService> logger)
    {
        _scopeFactory = scopeFactory;
        _metrics = metrics;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "ProjectionLagPollerService: poll cycle failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<KaramchariDbContext>();

        var checkpoints = await db.ProjectionCheckpoints
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (checkpoints.Count == 0)
            return;

        _metrics.UpdateCheckpointCount(checkpoints.Count);

        // Use max position across all projections as a conservative proxy for event store head.
        // Each projection's lag is how far behind the most-advanced projection it is.
        var headPosition = checkpoints.Max(c => c.LastProcessedPosition);

        foreach (var checkpoint in checkpoints)
        {
            var lagEvents = Math.Max(0L, headPosition - checkpoint.LastProcessedPosition);
            var lagMs = Math.Max(0.0, (DateTimeOffset.UtcNow - checkpoint.ProcessedUtc).TotalMilliseconds);
            _metrics.UpdateLag(checkpoint.ProjectionName, lagEvents, lagMs);
        }
    }
}
