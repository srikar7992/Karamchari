using Karamchari.Core.Contracts.IntegrationEvents.V1;
using Karamchari.Performance.Domain.Skills;
using Karamchari.Performance.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Performance.Consumers;

/// <summary>
/// Seeds an EmployeeSkillProfile when a new employee is onboarded.
/// Idempotent: if a profile already exists for (TenantId, EmployeeId), the message is acknowledged and skipped.
/// </summary>
public sealed partial class EmployeeOnboardedPerformanceConsumer : IConsumer<EmployeeOnboardedIntegrationEvent>
{
    private readonly PerformanceDbContext _dbContext;
    private readonly ILogger<EmployeeOnboardedPerformanceConsumer> _logger;

    public EmployeeOnboardedPerformanceConsumer(
        PerformanceDbContext dbContext,
        ILogger<EmployeeOnboardedPerformanceConsumer> logger)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(logger);
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EmployeeOnboardedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var message = context.Message;

        LogConsuming(_logger, message.EmployeeId, message.TenantId);

        var alreadyExists = await _dbContext.EmployeeSkillProfiles
            .AsNoTracking()
            .AnyAsync(p => p.TenantId == message.TenantId && p.EmployeeId == message.EmployeeId,
                context.CancellationToken);

        if (alreadyExists)
        {
            LogSkippedDuplicate(_logger, message.EmployeeId, message.TenantId);
            return;
        }

        var profile = EmployeeSkillProfile.Seed(message.TenantId, message.EmployeeId);
        _dbContext.EmployeeSkillProfiles.Add(profile);
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        LogSeeded(_logger, message.EmployeeId, message.TenantId, profile.Id);
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information,
        Message = "Consuming EmployeeOnboarded for EmployeeId={EmployeeId} TenantId={TenantId}")]
    private static partial void LogConsuming(ILogger logger, Guid employeeId, string tenantId);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Warning,
        Message = "SkillProfile already exists for EmployeeId={EmployeeId} TenantId={TenantId}. Skipping.")]
    private static partial void LogSkippedDuplicate(ILogger logger, Guid employeeId, string tenantId);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information,
        Message = "Seeded SkillProfile {ProfileId} for EmployeeId={EmployeeId} TenantId={TenantId}")]
    private static partial void LogSeeded(ILogger logger, Guid employeeId, string tenantId, Guid profileId);
}
