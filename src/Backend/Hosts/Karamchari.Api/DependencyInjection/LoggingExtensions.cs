using Karamchari.Core.Observability.Tenant;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;

namespace Karamchari.Api.DependencyInjection;

/// <summary>
/// Contains extension methods for configuring Serilog and OpenTelemetry logging in the Karamchari platform.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Configures Serilog as the primary logging provider, enriches log events with tenant and environment context,
    /// and sets up sinks for Console and OpenTelemetry.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance.</param>
    public static void AddKaramchariLogging(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.With<TenantLogEnricher>()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProcessId()
                .Enrich.WithProperty("Application", "Karamchari.Api")
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .Enrich.WithProperty("MachineName", Environment.MachineName)
                .Enrich.WithProperty("ApplicationVersion", typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0")
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File("logs/karamchari-.log", rollingInterval: RollingInterval.Day, outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = context.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317";
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = "Karamchari.Api",
                        ["deployment.environment"] = context.HostingEnvironment.EnvironmentName
                    };
                });

            if (context.HostingEnvironment.IsDevelopment())
            {
                configuration.MinimumLevel.Override("Microsoft", LogEventLevel.Information);
                configuration.MinimumLevel.Override("MassTransit", LogEventLevel.Debug);
            }
            else
            {
                configuration.MinimumLevel.Override("Microsoft", LogEventLevel.Warning);
                configuration.MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information);
            }
        });
    }
}
