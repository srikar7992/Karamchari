// -----------------------------------------------------------------------
// <copyright file="HelpdeskServiceCollectionExtensions.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.DependencyInjection;
using Karamchari.Helpdesk.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Helpdesk.DependencyInjection;

public static class HelpdeskServiceCollectionExtensions
{
    private const string ConnectionStringName = "KaramchariDb";

    public static IServiceCollection AddKaramchariHelpdesk(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.RegisterTenantTable("SupportTickets");
        services.RegisterTenantTable("TicketCategories");
        services.RegisterTenantTable("KbArticles");
        services.RegisterTenantTable("EscalationRules");

        services.AddDbContext<HelpdeskDbContext>((sp, options) =>
        {
            var cs = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"ConnectionStrings:{ConnectionStringName} not configured.");
            options.UseSqlServer(cs, sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null));
            options.AddKaramchariInterceptors(sp);
        });

        return services;
    }
}
