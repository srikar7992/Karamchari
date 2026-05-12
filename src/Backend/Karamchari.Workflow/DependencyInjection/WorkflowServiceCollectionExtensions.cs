using Karamchari.Core.DependencyInjection;
using Karamchari.Core.Persistence;
using Karamchari.Workflow.Consumers;
using Karamchari.Workflow.Persistence;
using Karamchari.Workflow.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Workflow.DependencyInjection;

/// <summary>
/// Composition root for the Workflow module.
/// </summary>
public static class WorkflowServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    /// <summary>
    /// Registers workflow engine services and persistence.
    /// </summary>
    public static IServiceCollection AddKaramchariWorkflow(
        this IServiceCollection services,
        IConfiguration configuration,
        IBusRegistrationConfigurator? busConfigurator = null)
    {
        services.AddScoped<WorkflowMetricsHandler>();

        if (busConfigurator != null)
        {
            AddKaramchariWorkflowConsumers(busConfigurator);
        }

        // RLS
        services.RegisterTenantTable("Workflow_Definitions");
        services.RegisterTenantTable("Workflow_Instances");

        services.AddDbContext<WorkflowDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName);
            options.UseSqlServer(connectionString);
            options.AddKaramchariInterceptors(sp);
        });

        return services;
    }

    /// <summary>
    /// Registers Workflow module consumers for MassTransit.
    /// </summary>
    public static void AddKaramchariWorkflowConsumers(this IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(busConfigurator);
        busConfigurator.AddConsumer<WorkflowTraceabilityConsumer>();
        busConfigurator.AddConsumer<WorkflowOrchestratorConsumer>();
    }
}
