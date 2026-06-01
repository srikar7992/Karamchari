using Karamchari.Billing.DependencyInjection;
using Karamchari.Core.Messaging.Tenant;
using Karamchari.Capability.DependencyInjection;
using Karamchari.Compensation.DependencyInjection;
using Karamchari.DataMigration.DependencyInjection;
using Karamchari.FinancialOps.DependencyInjection;
using Karamchari.Forecasting.DependencyInjection;
using Karamchari.Governance.DependencyInjection;
using Karamchari.HR.DependencyInjection;
using Karamchari.Identity.Infrastructure;
using Karamchari.Intelligence.DependencyInjection;
using Karamchari.Notifications.DependencyInjection;
using Karamchari.Payroll.DependencyInjection;
using Karamchari.Performance.DependencyInjection;
using Karamchari.PSA.DependencyInjection;
using Karamchari.Recruitment.DependencyInjection;
using Karamchari.Recruitment.Worker.DependencyInjection;
using Karamchari.TimeAttendance.DependencyInjection;
using Karamchari.Workflow.DependencyInjection;
using Karamchari.Core.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Karamchari.Worker.DependencyInjection;

public static class WorkerServiceCollectionExtensions
{
    public static IServiceCollection AddKaramchariWorkerMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // 4. Scoped Consumers for every module
        services.AddKaramchariMessaging(configuration, environment, x =>
        {
            new HRModule(configuration).RegisterConsumers(x);
            new PayrollModule(configuration).RegisterConsumers(x);
            
            var recruitment = new RecruitmentModule(configuration);
            recruitment.RegisterConsumers(x);
            x.AddRecruitmentConsumers();

            new TimeAttendanceModule(configuration).RegisterConsumers(x);
            new BillingModule(configuration).RegisterConsumers(x);
            new PSAModule(configuration).RegisterConsumers(x);
            new PerformanceModule(configuration).RegisterConsumers(x);
            new IntelligenceModule(configuration).RegisterConsumers(x);
            new DataMigrationModule(configuration).RegisterConsumers(x);
            new FinancialOpsModule(configuration).RegisterConsumers(x);
            new GovernanceModule(configuration).RegisterConsumers(x);
            new ForecastingModule(configuration).RegisterConsumers(x);
            new CompensationModule(configuration).RegisterConsumers(x);
            new CapabilityModule(configuration).RegisterConsumers(x);
            new WorkflowModule(configuration).RegisterConsumers(x);
            new NotificationsModule(configuration).RegisterConsumers(x);
        });

        return services;
    }

    public static IServiceCollection AddKaramchariWorkerPool(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Internal platform-wide background services
        services.AddOutboxRelay(configuration);
        
        // HR Workers
        services.AddHostedService<Karamchari.HR.Services.OrganizationHierarchyCleanupWorker>();
        
        // Payroll Workers
        services.AddHostedService<Karamchari.Payroll.Services.PayrollCycleCloser>();
        
        // Intelligence Workers
        services.AddHostedService<Karamchari.Intelligence.Services.DriftDetectionWorker>();
        services.AddHostedService<Karamchari.Notifications.BackgroundServices.DigestGenerationWorker>();

        // Identity Workers
        services.AddKaramchariIdentityWorkers();

        return services;
    }
}
