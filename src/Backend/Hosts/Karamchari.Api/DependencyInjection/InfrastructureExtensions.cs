// -----------------------------------------------------------------------
// <copyright file="InfrastructureExtensions.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading.RateLimiting;
using FluentValidation;
using Karamchari.HR.Persistence;
using Karamchari.Notifications.RealTime;
using Karamchari.PSA.DependencyInjection;
using Karamchari.PSA.Hubs;
using Karamchari.PSA.Persistence;
using Karamchari.PSA.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Karamchari.Api.DependencyInjection;

/// <summary>
/// Contains extension methods for registering foundational platform infrastructure, 
/// including exception handling, validation, rate limiting, and observability.
/// </summary>
public static class InfrastructureExtensions
{
    /// <summary>
    /// Registers cross-cutting infrastructure services, OpenTelemetry instrumentation, 
    /// persistence contexts, and background workers required for platform operation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddKaramchariInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Exception Handling
        services.AddExceptionHandler<Karamchari.Api.Middleware.GlobalExceptionHandler>();
        services.AddProblemDetails();

        // Validation
        services.AddValidatorsFromAssemblyContaining<Karamchari.Api.Validation.StartPayrollRunRequestValidator>();

        // Rate Limiting Configuration
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("ai", opt =>
            {
                opt.PermitLimit = 5;
                opt.Window = TimeSpan.FromSeconds(10);
                opt.QueueLimit = 2;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            options.AddFixedWindowLimiter("ess", opt =>
            {
                opt.PermitLimit = 10;
                opt.Window = TimeSpan.FromSeconds(1);
            });

            // Abuse protection for authentication endpoints (login/register/refresh).
            // Partitioned per client IP so one attacker cannot exhaust the limit for everyone.
            options.AddPolicy("auth", httpContext =>
                System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        // OpenTelemetry
        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(o =>
                {
                    o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
                    o.EnrichWithHttpRequest = (activity, request) =>
                    {
                        var tenantId = request.HttpContext.User.FindFirst("tenant_id")?.Value;
                        if (!string.IsNullOrEmpty(tenantId))
                        {
                            activity.SetTag("tenant.id", tenantId);
                        }
                    };
                })
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation(o =>
                {
                    o.SetDbStatementForText = true;
                })
                .AddSource("MassTransit")
                .AddSource("Karamchari.*")
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317");
                }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("Karamchari.*")
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317");
                }));

        // PSA context services
        services.AddKaramchariPSA(configuration);

        // Compliance Generators
        services.AddScoped<Karamchari.Payroll.Services.Compliance.IEcrGenerator, Karamchari.Payroll.Services.Compliance.EcrGenerator>();
        services.AddScoped<Karamchari.Payroll.Services.Compliance.IEsicGenerator, Karamchari.Payroll.Services.Compliance.EsicGenerator>();
        services.AddScoped<Karamchari.Payroll.Services.Compliance.ITdsGenerator, Karamchari.Payroll.Services.Compliance.TdsGenerator>();
        services.AddScoped<Karamchari.Payroll.Services.Compliance.IComplianceRiskEngine, Karamchari.Payroll.Services.Compliance.ComplianceRiskEngine>();
        services.AddScoped<Karamchari.Payroll.Services.Compliance.IComplianceService, Karamchari.Payroll.Services.Compliance.ComplianceService>();

        // SignalR
        services.AddSignalR();
        services.AddScoped<INotificationPushService, HubNotificationPushService>();

        return services;
    }
}
