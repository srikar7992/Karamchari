// -----------------------------------------------------------------------
// <copyright file="SkillValidatedReadinessConsumer.cs" company="Karamchari">
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
/// MassTransit consumer that recomputes <see cref="Domain.Skills.CareerReadinessProjection"/>
/// rows whenever a skill is validated for an employee.
/// </summary>
public sealed class SkillValidatedReadinessConsumer(CareerReadinessProjectionHandler handler)
    : IConsumer<SkillValidatedIntegrationEventV1>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<SkillValidatedIntegrationEventV1> context)
    {
        await handler.HandleAsync(context.Message, context.CancellationToken);
    }
}
