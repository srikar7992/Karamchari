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
        TestAuthHandler.TenantId = TestAuthHandler.DefaultTenantId;
        TestAuthHandler.ShouldAuthenticate = true;

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
                services.AddDbContext<Karamchari.Intelligence.Persistence.IntelligenceDbContext>(o => ConfigureInMemory(o, "IntelligenceEmployeeTestDb", inMemoryProvider));
                services.AddDbContext<Karamchari.Governance.Persistence.GovernanceDbContext>(o => ConfigureInMemory(o, "GovernanceEmployeeTestDb", inMemoryProvider));

                // Configure Test Authentication Scheme
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

                services.PostConfigure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                });

                // Mock IPublishEndpoint to bypass MassTransit reflection
                services.RemoveAll<MassTransit.IPublishEndpoint>();
                services.AddScoped(sp => new Moq.Mock<MassTransit.IPublishEndpoint>().Object);

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
        var command = new OnboardEmployeeCommand("EMP002", "Jane Smith", "jane.smith@acme.com", DateOnly.FromDateTime(DateTime.Today));
        
        var onboardResponse = await client.PostAsJsonAsync("/api/v1/hr/employees", command);
        onboardResponse.EnsureSuccessStatusCode();
        var created = await onboardResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        // Act
        var getResponse = await client.GetAsync($"/api/v1/hr/employees/{created!.Id}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var employee = await getResponse.Content.ReadFromJsonAsync<EmployeeDto>();
        employee.Should().NotBeNull();
        employee!.EmployeeNumber.Should().Be("EMP002");
        employee.LegalName.Should().Be("Jane Smith");
        employee.WorkEmail.Should().Be("jane.smith@acme.com");
    }

    [Fact]
    public async Task GetEmployees_ShouldReturnAllEmployeesForTenant()
    {
        // Arrange
        var client = CreateTenantClient("acme");
        var command = new OnboardEmployeeCommand("EMP003", "Alice Johnson", "alice.johnson@acme.com", DateOnly.FromDateTime(DateTime.Today));
        await client.PostAsJsonAsync("/api/v1/hr/employees", command);

        // Act
        var response = await client.GetAsync("/api/v1/hr/employees");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var employees = await response.Content.ReadFromJsonAsync<List<EmployeeDto>>();
        employees.Should().NotBeEmpty();
        employees.Should().Contain(e => e.EmployeeNumber == "EMP003");
    }

    [Fact]
    public async Task UpdateEmployee_ShouldModifyDetails_WhenPayloadIsValid()
    {
        // Arrange
        var client = CreateTenantClient("acme");
        var command = new OnboardEmployeeCommand("EMP004", "Bob Brown", "bob.brown@acme.com", DateOnly.FromDateTime(DateTime.Today));
        var onboardResponse = await client.PostAsJsonAsync("/api/v1/hr/employees", command);
        var created = await onboardResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var updateCommand = new UpdateEmployeeCommand("Bob Changed", "bob.changed@acme.com");

        // Act
        var updateResponse = await client.PutAsJsonAsync($"/api/v1/hr/employees/{created!.Id}", updateCommand);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert
        var getResponse = await client.GetAsync($"/api/v1/hr/employees/{created.Id}");
        var employee = await getResponse.Content.ReadFromJsonAsync<EmployeeDto>();
        employee.Should().NotBeNull();
        employee!.LegalName.Should().Be("Bob Changed");
        employee.WorkEmail.Should().Be("bob.changed@acme.com");
    }

    [Fact]
    public async Task DeleteEmployee_ShouldTerminateEmployee()
    {
        // Arrange
        var client = CreateTenantClient("acme");
        var command = new OnboardEmployeeCommand("EMP005", "Eve White", "eve.white@acme.com", DateOnly.FromDateTime(DateTime.Today));
        var onboardResponse = await client.PostAsJsonAsync("/api/v1/hr/employees", command);
        onboardResponse.EnsureSuccessStatusCode();
        var created = await onboardResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        // Act
        var deleteResponse = await client.DeleteAsync($"/api/v1/hr/employees/{created!.Id}");
        var body = await deleteResponse.Content.ReadAsStringAsync();

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK, $"Response body was: {body}");

        // Assert
        var getResponse = await client.GetAsync($"/api/v1/hr/employees/{created.Id}");
        var employee = await getResponse.Content.ReadFromJsonAsync<EmployeeDto>();
        employee.Should().NotBeNull();
        employee!.Status.Should().Be("Terminated");
    }

    [Fact]
    public async Task CrossTenantAccess_ShouldBeDenied()
    {
        // Arrange
        // Onboard as acme tenant
        TestAuthHandler.TenantId = "acme";
        var clientAcme = CreateTenantClient("acme");
        var command = new OnboardEmployeeCommand("EMP006", "Mallory Green", "mallory@acme.com", DateOnly.FromDateTime(DateTime.Today));
        var onboardResponse = await clientAcme.PostAsJsonAsync("/api/v1/hr/employees", command);
        var created = await onboardResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        // Switch logged-in user to globex tenant
        TestAuthHandler.TenantId = "globex";
        var clientGlobex = CreateTenantClient("globex");

        // Act
        var getResponse = await clientGlobex.GetAsync($"/api/v1/hr/employees/{created!.Id}");

        // Assert
        // In a tenant-isolated environment, reading foreign tenant data returns NotFound
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnauthorizedAccess_ShouldBeDenied()
    {
        // Arrange
        var client = CreateTenantClient("acme");
        TestAuthHandler.ShouldAuthenticate = false;

        // Act
        var response = await client.GetAsync("/api/v1/hr/employees");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string DefaultTenantId = "acme";
    public static string TenantId { get; set; } = DefaultTenantId;
    public static bool ShouldAuthenticate { get; set; } = true;

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!ShouldAuthenticate)
        {
            return Task.FromResult(AuthenticateResult.Fail("Test authentication disabled"));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim("tenant_id", TenantId),
            new Claim("permission", "hr:write")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class CreatedResponse
{
    public Guid Id { get; set; }
}

public class TestDomainEventDispatcher : Karamchari.Core.Messaging.IDomainEventDispatcher
{
    public Task DispatchAsync(IReadOnlyCollection<Karamchari.Core.Domain.Primitives.IDomainEvent> events, System.Threading.CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
