// -----------------------------------------------------------------------
// <copyright file="RecruitmentWorkerExtensions.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Recruitment.Application.EventHandlers.Analytics;
using Karamchari.Recruitment.Worker.Consumers;
using MassTransit;

namespace Karamchari.Recruitment.Worker.DependencyInjection;

public static class RecruitmentWorkerExtensions
{
    public static void AddRecruitmentConsumers(this IBusRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<RequisitionPublishedAuditConsumer>();
        configurator.AddConsumer<RecruitmentVelocityConsumer>();
        configurator.AddConsumer<TimeToHireConsumer>();
        configurator.AddConsumer<HiringFunnelConsumer>();
    }
}
