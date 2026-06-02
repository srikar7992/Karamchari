using System.Security.Claims;
using System.Text.Encodings.Web;
using Karamchari.Core.Multitenancy;
using Karamchari.Recruitment.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Karamchari.Recruitment.Tests.Api;

public sealed class RecruitmentApiCertificationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "karamchari-api-cert-test-secret-minimum-32-bytes-xxxxx"
            });
        });

        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = false;
            options.ValidateOnBuild = false;
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();
            RemoveAndReplaceWithInMemory(services);

            // Add a test-only authentication handler that signs in a user based on the X-Test-User-Role header
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder("Test")
                    .RequireAuthenticatedUser()
                    .Build();
            });
        });
    }

    private static void RemoveAndReplaceWithInMemory(IServiceCollection services)
    {
        // Use a SINGLE shared provider for all contexts in this factory to avoid provider collisions
        // while still isolating from the rest of the app's potentially polluted service collection.
        var sharedIsolatedProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        void InMemory(DbContextOptionsBuilder o, string name)
        {
            o.UseInMemoryDatabase(name)
             .UseInternalServiceProvider(sharedIsolatedProvider);
        }

        var contextTypes = new[]
        {
            typeof(Karamchari.Identity.Infrastructure.Persistence.IdentityDbContext),
            typeof(Karamchari.HR.Persistence.HRDbContext),
            typeof(Karamchari.Payroll.Data.PayrollDbContext),
            typeof(Karamchari.TimeAttendance.Persistence.TimeAttendanceDbContext),
            typeof(Karamchari.PSA.Persistence.PSADbContext),
            typeof(Karamchari.Performance.Persistence.PerformanceDbContext),
            typeof(Karamchari.Notifications.Persistence.NotificationsDbContext),
            typeof(Karamchari.Compensation.Persistence.CompensationDbContext),
            typeof(Karamchari.Billing.Persistence.BillingDbContext),
            typeof(Karamchari.Forecasting.Persistence.ForecastingDbContext),
            typeof(Karamchari.Core.Persistence.CoreDbContext),
            typeof(Karamchari.Workflow.Persistence.WorkflowDbContext),
            typeof(RecruitmentDbContext),
            typeof(Karamchari.DataMigration.Persistence.DataMigrationDbContext),
            typeof(Karamchari.Capability.Persistence.CapabilityDbContext),
            typeof(Karamchari.Intelligence.Persistence.IntelligenceDbContext),
            typeof(Karamchari.Governance.Persistence.GovernanceDbContext),
            typeof(Karamchari.FinancialOps.Persistence.FinancialOpsDbContext),
            typeof(Karamchari.Core.Messaging.Outbox.OutboxRelayDbContext),
        };

        foreach (var ctxType in contextTypes)
        {
            var optType = typeof(DbContextOptions<>).MakeGenericType(ctxType);
            services.RemoveAll(optType);
            services.RemoveAll(ctxType);
        }
        services.RemoveAll<DbContext>();
        services.RemoveAll<DbContextOptions<DbContext>>();

        services.AddDbContext<Karamchari.Identity.Infrastructure.Persistence.IdentityDbContext>(
            o => InMemory(o, "CertIdentity"), ServiceLifetime.Scoped, ServiceLifetime.Singleton);
        services.AddDbContextFactory<Karamchari.Identity.Infrastructure.Persistence.IdentityDbContext>(
            o => InMemory(o, "CertIdentity"), ServiceLifetime.Singleton);

        services.AddDbContext<Karamchari.HR.Persistence.HRDbContext>(o => InMemory(o, "CertHR"));
        services.AddDbContext<Karamchari.Payroll.Data.PayrollDbContext>(o => InMemory(o, "CertPayroll"));
        services.AddDbContext<Karamchari.TimeAttendance.Persistence.TimeAttendanceDbContext>(o => InMemory(o, "CertTA"));
        services.AddDbContext<Karamchari.PSA.Persistence.PSADbContext>(o => InMemory(o, "CertPSA"));
        services.AddDbContext<Karamchari.Performance.Persistence.PerformanceDbContext>(o => InMemory(o, "CertPerf"));
        services.AddDbContext<Karamchari.Notifications.Persistence.NotificationsDbContext>(o => InMemory(o, "CertNotif"));
        services.AddDbContext<Karamchari.Compensation.Persistence.CompensationDbContext>(o => InMemory(o, "CertComp"));
        services.AddDbContext<Karamchari.Billing.Persistence.BillingDbContext>(o => InMemory(o, "CertBilling"));
        services.AddDbContext<Karamchari.Forecasting.Persistence.ForecastingDbContext>(o => InMemory(o, "CertForecast"));
        services.AddDbContext<Karamchari.Core.Persistence.CoreDbContext>(o => InMemory(o, "CertCore"));
        services.AddDbContext<Karamchari.Workflow.Persistence.WorkflowDbContext>(o => InMemory(o, "CertWorkflow"));
        services.AddDbContext<RecruitmentDbContext>(o => InMemory(o, "CertRecruit"));
        services.AddDbContext<Karamchari.Capability.Persistence.CapabilityDbContext>(o => InMemory(o, "CertCapability"));
        services.AddDbContext<Karamchari.Intelligence.Persistence.IntelligenceDbContext>(o => InMemory(o, "CertIntel"));
        services.AddDbContext<Karamchari.Governance.Persistence.GovernanceDbContext>(o => InMemory(o, "CertGov"));
        services.AddDbContext<Karamchari.FinancialOps.Persistence.FinancialOpsDbContext>(o => InMemory(o, "CertFinOps"));
        services.AddDbContext<Karamchari.Core.Messaging.Outbox.OutboxRelayDbContext>(o => InMemory(o, "CertOutbox"));
    }

    public Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable("Jwt__Secret", "karamchari-api-cert-test-secret-minimum-32-bytes-xxxxx");
        return Task.CompletedTask;
    }
    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    private sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-User-Role", out var role))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var tenantId = Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? "dev";

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "test-user-id"),
                new(ClaimTypes.Name, "test-user"),
                new("tenant_id", tenantId)
            };

            if (role == "Recruiter" || role == "Admin")
            {
                claims.Add(new Claim("permission", "recruitment.read"));
                claims.Add(new Claim("permission", "recruitment.create"));
                claims.Add(new Claim("permission", "recruitment.update"));
                claims.Add(new Claim("permission", "recruitment.interview"));
                claims.Add(new Claim("permission", "recruitment.offer"));
                claims.Add(new Claim("permission", "recruitment.hire"));
            }
            else if (role == "Manager")
            {
                claims.Add(new Claim("permission", "recruitment.read"));
                claims.Add(new Claim("permission", "recruitment.create"));
                claims.Add(new Claim("permission", "recruitment.interview"));
            }

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
