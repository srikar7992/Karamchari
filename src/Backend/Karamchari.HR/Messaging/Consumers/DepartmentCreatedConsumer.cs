using Karamchari.HR.Domain.Departments.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Karamchari.HR.Messaging.Consumers;

public sealed partial class DepartmentCreatedConsumer : IConsumer<DepartmentCreated>
{
    private readonly ILogger<DepartmentCreatedConsumer> _logger;

    public DepartmentCreatedConsumer(ILogger<DepartmentCreatedConsumer> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task Consume(ConsumeContext<DepartmentCreated> context)
    {
        var message = context.Message;

        LogDepartmentCreatedConsumed(
            _logger,
            message.EventId,
            message.DepartmentId,
            message.Name,
            message.OccurredOnUtc);

        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Consumed DepartmentCreated event {EventId} for DepartmentId={DepartmentId}, Name={Name}, OccurredOnUtc={OccurredOnUtc}.")]
    private static partial void LogDepartmentCreatedConsumed(
        ILogger logger,
        Guid eventId,
        Guid departmentId,
        string name,
        DateTimeOffset occurredOnUtc);
}
