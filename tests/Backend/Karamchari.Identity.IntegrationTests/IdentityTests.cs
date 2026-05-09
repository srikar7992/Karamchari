using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Karamchari.Identity.Contracts;
using Karamchari.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Karamchari.Identity.IntegrationTests;

/// <summary>
/// Integration tests for the Identity system.
/// </summary>
public class IdentityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Initializes a new instance of the <see cref="IdentityTests"/> class.</summary>
    public IdentityTests(WebApplicationFactory<Program> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                // Remove all DB registrations and related services to avoid provider conflicts
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
                    typeof(Karamchari.Governance.Persistence.GovernanceDbContext)
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

                // Add isolated In-Memory DBs. UseInternalServiceProvider prevents EF provider
                // collisions with SQL Server registrations retained by MassTransit internals.
                services.AddDbContext<IdentityDbContext>(o => ConfigureInMemory(o, "IdentityTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.Payroll.Data.PayrollDbContext>(o => ConfigureInMemory(o, "PayrollTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.HR.Persistence.HRDbContext>(o => ConfigureInMemory(o, "HRTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.TimeAttendance.Persistence.TimeAttendanceDbContext>(o => ConfigureInMemory(o, "TimeAttendanceTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.PSA.Persistence.PSADbContext>(o => ConfigureInMemory(o, "PSATestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.Performance.Persistence.PerformanceDbContext>(o => ConfigureInMemory(o, "PerformanceTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.Notifications.Persistence.NotificationsDbContext>(o => ConfigureInMemory(o, "NotificationsTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.Compensation.Persistence.CompensationDbContext>(o => ConfigureInMemory(o, "CompensationTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.Billing.Persistence.BillingDbContext>(o => ConfigureInMemory(o, "BillingTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.Forecasting.Persistence.ForecastingDbContext>(o => ConfigureInMemory(o, "ForecastingTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.Core.Persistence.CoreDbContext>(o => ConfigureInMemory(o, "CoreTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.Recruitment.Persistence.RecruitmentDbContext>(o => ConfigureInMemory(o, "RecruitmentTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.Capability.Persistence.CapabilityDbContext>(o => ConfigureInMemory(o, "CapabilityTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.Intelligence.Persistence.IntelligenceDbContext>(o => ConfigureInMemory(o, "IntelligenceTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.Governance.Persistence.GovernanceDbContext>(o => ConfigureInMemory(o, "GovernanceTestDb", inMemoryProvider));
                services.AddScoped<DbContext>(sp => sp.GetRequiredService<Karamchari.Payroll.Data.PayrollDbContext>());
            });
        });
    }

    /// <summary>Tests that registration returns OK for valid requests.</summary>
    [Fact]
    public async Task Register_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new RegisterRequest("test@karamchari.com", "Password123!", "Test User", "tenant_oakridge");

        // Act
        var response = await client.PostAsJsonAsync("/api/identity/register", request);

        // Assert
        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);
    }

    /// <summary>Tests that login returns Unauthorized for invalid credentials.</summary>
    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new LoginRequest("nonexistent@karamchari.com", "WrongPassword!");

        // Act
        var response = await client.PostAsJsonAsync("/api/identity/login", request);

        // Assert
        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, responseBody);
    }

    private static void ConfigureInMemory(DbContextOptionsBuilder options, string databaseName, IServiceProvider provider)
    {
        options.UseInMemoryDatabase(databaseName);
        options.UseInternalServiceProvider(provider);
    }
}
