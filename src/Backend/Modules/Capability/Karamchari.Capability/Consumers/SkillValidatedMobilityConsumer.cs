using System.Threading.Tasks;
using Karamchari.Capability.Projections;
using Karamchari.Core.Contracts.IntegrationEvents;
using MassTransit;

namespace Karamchari.Capability.Consumers;

/// <summary>
/// Recomputes <see cref="Domain.Mobility.InternalMobilityProjection"/> rows for all open vacancies
/// affected by a newly validated skill. Computes from source data to avoid ordering races with
/// <see cref="SkillValidatedReadinessConsumer"/>.
/// </summary>
public sealed class SkillValidatedMobilityConsumer(InternalMobilityProjectionHandler handler)
    : IConsumer<SkillValidatedIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<SkillValidatedIntegrationEvent> context)
    {
        await handler.HandleSkillValidatedAsync(context.Message, context.CancellationToken);
    }
}
