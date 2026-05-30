using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using FluentAssertions;
using Karamchari.HR.Contracts.Employees;
using Karamchari.HR.Persistence;
using Karamchari.Core.Multitenancy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Karamchari.Identity.IntegrationTests;

public class EmployeeApiTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public EmployeeApiTests(WebApplicationFactory<Program> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        
        // Reset the test auth handler state for each test run
        TestAuthHandler.Reset();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext configurations to avoid provider conflicts
                var dbContextTypes = new[]
                {
                    typeof(Identity.Infrastructure.Persistence.IdentityDbContext),
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
                services.RemoveAll<Microsoft.Extensions.Hosting.IHostedService>();

                // Configure InMemory DBs with a shared provider
                var inMemoryProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                services.AddDbContext<Identity.Infrastructure.Persistence.IdentityDbContext>(o => ConfigureInMemory(o, "IdentityEmployeeTestDb", inMemoryProvider), ServiceLifetime.Scoped, ServiceLifetime.Singleton);
                services.AddDbContextFactory<Identity.Infrastructure.Persistence.IdentityDbContext>(o => ConfigureInMemory(o, "IdentityEmployeeTestDb", inMemoryProvider), ServiceLifetime.Singleton);
                services.AddDbContext<Karamchari.Payroll.Data.PayrollDbContext>(o => ConfigureInMemory(o, "PayrollEmployeeTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.HR.Persistence.HRDbContext>(o => ConfigureInMemory(o, "HREmployeeTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.TimeAttendance.Persistence.TimeAttendanceDbContext>(o => ConfigureInMemory(o, "TimeAttendanceEmployeeTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.PSA.Persistence.PSADbContext>(o => ConfigureInMemory(o, "PSAEmployeeTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.Performance.Persistence.PerformanceDbContext>(o => ConfigureInMemory(o, "PerformanceEmployeeTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.Notifications.Persistence.NotificationsDbContext>(o => ConfigureInMemory(o, "NotificationsEmployeeTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.Compensation.Persistence.CompensationDbContext>(o => ConfigureInMemory(o, "CompensationEmployeeTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.Billing.Persistence.BillingDbContext>(o => ConfigureInMemory(o, "BillingEmployeeTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.Forecasting.Persistence.ForecastingDbContext>(o => ConfigureInMemory(o, "ForecastingEmployeeTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.Core.Persistence.CoreDbContext>(o => ConfigureInMemory(o, "CoreEmployeeTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.Recruitment.Persistence.RecruitmentDbContext>(o => ConfigureInMemory(o, "RecruitmentEmployeeTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.Capability.Persistence.CapabilityDbContext>(o => ConfigureInMemory(o, "CapabilityEmployeeTestDb", inMemoryProvider));
                services.AddDbContext<Intelligence.Persistence.IntelligenceDbContext>(o => ConfigureInMemory(o, "IntelligenceEmployeeTestDb", inMemoryProvider));
                services.AddDbContext<Governance.Persistence.GovernanceDbContext>(o => ConfigureInMemory(o, "GovernanceEmployeeTestDb", inMemoryProvider));

                // Configure Test Authentication Scheme
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

                services.PostConfigure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                });

                // Replace DomainEventDispatcher with TestDomainEventDispatcher to avoid reflection on generic envelopes
                services.RemoveAll<Karamchari.Core.Messaging.IDomainEventDispatcher>();
                services.AddScoped<Karamchari.Core.Messaging.IDomainEventDispatcher, TestDomainEventDispatcher>();
            });
        });
    }

    private static void ConfigureInMemory(DbContextOptionsBuilder options, string databaseName, IServiceProvider provider)
    {
        options.UseInMemoryDatabase(databaseName);
        options.UseInternalServiceProvider(provider);
    }

    private HttpClient CreateTenantClient(string tenantId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Host = $"{tenantId}.karamchari.com";
        return client;
    }

    [Fact]
    public async Task OnboardEmployee_ShouldCreateEmployee_WhenPayloadIsValid()
    {
        // Arrange
        var client = CreateTenantClient("acme");
        var command = new OnboardEmployeeCommand("EMP001", "John Doe", "john.doe@acme.com", DateOnly.FromDateTime(DateTime.Today));

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/hr/employees", command);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created, $"Response body was: {body}");
        var result = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetEmployee_ShouldReturnEmployee_WhenEmployeeExists()
    {
        // Arrange
        var client = CreateTenantClient("acme");
        var onboardCommand = new OnboardEmployeeCommand("EMP002", "Jane Smith", "jane.smith@acme.com", DateOnly.FromDateTime(DateTime.Today));
        var onboardResponse = await client.PostAsJsonAsync("/api/v1/hr/employees", onboardCommand);
        var onboardResult = await onboardResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        var employeeId = onboardResult!.Id;

        // Act
        var response = await client.GetAsync($"/api/v1/hr/employees/{employeeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var employee = await response.Content.ReadFromJsonAsync<EmployeeDto>();
        employee.Should().NotBeNull();
        employee!.Id.Should().Be(employeeId);
        employee.LegalName.Should().Be("Jane Smith");
    }

    [Fact]
    public async Task GetEmployee_ShouldReturnNotFound_WhenEmployeeDoesNotExist()
    {
        // Arrange
        var client = CreateTenantClient("acme");
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1/hr/employees/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateEmployee_ShouldModifyEmployee_WhenEmployeeExists()
    {
        // Arrange
        var client = CreateTenantClient("acme");
        var onboardCommand = new OnboardEmployeeCommand("EMP003", "Alice Johnson", "alice.johnson@acme.com", DateOnly.FromDateTime(DateTime.Today));
        var onboardResponse = await client.PostAsJsonAsync("/api/v1/hr/employees", onboardCommand);
        var onboardResult = await onboardResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        var employeeId = onboardResult!.Id;

        var updateCommand = new UpdateEmployeeCommand("Alice J. Smith", "alice.smith@acme.com");

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1/hr/employees/{employeeId}", updateCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var getResponse = await client.GetAsync($"/api/v1/hr/employees/{employeeId}");
        var updatedEmployee = await getResponse.Content.ReadFromJsonAsync<EmployeeDto>();
        updatedEmployee!.LegalName.Should().Be("Alice J. Smith");
        updatedEmployee.WorkEmail.Should().Be("alice.smith@acme.com");
    }

    [Fact]
    public async Task DeleteEmployee_ShouldTerminateEmployee_WhenEmployeeExists()
    {
        // Arrange
        var client = CreateTenantClient("acme");
        var onboardCommand = new OnboardEmployeeCommand("EMP004", "Bob Brown", "bob.brown@acme.com", DateOnly.FromDateTime(DateTime.Today));
        var onboardResponse = await client.PostAsJsonAsync("/api/v1/hr/employees", onboardCommand);
        var onboardResult = await onboardResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        var employeeId = onboardResult!.Id;

        // Act
        var response = await client.DeleteAsync($"/api/v1/hr/employees/{employeeId}");

        // Assert — DELETE is a soft-delete (Employee.Terminate): the record is RETAINED for
        // audit/compliance and remains retrievable with Status = Terminated. (Hard-delete of an
        // employee is intentionally not supported in this HR domain.)
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await client.GetAsync($"/api/v1/hr/employees/{employeeId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var terminated = await getResponse.Content.ReadFromJsonAsync<EmployeeDto>();
        terminated!.Status.Should().Be("Terminated");
    }

    [Fact]
    public async Task GetEmployees_ShouldReturnOnlyTenantEmployees()
    {
        // Arrange
        var acmeClient = CreateTenantClient("acme");
        var globexClient = CreateTenantClient("globex");

        await acmeClient.PostAsJsonAsync("/api/v1/hr/employees", new OnboardEmployeeCommand("EMP005", "Eve White", "eve.white@acme.com", DateOnly.FromDateTime(DateTime.Today)));
        await globexClient.PostAsJsonAsync("/api/v1/hr/employees", new OnboardEmployeeCommand("EMP006", "Mallory Green", "mallory@acme.com", DateOnly.FromDateTime(DateTime.Today)));

        // Act
        var acmeResponse = await acmeClient.GetAsync("/api/v1/hr/employees");
        var globexResponse = await globexClient.GetAsync("/api/v1/hr/employees");

        // Assert
        var acmeEmployees = await acmeResponse.Content.ReadFromJsonAsync<List<EmployeeDto>>();
        var globexEmployees = await globexResponse.Content.ReadFromJsonAsync<List<EmployeeDto>>();

        acmeEmployees.Should().ContainSingle(e => e.LegalName == "Eve White");
        acmeEmployees.Should().NotContain(e => e.LegalName == "Mallory Green");

        globexEmployees.Should().ContainSingle(e => e.LegalName == "Mallory Green");
        globexEmployees.Should().NotContain(e => e.LegalName == "Eve White");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

public record CreatedResponse(Guid Id);

public record EmployeeDto(
    Guid Id,
    string EmployeeNumber,
    string LegalName,
    string WorkEmail,
    DateOnly HiredOn,
    string Status,
    string TimeZoneId);

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private static string _defaultTenantId = "acme";
    private static string _tenantId = _defaultTenantId;
    private static bool _shouldAuthenticate = true;

    public static void Reset()
    {
        _tenantId = _defaultTenantId;
        _shouldAuthenticate = true;
    }

    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!_shouldAuthenticate)
        {
            return Task.FromResult(AuthenticateResult.Fail("Test authentication disabled"));
        }

        // Resolve the tenant per-request from the Host subdomain so that CreateTenantClient(tenant)
        // genuinely authenticates as that tenant. Relying on the static _tenantId field made
        // multi-tenant-in-one-test scenarios resolve every client to the same tenant. The Host
        // (e.g. "acme.karamchari.com") is the per-client tenant signal; fall back to _tenantId.
        var host = Request.Host.Host;
        var tenant = _tenantId;
        if (!string.IsNullOrWhiteSpace(host) && host.Contains('.'))
        {
            var label = host.Split('.')[0];
            if (!string.IsNullOrWhiteSpace(label))
            {
                tenant = label.ToLowerInvariant();
            }
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("tenant_id", tenant)
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class TestDomainEventDispatcher : Karamchari.Core.Messaging.IDomainEventDispatcher
{
    public Task DispatchAsync(IReadOnlyCollection<Karamchari.Core.Domain.Primitives.IDomainEvent> events, System.Threading.CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
