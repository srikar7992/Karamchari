// -----------------------------------------------------------------------
// <copyright file="SecurityWebApplicationFactory.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using System.Text.Encodings.Web;
using Karamchari.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Karamchari.SecurityTests;

/// <summary>
/// Shared WebApplicationFactory for security tests.
/// Replaces all SQL Server DbContexts with in-memory equivalents and
/// installs a configurable test authentication handler so individual
/// tests can exercise specific JWT scenarios.
/// </summary>
public sealed class SecurityWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove all DbContext registrations to avoid SQL Server provider conflicts
            var dbContextTypes = new[]
            {
                typeof(IdentityDbContext),
                typeof(Karamchari.Payroll.Data.PayrollDbContext),
                typeof(Karamchari.HR.Persistence.HRDbContext),
                typeof(Karamchari.TimeAttendance.Persistence.TimeAttendanceDbContext),
                typeof(Karamchari.PSA.Persistence.PSADbContext),
                typeof(Karamchari.Performance.Persistence.PerformanceDbContext),
                typeof(Karamchari.Notifications.Persistence.NotificationsDbContext),
                typeof(Karamchari.Compensation.Persistence.CompensationDbContext),
                typeof(Karamchari.Billing.Persistence.BillingDbContext),
                typeof(Karamchari.Forecasting.Persistence.ForecastingDbContext),
                typeof(Karamchari.Core.Persistence.CoreDbContext),
                typeof(Karamchari.Recruitment.Persistence.RecruitmentDbContext),
                typeof(Karamchari.Capability.Persistence.CapabilityDbContext),
                typeof(Karamchari.Intelligence.Persistence.IntelligenceDbContext),
                typeof(Karamchari.Governance.Persistence.GovernanceDbContext),
            };

            foreach (var dbContextType in dbContextTypes)
            {
                var optionsType = typeof(DbContextOptions<>).MakeGenericType(dbContextType);
                services.RemoveAll(optionsType);
                services.RemoveAll(dbContextType);
            }

            services.RemoveAll<DbContext>();
            services.RemoveAll<DbContextOptions<DbContext>>();
            services.RemoveAll<IHostedService>();

            var inMemoryProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<IdentityDbContext>(o => ConfigureInMemory(o, "SecurityIdentityDb", inMemoryProvider), ServiceLifetime.Scoped, ServiceLifetime.Singleton);
            services.AddDbContextFactory<IdentityDbContext>(o => ConfigureInMemory(o, "SecurityIdentityDb", inMemoryProvider), ServiceLifetime.Singleton);
            services.AddDbContext<Karamchari.Payroll.Data.PayrollDbContext>(o => ConfigureInMemory(o, "SecurityPayrollDb", inMemoryProvider));
            services.AddDbContext<Karamchari.HR.Persistence.HRDbContext>(o => ConfigureInMemory(o, "SecurityHRDb", inMemoryProvider));
            services.AddDbContext<Karamchari.TimeAttendance.Persistence.TimeAttendanceDbContext>(o => ConfigureInMemory(o, "SecurityTimeAttendanceDb", inMemoryProvider));
            services.AddDbContext<Karamchari.PSA.Persistence.PSADbContext>(o => ConfigureInMemory(o, "SecurityPSADb", inMemoryProvider));
            services.AddDbContext<Karamchari.Performance.Persistence.PerformanceDbContext>(o => ConfigureInMemory(o, "SecurityPerformanceDb", inMemoryProvider));
            services.AddDbContext<Karamchari.Notifications.Persistence.NotificationsDbContext>(o => ConfigureInMemory(o, "SecurityNotificationsDb", inMemoryProvider));
            services.AddDbContext<Karamchari.Compensation.Persistence.CompensationDbContext>(o => ConfigureInMemory(o, "SecurityCompensationDb", inMemoryProvider));
            services.AddDbContext<Karamchari.Billing.Persistence.BillingDbContext>(o => ConfigureInMemory(o, "SecurityBillingDb", inMemoryProvider));
            services.AddDbContext<Karamchari.Forecasting.Persistence.ForecastingDbContext>(o => ConfigureInMemory(o, "SecurityForecastingDb", inMemoryProvider));
            services.AddDbContext<Karamchari.Core.Persistence.CoreDbContext>(o => ConfigureInMemory(o, "SecurityCoreDb", inMemoryProvider));
            services.AddDbContext<Karamchari.Recruitment.Persistence.RecruitmentDbContext>(o => ConfigureInMemory(o, "SecurityRecruitmentDb", inMemoryProvider));
            services.AddDbContext<Karamchari.Capability.Persistence.CapabilityDbContext>(o => ConfigureInMemory(o, "SecurityCapabilityDb", inMemoryProvider));
            services.AddDbContext<Karamchari.Intelligence.Persistence.IntelligenceDbContext>(o => ConfigureInMemory(o, "SecurityIntelligenceDb", inMemoryProvider));
            services.AddDbContext<Karamchari.Governance.Persistence.GovernanceDbContext>(o => ConfigureInMemory(o, "SecurityGovernanceDb", inMemoryProvider));
            services.AddScoped<DbContext>(sp => sp.GetRequiredService<Karamchari.Payroll.Data.PayrollDbContext>());

            // Replace domain event dispatcher to avoid MassTransit reflection issues
            services.RemoveAll<Karamchari.Core.Messaging.IDomainEventDispatcher>();
            services.AddScoped<Karamchari.Core.Messaging.IDomainEventDispatcher, NullDomainEventDispatcher>();
        });
    }

    private static void ConfigureInMemory(DbContextOptionsBuilder options, string databaseName, IServiceProvider provider)
    {
        options.UseInMemoryDatabase(databaseName);
        options.UseInternalServiceProvider(provider);
    }

    /// <summary>
    /// Creates an HTTP client authenticated as the specified tenant and role.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(
        string tenantId,
        string role = "Employee",
        string? userId = null)
    {
        var client = WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication("Test")
                    .AddScheme<TestAuthOptions, ConfigurableTestAuthHandler>(
                        "Test",
                        opts =>
                        {
                            opts.TenantId = tenantId;
                            opts.Role = role;
                            opts.UserId = userId ?? Guid.NewGuid().ToString();
                            opts.Authenticated = true;
                        });

                services.PostConfigure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                });
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Host = $"{tenantId}.karamchari.com";
        return client;
    }

    /// <summary>
    /// Creates an unauthenticated client (simulates missing/invalid token scenarios
    /// without using the test auth handler — the real JWT middleware is engaged).
    /// </summary>
    public HttpClient CreateUnauthenticatedClient(string tenantId = "acme")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Host = $"{tenantId}.karamchari.com";
        return client;
    }
}

public sealed class TestAuthOptions : AuthenticationSchemeOptions
{
    public string TenantId { get; set; } = "acme";
    public string Role { get; set; } = "Employee";
    public string UserId { get; set; } = Guid.NewGuid().ToString();
    public bool Authenticated { get; set; } = true;
}

/// <summary>
/// Configurable test authentication handler: builds a ClaimsPrincipal from TestAuthOptions.
/// </summary>
public sealed class ConfigurableTestAuthHandler : AuthenticationHandler<TestAuthOptions>
{
    public ConfigurableTestAuthHandler(
        IOptionsMonitor<TestAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Options.Authenticated)
        {
            return Task.FromResult(AuthenticateResult.Fail("Test: authentication disabled"));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "Security Test User"),
            new(ClaimTypes.NameIdentifier, Options.UserId),
            new("tenant_id", Options.TenantId),
            new(ClaimTypes.Role, Options.Role),
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

internal sealed class NullDomainEventDispatcher : Karamchari.Core.Messaging.IDomainEventDispatcher
{
    public Task DispatchAsync(
        IReadOnlyCollection<Karamchari.Core.Domain.Primitives.IDomainEvent> events,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
