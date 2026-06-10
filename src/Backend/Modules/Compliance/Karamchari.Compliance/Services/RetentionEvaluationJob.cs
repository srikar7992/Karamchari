// -----------------------------------------------------------------------
// <copyright file="RetentionEvaluationJob.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Compliance.Domain.Retention;
using Karamchari.Compliance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Karamchari.Compliance.Services;

public sealed class RetentionEvaluationJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RetentionEvaluationJob> _logger;
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24);

    public RetentionEvaluationJob(IServiceScopeFactory scopeFactory, ILogger<RetentionEvaluationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunEvaluationAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Retention evaluation job failed");
            }

            await Task.Delay(RunInterval, stoppingToken);
        }
    }

    private async Task RunEvaluationAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var executionService = scope.ServiceProvider.GetRequiredService<RetentionExecutionService>();
        var db = scope.ServiceProvider.GetRequiredService<ComplianceDbContext>();

        var policies = await db.RetentionPolicies
            .Where(p => p.Enabled)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;

        foreach (var policy in policies)
        {
            _logger.LogDebug(
                "Retention policy {RecordType}: archive={ArchiveDays}d, delete={DeleteDays}d, anonymize={AnonymizeDays}d",
                policy.RecordType, policy.ArchiveAfterDays, policy.DeleteAfterDays, policy.AnonymizeAfterDays);
        }

        var snapshotService = scope.ServiceProvider.GetRequiredService<ComplianceSnapshotService>();
        var tenants = policies.Select(p => p.TenantId).Distinct().ToList();
        foreach (var tenantId in tenants)
        {
            await executionService.ExecutePendingActionsAsync(tenantId, ct);
            await snapshotService.RefreshCurrentMonthAsync(tenantId, ct);
        }

        _logger.LogInformation("Retention evaluation complete: {Count} policies, {Tenants} tenants",
            policies.Count, policies.Select(p => p.TenantId).Distinct().Count());
    }
}
