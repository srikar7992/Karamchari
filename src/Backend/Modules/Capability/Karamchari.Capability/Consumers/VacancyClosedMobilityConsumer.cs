// -----------------------------------------------------------------------
// <copyright file="VacancyClosedMobilityConsumer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading.Tasks;
using Karamchari.Capability.Projections;
using Karamchari.Core.Contracts.IntegrationEvents;
using MassTransit;

namespace Karamchari.Capability.Consumers;

/// <summary>
/// Deletes <see cref="Domain.Mobility.InternalMobilityProjection"/> rows when a vacancy closes.
/// </summary>
public sealed class VacancyClosedMobilityConsumer(InternalMobilityProjectionHandler handler)
    : IConsumer<VacancyClosedIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<VacancyClosedIntegrationEvent> context)
    {
        await handler.HandleVacancyClosedAsync(context.Message, context.CancellationToken);
    }
}
