using FluentAssertions;
using Karamchari.Identity.Contracts;
using Karamchari.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
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
                    typeof(Karamchari.Core.Persistence.CoreDbContext)
                };

                foreach (var dbContextType in dbContextTypes)
                {
                    // Remove DbContextOptions
                    var optionsType = typeof(DbContextOptions<>).MakeGenericType(dbContextType);
                    var descriptors = services.Where(d => d.ServiceType == optionsType).ToList();
                    foreach (var d in descriptors) services.Remove(d);

                    // Remove DbContext itself
                    var dbDescriptors = services.Where(d => d.ServiceType == dbContextType).ToList();
                    foreach (var d in dbDescriptors) services.Remove(d);
                }

                // Add In-Memory DBs
                services.AddDbContext<IdentityDbContext>(o => o.UseInMemoryDatabase("IdentityTestDb"));
                services.AddDbContext<Karamchari.Payroll.Data.PayrollDbContext>(o => o.UseInMemoryDatabase("PayrollTestDb"));
                services.AddDbContext<Karamchari.HR.Persistence.HRDbContext>(o => o.UseInMemoryDatabase("HRTestDb"));
                services.AddDbContext<Karamchari.TimeAttendance.Persistence.TimeAttendanceDbContext>(o => o.UseInMemoryDatabase("TimeAttendanceTestDb"));
                services.AddDbContext<Karamchari.PSA.Persistence.PSADbContext>(o => o.UseInMemoryDatabase("PSATestDb"));
                services.AddDbContext<Karamchari.Performance.Persistence.PerformanceDbContext>(o => o.UseInMemoryDatabase("PerformanceTestDb"));
                services.AddDbContext<Karamchari.Notifications.Persistence.NotificationsDbContext>(o => o.UseInMemoryDatabase("NotificationsTestDb"));
                services.AddDbContext<Karamchari.Compensation.Persistence.CompensationDbContext>(o => o.UseInMemoryDatabase("CompensationTestDb"));
                services.AddDbContext<Karamchari.Billing.Persistence.BillingDbContext>(o => o.UseInMemoryDatabase("BillingTestDb"));
                services.AddDbContext<Karamchari.Forecasting.Persistence.ForecastingDbContext>(o => o.UseInMemoryDatabase("ForecastingTestDb"));
                services.AddDbContext<Karamchari.Core.Persistence.CoreDbContext>(o => o.UseInMemoryDatabase("CoreTestDb"));
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
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
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
