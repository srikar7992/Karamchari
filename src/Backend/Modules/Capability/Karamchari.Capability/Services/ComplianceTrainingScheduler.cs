using Karamchari.Capability.Domain.Compliance;
using Karamchari.Capability.Domain.Events;
using Karamchari.Capability.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Karamchari.Capability.Services;

public sealed class ComplianceTrainingScheduler(
    IServiceScopeFactory scopeFactory,
    ILogger<ComplianceTrainingScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunScanAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Compliance training scheduler failed");
            }

            var nextRun = DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow;
            await Task.Delay(nextRun, stoppingToken);
        }
    }

    private async Task RunScanAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CapabilityDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var overdueKey = today.ToString("yyyy-MM-dd");

        var overdueAssignments = await db.ComplianceAssignments
            .AsNoTracking()
            .Where(a => a.Status == ComplianceAssignmentStatus.Active && a.DueDate < today)
            .ToListAsync(ct);

        if (overdueAssignments.Count == 0) return;

        var assignmentIds = overdueAssignments.Select(a => a.Id).ToList();
        var completedPairs = await db.ComplianceCompletions
            .AsNoTracking()
            .Where(c => assignmentIds.Contains(c.AssignmentId))
            .Select(c => new { c.AssignmentId, c.EmployeeId })
            .ToListAsync(ct);

        var completedSet = completedPairs
            .Select(p => (p.AssignmentId, p.EmployeeId))
            .ToHashSet();

        // TargetGroup="All" → get all active employeeIds from compliance completions + active assignments
        // For now: emit for each assignment with TargetGroup="All" using a placeholder employee scan.
        // Full HR fanout wired when IHRReadService is available cross-module.
        int published = 0;
        foreach (var assignment in overdueAssignments)
        {
            logger.LogWarning(
                "ComplianceAssignment {AssignmentId} is overdue (DueDate={DueDate}, TargetGroup={TargetGroup}). " +
                "Fanout to employees requires HR cross-module wiring.",
                assignment.Id, assignment.DueDate, assignment.TargetGroup);
            published++;
        }

        logger.LogInformation("Compliance scan {Date}: {Count} overdue assignments flagged", overdueKey, published);
    }
}
