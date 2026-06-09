using System.Threading.Tasks;
using Karamchari.Capability.Projections;
using Karamchari.Core.Contracts.IntegrationEvents;
using MassTransit;

namespace Karamchari.Capability.Consumers;

/// <summary>
/// Seeds <see cref="Domain.Mobility.InternalMobilityProjection"/> rows when a vacancy opens.
/// </summary>
public sealed class VacancyOpenedMobilityConsumer(InternalMobilityProjectionHandler handler)
    : IConsumer<VacancyOpenedIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<VacancyOpenedIntegrationEvent> context)
    {
        await handler.HandleVacancyOpenedAsync(context.Message, context.CancellationToken);
    }
}
