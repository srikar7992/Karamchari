// -----------------------------------------------------------------------
// <copyright file="IntelligenceProjectionConsumers.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading.Tasks;
using Karamchari.Compensation.Contracts.IntegrationEvents;
using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Core.Messaging;
using Karamchari.Core.Projections;
using Karamchari.HR.Domain.Organization.Events;
using Karamchari.HR.Projections;
using Karamchari.Performance.Contracts.IntegrationEvents;
using MassTransit;

namespace Karamchari.HR.Consumers;

public sealed class IntelligencePositionAssignmentCreatedConsumer(EmployeeIntelligenceProjectionHandler handler)
    : IConsumer<EnterpriseEventEnvelope<PositionAssignmentCreated>>
{
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<PositionAssignmentCreated>> context)
    {
        await handler.HandleAsync(context.Message.Payload, context.CancellationToken);
    }
}

public sealed class IntelligencePositionAssignmentEndedConsumer(EmployeeIntelligenceProjectionHandler handler)
    : IConsumer<EnterpriseEventEnvelope<PositionAssignmentEnded>>
{
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<PositionAssignmentEnded>> context)
    {
        await handler.HandleAsync(context.Message.Payload, context.CancellationToken);
    }
}

public sealed class IntelligenceReportingRelationshipCreatedConsumer(EmployeeIntelligenceProjectionHandler handler)
    : IConsumer<EnterpriseEventEnvelope<ReportingRelationshipCreated>>
{
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<ReportingRelationshipCreated>> context)
    {
        await handler.HandleAsync(context.Message.Payload, context.CancellationToken);
    }
}

public sealed class IntelligenceReportingRelationshipEndedConsumer(EmployeeIntelligenceProjectionHandler handler)
    : IConsumer<EnterpriseEventEnvelope<ReportingRelationshipEnded>>
{
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<ReportingRelationshipEnded>> context)
    {
        await handler.HandleAsync(context.Message.Payload, context.CancellationToken);
    }
}

public sealed class IntelligenceEmployeeCompensationRevisedConsumer(EmployeeIntelligenceProjectionHandler handler)
    : IConsumer<EmployeeCompensationRevisedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<EmployeeCompensationRevisedIntegrationEvent> context)
    {
        await handler.HandleAsync(context.Message, context.CancellationToken);
    }
}

public sealed class IntelligenceEmployeePerformanceSnapshotConsumer(EmployeeIntelligenceProjectionHandler handler)
    : IConsumer<EmployeePerformanceSnapshotMaterializedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<EmployeePerformanceSnapshotMaterializedIntegrationEvent> context)
    {
        await handler.HandleAsync(context.Message, context.CancellationToken);
    }
}

public sealed class EmployeeCompensationRevisedConsumer(IProjectionHandler<EmployeeCompensationRevisedIntegrationEvent> handler)
    : IConsumer<EmployeeCompensationRevisedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<EmployeeCompensationRevisedIntegrationEvent> context)
    {
        await handler.HandleAsync(context.Message, context.CancellationToken);
    }
}

public sealed class EmployeePerformanceSnapshotConsumer(IProjectionHandler<EmployeePerformanceSnapshotMaterializedIntegrationEvent> handler)
    : IConsumer<EmployeePerformanceSnapshotMaterializedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<EmployeePerformanceSnapshotMaterializedIntegrationEvent> context)
    {
        await handler.HandleAsync(context.Message, context.CancellationToken);
    }
}

/// <summary>
/// MassTransit consumer that handles <see cref="SkillValidatedIntegrationEvent"/> by delegating 
/// to the <see cref="EmployeeIntelligenceProjectionHandler"/>.
/// </summary>
public sealed class IntelligenceSkillValidatedConsumer(EmployeeIntelligenceProjectionHandler handler)
    : IConsumer<SkillValidatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<SkillValidatedIntegrationEvent> context)
    {
        await handler.HandleAsync(context.Message, context.CancellationToken);
    }
}

/// <summary>
/// MassTransit consumer that handles wrapped <see cref="ReportingRelationshipChanged"/> events 
/// by delegating to the <see cref="EmployeeIntelligenceProjectionHandler"/>.
/// </summary>
public sealed class IntelligenceReportingRelationshipChangedConsumer(EmployeeIntelligenceProjectionHandler handler)
    : IConsumer<EnterpriseEventEnvelope<ReportingRelationshipChanged>>
{
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<ReportingRelationshipChanged>> context)
    {
        await handler.HandleAsync(context.Message.Payload, context.CancellationToken);
    }
}


