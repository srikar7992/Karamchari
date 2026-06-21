// -----------------------------------------------------------------------
// <copyright file="HRServiceCollectionExtensions.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Compensation.Contracts.IntegrationEvents;
using Karamchari.Core.DependencyInjection;
using Karamchari.Core.Messaging;
using Karamchari.Core.Projections;
using Karamchari.HR.Contracts.Employees;
using Karamchari.HR.Contracts.Organization;
using Karamchari.HR.Domain.Organization.Events;
using Karamchari.HR.Messaging;
using Karamchari.HR.Persistence;
using Karamchari.HR.Projections;
using Karamchari.HR.Services;
using Karamchari.Performance.Contracts.IntegrationEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Karamchari.HR.DependencyInjection;

/// <summary>
/// Extension methods for registering HR module services.
/// </summary>
public static class HRServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    /// <summary>
    /// Adds the HR module services to the service collection.
    /// </summary>
    public static IServiceCollection AddKaramchariHR(
        this IServiceCollection services,
        IConfiguration configuration,
        MassTransit.IBusRegistrationConfigurator? busConfigurator = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (busConfigurator != null)
        {
            AddKaramchariHRConsumers(busConfigurator);
        }

        // RLS: Employees lives under each tenant's schema and must be covered by the
        // per-tenant security policy. Forgetting this line is a security regression.
        services.RegisterTenantTable("Departments");
        services.RegisterTenantTable("Employees");
        services.RegisterTenantTable("EmployeeRelationships");
        services.RegisterTenantTable("EmployeeHistory");
        services.RegisterTenantTable("Designations");
        services.RegisterTenantTable("Branches");
        services.RegisterTenantTable("LegalEntities");
        services.RegisterTenantTable("CostCenters");
        services.RegisterTenantTable("BusinessUnits");
        services.RegisterTenantTable("Positions");
        services.RegisterTenantTable("Locations");
        services.RegisterTenantTable("HR_PositionAssignments");
        services.RegisterTenantTable("HR_PositionBudgets");
        services.RegisterTenantTable("HR_PositionVacancies");
        services.RegisterTenantTable("HR_OrganizationUnits");
        services.RegisterTenantTable("HR_ReportingRelationships");
        services.RegisterTenantTable("HR_FutureOrgScenarios");
        services.RegisterTenantTable("HR_ScenarioChanges");
        services.RegisterTenantTable("HR_OrganizationSnapshots");
        services.RegisterTenantTable("HR_OrgHierarchyProjections");
        services.RegisterTenantTable("HR_OrgMetricsProjections");
        services.RegisterTenantTable("HR_PositionOccupancyProjections");
        services.RegisterTenantTable("HR_PositionOccupantProjections");
        services.RegisterTenantTable("HR_VacancyPipelineProjections");
        services.RegisterTenantTable("HR_SpanOfControlProjections");
        services.RegisterTenantTable("HR_ManagerDirectReportProjections");
        services.RegisterTenantTable("HR_GraphNodes");
        services.RegisterTenantTable("HR_GraphEdges");
        services.RegisterTenantTable("HR_EmployeeCompensationProjections");
        services.RegisterTenantTable("HR_EmployeePerformanceProjections");
        services.RegisterTenantTable("HR_FlightRiskPredictions");
        services.RegisterTenantTable("HR_FlightRiskDrivers");
        services.RegisterTenantTable("HR_PromotionReadinessPredictions");
        services.RegisterTenantTable("HR_PromotionReadinessDrivers");

        // Replace the Core-shipped fail-closed null dispatcher with the
        // MassTransit-backed publisher. Replace (not Add) ensures we win even
        // if AddKaramchariCore registered the null implementation first.
        services.Replace(ServiceDescriptor.Scoped<IDomainEventDispatcher, MassTransitDomainEventDispatcher>());
        services.AddSingleton<Karamchari.HR.Observability.KaramchariGraphMetrics>();
        services.AddHostedService<Karamchari.HR.Observability.GraphStatisticsPollerService>();

        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IVisibilityResolver, VisibilityResolver>();
        services.AddScoped<Karamchari.HR.Services.EmployeeSummaryService>();
        services.AddScoped<IWorkforceGraphService, WorkforceGraphService>();

        services.Configure<DocumentIntelligenceOptions>(
            configuration.GetSection(DocumentIntelligenceOptions.SectionName));
        services.AddScoped<IDocumentIntelligenceService, DocumentIntelligenceService>();

        // Register projection handlers
        services.AddScoped<IProjectionHandler<OrganizationUnitCreated>, OrganizationHierarchyProjectionHandler>();
        services.AddScoped<IProjectionHandler<OrganizationUnitDeactivated>, OrganizationHierarchyProjectionHandler>();

        services.AddScoped<IProjectionHandler<PositionCreated>, PositionOccupancyProjectionHandler>();
        services.AddScoped<IProjectionHandler<PositionStatusChanged>, PositionOccupancyProjectionHandler>();
        services.AddScoped<IProjectionHandler<PositionAssignmentCreated>, PositionOccupancyProjectionHandler>();
        services.AddScoped<IProjectionHandler<PositionAssignmentEnded>, PositionOccupancyProjectionHandler>();

        services.AddScoped<IProjectionHandler<PositionVacancyOpened>, VacancyPipelineProjectionHandler>();
        services.AddScoped<IProjectionHandler<PositionVacancyClosed>, VacancyPipelineProjectionHandler>();

        services.AddScoped<IProjectionHandler<ReportingRelationshipCreated>, SpanOfControlProjectionHandler>();
        services.AddScoped<IProjectionHandler<ReportingRelationshipEnded>, SpanOfControlProjectionHandler>();

        // Register intelligence handlers and calculators
        services.AddScoped<EmployeeIntelligenceProjectionHandler>();
        services.AddScoped<IProjectionHandler<EmployeeCompensationRevisedIntegrationEventV1>, EmployeeCompensationProjectionHandler>();
        services.AddScoped<IProjectionHandler<EmployeePerformanceSnapshotMaterializedIntegrationEventV1>, EmployeePerformanceProjectionHandler>();

        services.AddDbContext<HRDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"ConnectionStrings:{ConnectionStringName} must be configured before HRDbContext can be resolved.");

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
            options.AddKaramchariInterceptors(serviceProvider);
        });

        return services;
    }

    /// <summary>
    /// Registers HR module consumers for MassTransit.
    /// </summary>
    public static void AddKaramchariHRConsumers(this MassTransit.IBusRegistrationConfigurator busConfigurator)
    {
        ArgumentNullException.ThrowIfNull(busConfigurator);
        busConfigurator.AddConsumer<Karamchari.HR.Consumers.TenantProvisionedConsumer>();
        busConfigurator.AddConsumer<Karamchari.HR.Consumers.CreateEmployeeOnCandidateHiredConsumer>();
        busConfigurator.AddConsumer<Karamchari.HR.Consumers.OrganizationUnitCreatedConsumer>();
        busConfigurator.AddConsumer<Karamchari.HR.Consumers.OrganizationUnitDeactivatedConsumer>();
        busConfigurator.AddConsumer<Karamchari.HR.Consumers.PositionCreatedConsumer>();
        busConfigurator.AddConsumer<Karamchari.HR.Consumers.PositionStatusChangedConsumer>();
        busConfigurator.AddConsumer<Karamchari.HR.Consumers.PositionAssignmentCreatedConsumer>();
        busConfigurator.AddConsumer<Karamchari.HR.Consumers.PositionAssignmentEndedConsumer>();
        busConfigurator.AddConsumer<Karamchari.HR.Consumers.PositionVacancyOpenedConsumer>();
        busConfigurator.AddConsumer<Karamchari.HR.Consumers.PositionVacancyClosedConsumer>();
        busConfigurator.AddConsumer<Karamchari.HR.Consumers.ReportingRelationshipCreatedConsumer>();
        busConfigurator.AddConsumer<Karamchari.HR.Consumers.ReportingRelationshipEndedConsumer>();

        // Intelligence Consumers
        busConfigurator.AddConsumer<Karamchari.HR.Consumers.IntelligencePositionAssignmentCreatedConsumer>();
        busConfigurator.AddConsumer<Karamchari.HR.Consumers.IntelligencePositionAssignmentEndedConsumer>();
        busConfigurator.AddConsumer<Karamchari.HR.Consumers.IntelligenceReportingRelationshipCreatedConsumer>();
        busConfigurator.AddConsumer<Karamchari.HR.Consumers.IntelligenceReportingRelationshipEndedConsumer>();
        busConfigurator.AddConsumer<Karamchari.HR.Consumers.IntelligenceEmployeeCompensationRevisedConsumer>();
        busConfigurator.AddConsumer<Karamchari.HR.Consumers.IntelligenceEmployeePerformanceSnapshotConsumer>();
        busConfigurator.AddConsumer<Karamchari.HR.Consumers.EmployeeCompensationRevisedConsumer>();
        busConfigurator.AddConsumer<Karamchari.HR.Consumers.EmployeePerformanceSnapshotConsumer>();
        busConfigurator.AddConsumer<Karamchari.HR.Consumers.IntelligenceSkillValidatedConsumer>();
        busConfigurator.AddConsumer<Karamchari.HR.Consumers.IntelligenceReportingRelationshipChangedConsumer>();
    }
}
