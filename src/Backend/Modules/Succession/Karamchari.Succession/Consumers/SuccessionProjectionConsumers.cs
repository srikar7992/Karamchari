// -----------------------------------------------------------------------
// <copyright file="SuccessionProjectionConsumers.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading.Tasks;
using Karamchari.Core.Messaging;
using Karamchari.Core.Projections;
using Karamchari.HR.Domain.Organization.Events;
using Karamchari.Succession.Domain.Events;
using MassTransit;

namespace Karamchari.Succession.Consumers;

public sealed class PositionCreatedConsumer(IProjectionHandler<PositionCreated> handler)
    : IConsumer<EnterpriseEventEnvelope<PositionCreated>>
{
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<PositionCreated>> context)
    {
        await handler.HandleAsync(context.Message.Payload, context.CancellationToken);
    }
}

public sealed class PositionAssignmentCreatedConsumer(IProjectionHandler<PositionAssignmentCreated> handler)
    : IConsumer<EnterpriseEventEnvelope<PositionAssignmentCreated>>
{
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<PositionAssignmentCreated>> context)
    {
        await handler.HandleAsync(context.Message.Payload, context.CancellationToken);
    }
}

public sealed class PositionAssignmentEndedConsumer(IProjectionHandler<PositionAssignmentEnded> handler)
    : IConsumer<EnterpriseEventEnvelope<PositionAssignmentEnded>>
{
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<PositionAssignmentEnded>> context)
    {
        await handler.HandleAsync(context.Message.Payload, context.CancellationToken);
    }
}

public sealed class SuccessorAddedConsumer(IProjectionHandler<SuccessorAddedEvent> handler)
    : IConsumer<EnterpriseEventEnvelope<SuccessorAddedEvent>>
{
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<SuccessorAddedEvent>> context)
    {
        await handler.HandleAsync(context.Message.Payload, context.CancellationToken);
    }
}

public sealed class SuccessorPromotedConsumer(IProjectionHandler<SuccessorPromotedEvent> handler)
    : IConsumer<EnterpriseEventEnvelope<SuccessorPromotedEvent>>
{
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<SuccessorPromotedEvent>> context)
    {
        await handler.HandleAsync(context.Message.Payload, context.CancellationToken);
    }
}
