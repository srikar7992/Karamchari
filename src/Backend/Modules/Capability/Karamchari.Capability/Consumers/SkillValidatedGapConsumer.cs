// -----------------------------------------------------------------------
// <copyright file="SkillValidatedGapConsumer.cs" company="Karamchari">
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
/// MassTransit consumer that recomputes <see cref="Domain.Skills.EmployeeSkillGapProjection"/>
/// rows whenever a skill is validated for an employee.
/// </summary>
public sealed class SkillValidatedGapConsumer(EmployeeSkillGapProjectionHandler handler)
    : IConsumer<SkillValidatedIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<SkillValidatedIntegrationEvent> context)
    {
        await handler.HandleAsync(context.Message, context.CancellationToken);
    }
}
