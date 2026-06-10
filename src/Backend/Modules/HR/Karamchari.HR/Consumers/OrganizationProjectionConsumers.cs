// -----------------------------------------------------------------------
// <copyright file="OrganizationProjectionConsumers.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading.Tasks;
using Karamchari.Core.Messaging;
using Karamchari.Core.Projections;
using Karamchari.HR.Domain.Organization.Events;
using MassTransit;

namespace Karamchari.HR.Consumers;

public sealed class OrganizationUnitCreatedConsumer(IProjectionHandler<OrganizationUnitCreated> handler)
    : IConsumer<EnterpriseEventEnvelope<OrganizationUnitCreated>>
{
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<OrganizationUnitCreated>> context)
    {
        await handler.HandleAsync(context.Message.Payload, context.CancellationToken);
    }
}

public sealed class OrganizationUnitDeactivatedConsumer(IProjectionHandler<OrganizationUnitDeactivated> handler)
    : IConsumer<EnterpriseEventEnvelope<OrganizationUnitDeactivated>>
{
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<OrganizationUnitDeactivated>> context)
    {
        await handler.HandleAsync(context.Message.Payload, context.CancellationToken);
    }
}

public sealed class PositionCreatedConsumer(IProjectionHandler<PositionCreated> handler)
    : IConsumer<EnterpriseEventEnvelope<PositionCreated>>
{
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<PositionCreated>> context)
    {
        await handler.HandleAsync(context.Message.Payload, context.CancellationToken);
    }
}

public sealed class PositionStatusChangedConsumer(IProjectionHandler<PositionStatusChanged> handler)
    : IConsumer<EnterpriseEventEnvelope<PositionStatusChanged>>
{
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<PositionStatusChanged>> context)
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

public sealed class PositionVacancyOpenedConsumer(IProjectionHandler<PositionVacancyOpened> handler)
    : IConsumer<EnterpriseEventEnvelope<PositionVacancyOpened>>
{
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<PositionVacancyOpened>> context)
    {
        await handler.HandleAsync(context.Message.Payload, context.CancellationToken);
    }
}

public sealed class PositionVacancyClosedConsumer(IProjectionHandler<PositionVacancyClosed> handler)
    : IConsumer<EnterpriseEventEnvelope<PositionVacancyClosed>>
{
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<PositionVacancyClosed>> context)
    {
        await handler.HandleAsync(context.Message.Payload, context.CancellationToken);
    }
}

public sealed class ReportingRelationshipCreatedConsumer(IProjectionHandler<ReportingRelationshipCreated> handler)
    : IConsumer<EnterpriseEventEnvelope<ReportingRelationshipCreated>>
{
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<ReportingRelationshipCreated>> context)
    {
        await handler.HandleAsync(context.Message.Payload, context.CancellationToken);
    }
}

public sealed class ReportingRelationshipEndedConsumer(IProjectionHandler<ReportingRelationshipEnded> handler)
    : IConsumer<EnterpriseEventEnvelope<ReportingRelationshipEnded>>
{
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<ReportingRelationshipEnded>> context)
    {
        await handler.HandleAsync(context.Message.Payload, context.CancellationToken);
    }
}
