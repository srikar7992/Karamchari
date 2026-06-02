using Karamchari.Core.DependencyInjection;
using Karamchari.Recruitment.Api;
using Karamchari.Recruitment.Application;
using Karamchari.Recruitment.Persistence;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Karamchari.Recruitment.DependencyInjection;

public class RecruitmentModule(IConfiguration configuration) : ICapabilityModule
{
    public void RegisterServices(IServiceCollection services)
    {
        if (!services.Any(d => d.ServiceType == typeof(DbContextOptions<RecruitmentDbContext>)))
        {
            services.AddDbContext<RecruitmentDbContext>((sp, opts) =>
            {
                var connectionString = configuration.GetConnectionString("KaramchariDb");
                if (!string.IsNullOrEmpty(connectionString))
                {
                    opts.UseSqlServer(connectionString);
                }
                opts.AddKaramchariInterceptors(sp);
            });
        }

        if (!services.Any(d => d.ServiceType == typeof(IRecruitmentDbContext)))
        {
            services.AddScoped<IRecruitmentDbContext, RecruitmentDbContext>();
        }

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(Karamchari.Recruitment.Application.Commands.CreateRequisitionCommand).Assembly));
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapRecruitmentEndpoints();
    }

    public void RegisterConsumers(IBusRegistrationConfigurator configurator)
    {
        // NOTE: Consumers are registered directly in the Worker host or via Worker extension
        // to avoid Api -> Worker dependency.
        configurator.AddEntityFrameworkOutbox<RecruitmentDbContext>(o =>
        {
            o.UseBusOutbox();
        });
    }
}
