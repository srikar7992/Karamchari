using Karamchari.Recruitment.Worker.Consumers;
using MassTransit;

namespace Karamchari.Recruitment.Worker.DependencyInjection;

public static class RecruitmentWorkerExtensions
{
    public static void AddRecruitmentConsumers(this IBusRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<RequisitionPublishedAuditConsumer>();
    }
}
