using Karamchari.Workflow.Consumers;
using Karamchari.Workflow.Services;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Workflow.DependencyInjection;

/// <summary>
/// Composition root for the Workflow module.
/// </summary>
public static class WorkflowServiceCollectionExtensions
{
    /// <summary>
    /// Registers workflow engine services, including the condition compiler, metrics handlers, 
    /// and observability infrastructure required for business process orchestration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="busConfigurator">Optional MassTransit bus configurator for registering consumers.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddKaramchariWorkflow(
        this IServiceCollection services,
        IBusRegistrationConfigurator? busConfigurator = null)
    {
        services.AddSingleton<WorkflowConditionCompiler>();
        services.AddScoped<WorkflowMetricsHandler>();

        busConfigurator?.AddConsumer<WorkflowTraceabilityConsumer>();

        return services;
    }
}
