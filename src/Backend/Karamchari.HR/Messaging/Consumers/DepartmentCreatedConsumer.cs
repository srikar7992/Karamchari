using Karamchari.HR.Domain.Departments.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Karamchari.HR.Messaging.Consumers;

/// <summary>
/// Consumer for the <see cref="DepartmentCreated"/> domain event.
/// Currently just logs the event for auditing purposes.
/// </summary>
public sealed partial class DepartmentCreatedConsumer : IConsumer<DepartmentCreated>
{
    private readonly ILogger<DepartmentCreatedConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DepartmentCreatedConsumer"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public DepartmentCreatedConsumer(ILogger<DepartmentCreatedConsumer> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Consumes the <see cref="DepartmentCreated"/> event.
    /// </summary>
    /// <param name="context">The consume context.</param>
    public Task Consume(ConsumeContext<DepartmentCreated> context)
    {
        ArgumentNullException.ThrowIfNull(context);
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
