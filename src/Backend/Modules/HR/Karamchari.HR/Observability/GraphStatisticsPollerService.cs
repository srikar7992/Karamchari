// -----------------------------------------------------------------------
// <copyright file="GraphStatisticsPollerService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Karamchari.HR.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Karamchari.HR.Observability;

/// <summary>
/// Background service that polls workforce graph node and edge counts every 60 seconds
/// and updates cardinality gauges in <see cref="KaramchariGraphMetrics"/>.
/// Uses GROUP BY COUNT(*) queries — acceptable at current scale. Migrate to
/// <c>GraphStatisticsProjection</c> when edge count exceeds 1M.
/// </summary>
public sealed class GraphStatisticsPollerService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KaramchariGraphMetrics _metrics;
    private readonly ILogger<GraphStatisticsPollerService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphStatisticsPollerService"/> class.
    /// </summary>
    public GraphStatisticsPollerService(
        IServiceScopeFactory scopeFactory,
        KaramchariGraphMetrics metrics,
        ILogger<GraphStatisticsPollerService> logger)
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
                _logger.LogWarning(ex, "GraphStatisticsPollerService: poll cycle failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HRDbContext>();

        var nodeCountRows = await db.GraphNodes
            .AsNoTracking()
            .GroupBy(n => n.Type)
            .Select(g => new { Type = g.Key.ToString(), Count = (long)g.Count() })
            .ToListAsync(cancellationToken);

        var edgeCountRows = await db.GraphEdges
            .AsNoTracking()
            .GroupBy(e => e.Type)
            .Select(g => new { Type = g.Key.ToString(), Count = (long)g.Count() })
            .ToListAsync(cancellationToken);

        var activeEdgeCountRows = await db.GraphEdges
            .AsNoTracking()
            .Where(e => e.IsActive)
            .GroupBy(e => e.Type)
            .Select(g => new { Type = g.Key.ToString(), Count = (long)g.Count() })
            .ToListAsync(cancellationToken);

        _metrics.UpdateNodeCounts(
            (IReadOnlyDictionary<string, long>)nodeCountRows.ToDictionary(x => x.Type, x => x.Count));

        _metrics.UpdateEdgeCounts(
            (IReadOnlyDictionary<string, long>)edgeCountRows.ToDictionary(x => x.Type, x => x.Count),
            (IReadOnlyDictionary<string, long>)activeEdgeCountRows.ToDictionary(x => x.Type, x => x.Count));
    }
}
