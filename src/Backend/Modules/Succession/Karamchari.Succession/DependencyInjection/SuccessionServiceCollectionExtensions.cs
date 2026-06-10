// -----------------------------------------------------------------------
// <copyright file="SuccessionServiceCollectionExtensions.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.DependencyInjection;
using Karamchari.Core.Messaging;
using Karamchari.Core.Projections;
using Karamchari.HR.Domain.Organization.Events;
using Karamchari.HR.Messaging;
using Karamchari.Succession.Domain.Events;
using Karamchari.Succession.Persistence;
using Karamchari.Succession.Projections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Karamchari.Succession.DependencyInjection;

public static class SuccessionServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    public static IServiceCollection AddKaramchariSuccession(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.RegisterTenantTable("Succession_CriticalRoles");
        services.RegisterTenantTable("Succession_Plans");
        services.RegisterTenantTable("Succession_Candidates");
        services.RegisterTenantTable("Succession_TalentPools");
        services.RegisterTenantTable("Succession_TalentPoolMembers");
        services.RegisterTenantTable("Succession_CareerPaths");
        services.RegisterTenantTable("Succession_CareerSteps");
        services.RegisterTenantTable("Succession_CoverageProjections");

        services.AddDbContext<SuccessionDbContext>((sp, options) =>
        {
            var cs = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"ConnectionStrings:{ConnectionStringName} not configured.");
            options.UseSqlServer(cs, sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null));
            options.AddKaramchariInterceptors(sp);
        });

        services.Replace(ServiceDescriptor.Scoped<IDomainEventDispatcher, MassTransitDomainEventDispatcher>());

        // Register projection handlers for Succession context
        services.AddScoped<IProjectionHandler<PositionCreated>, SuccessionCoverageProjectionHandler>();
        services.AddScoped<IProjectionHandler<PositionAssignmentCreated>, SuccessionCoverageProjectionHandler>();
        services.AddScoped<IProjectionHandler<PositionAssignmentEnded>, SuccessionCoverageProjectionHandler>();
        services.AddScoped<IProjectionHandler<SuccessorAddedEvent>, SuccessionCoverageProjectionHandler>();
        services.AddScoped<IProjectionHandler<SuccessorPromotedEvent>, SuccessionCoverageProjectionHandler>();

        return services;
    }
}
