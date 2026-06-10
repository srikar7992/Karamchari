// -----------------------------------------------------------------------
// <copyright file="EngagementServiceCollectionExtensions.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.DependencyInjection;
using Karamchari.Engagement.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Engagement.DependencyInjection;

public static class EngagementServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    public static IServiceCollection AddKaramchariEngagement(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.RegisterTenantTable("PulseSurveys");
        services.RegisterTenantTable("Recognitions");
        services.RegisterTenantTable("Badges");
        services.RegisterTenantTable("Announcements");
        services.RegisterTenantTable("Polls");

        services.AddDbContext<EngagementDbContext>((sp, options) =>
        {
            var cs = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"ConnectionStrings:{ConnectionStringName} not configured.");
            options.UseSqlServer(cs, sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null));
            options.AddKaramchariInterceptors(sp);
        });

        return services;
    }
}
