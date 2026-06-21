// -----------------------------------------------------------------------
// <copyright file="WorkerServiceCollectionExtensions.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Analytics.DependencyInjection;
using Karamchari.Approvals.DependencyInjection;
using Karamchari.Benefits.DependencyInjection;
using Karamchari.Billing.DependencyInjection;
using Karamchari.Extensibility.DependencyInjection;
using Karamchari.Succession.DependencyInjection;
using Karamchari.Capability.DependencyInjection;
using Karamchari.Compensation.DependencyInjection;
using Karamchari.Core.DependencyInjection;
using Karamchari.Core.Messaging.Tenant;
using Karamchari.DataMigration.DependencyInjection;
using Karamchari.FinancialOps.DependencyInjection;
using Karamchari.Forecasting.DependencyInjection;
using Karamchari.Governance.DependencyInjection;
using Karamchari.HR.DependencyInjection;
using Karamchari.Identity.Infrastructure;
using Karamchari.Intelligence.DependencyInjection;
using Karamchari.Notifications.DependencyInjection;
using Karamchari.Onboarding.DependencyInjection;
using Karamchari.Payroll.DependencyInjection;
using Karamchari.Performance.DependencyInjection;
using Karamchari.PSA.DependencyInjection;
using Karamchari.Recruitment.DependencyInjection;
using Karamchari.Recruitment.Worker.DependencyInjection;
using Karamchari.TimeAttendance.DependencyInjection;
using Karamchari.Workflow.DependencyInjection;
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
        // Register module DbContexts and domain services required by consumers.
        // Mirrors the API's CapabilityRegistry loop — consumers cannot activate without
        // their module's DbContext registered in the Worker's DI container.
        new ApprovalsModule(configuration).RegisterServices(services);
        new BenefitsModule(configuration).RegisterServices(services);
        new AnalyticsModule(configuration).RegisterServices(services);
        new HRModule(configuration).RegisterServices(services);
        new PayrollModule(configuration).RegisterServices(services);
        new RecruitmentModule(configuration).RegisterServices(services);
        new TimeAttendanceModule(configuration).RegisterServices(services);
        new BillingModule(configuration).RegisterServices(services);
        new PSAModule(configuration).RegisterServices(services);
        new PerformanceModule(configuration).RegisterServices(services);
        new IntelligenceModule(configuration).RegisterServices(services);
        new DataMigrationModule(configuration).RegisterServices(services);
        new FinancialOpsModule(configuration).RegisterServices(services);
        new GovernanceModule(configuration).RegisterServices(services);
        new ForecastingModule(configuration).RegisterServices(services);
        new CompensationModule(configuration).RegisterServices(services);
        new SuccessionModule(configuration).RegisterServices(services);
        new ExtensibilityModule(configuration).RegisterServices(services);
        new CapabilityModule(configuration).RegisterServices(services);
        new WorkflowModule(configuration).RegisterServices(services);
        new NotificationsModule(configuration).RegisterServices(services);

        // 4. Scoped Consumers for every module
        services.AddKaramchariMessaging(configuration, environment, x =>
        {
            new ApprovalsModule(configuration).RegisterConsumers(x);
            new BenefitsModule(configuration).RegisterConsumers(x);
            new AnalyticsModule(configuration).RegisterConsumers(x);
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
            new SuccessionModule(configuration).RegisterConsumers(x);
            new ExtensibilityModule(configuration).RegisterConsumers(x);
            new CapabilityModule(configuration).RegisterConsumers(x);
            new WorkflowModule(configuration).RegisterConsumers(x);
            new NotificationsModule(configuration).RegisterConsumers(x);
            new OnboardingModule(configuration).RegisterConsumers(x);
        });

        return services;
    }

    public static IServiceCollection AddKaramchariWorkerPool(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Module services required by consumers and hosted services.
        // Consumers injecting a DbContext need the DbContext registered in the Worker container.
        services.AddKaramchariOnboarding(configuration);
        services.AddKaramchariCapability(configuration);

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
