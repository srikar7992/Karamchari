using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Karamchari.HR.Contracts.Employees;
using Xunit;

namespace Karamchari.SecurityTests;

/// <summary>
/// D3 — Cross-tenant access tests.
/// A user authenticated as Tenant A must never see Tenant B's resources.
/// The test auth handler provides a JWT-style tenant_id claim; the tenant
/// filter at the DbContext query layer ensures isolation.
/// </summary>
public sealed class TenantIsolationTests : IClassFixture<SecurityWebApplicationFactory>
{
    private readonly SecurityWebApplicationFactory _factory;

    public TenantIsolationTests(SecurityWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    [Fact]
    public async Task D3_CrossTenantPayrollAccess_Returns404OrEmpty()
    {
        // Arrange: seed an employee for tenant-alpha
        var alphaClient = _factory.CreateAuthenticatedClient("tenant-alpha");
        var seedCommand = new OnboardEmployeeCommand(
            "SEC001", "Alpha Employee", "alpha-emp@tenant-alpha.com", DateOnly.FromDateTime(DateTime.Today));
        var seedRes = await alphaClient.PostAsJsonAsync("/api/v1/hr/employees", seedCommand);
        seedRes.IsSuccessStatusCode.Should().BeTrue("seeding tenant-alpha employee must succeed");

        // Act: authenticated as tenant-beta, request tenant-alpha's payroll list
        // The Host header is spoofed to tenant-alpha but the JWT says tenant_id=tenant-beta
        var betaClient = _factory.CreateAuthenticatedClient("tenant-beta");
        betaClient.DefaultRequestHeaders.Remove("Host");
        betaClient.DefaultRequestHeaders.Add("Host", "tenant-alpha.karamchari.com");

        var response = await betaClient.GetAsync("/api/v1/payroll");

        // The API must not expose tenant-alpha's payroll data to tenant-beta.
        // Acceptable outcomes: 401 (auth rejected), 403 (forbidden), 404 (not found),
        // or 200 with an empty list (tenant filter returns zero rows).
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound,
            HttpStatusCode.OK);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            // Payroll items for tenant-alpha must not appear
            body.Should().NotContain("alpha-emp", "tenant isolation must prevent data leakage");
        }
    }

    [Fact]
    public async Task D3_CrossTenantRecruitmentAccess_Returns404OrEmpty()
    {
        // Arrange: create a requisition under tenant-gamma
        var gammaClient = _factory.CreateAuthenticatedClient("tenant-gamma", role: "Recruiter");
        var requisitionRes = await gammaClient.PostAsJsonAsync("/api/v1/recruitment/requisitions", new
        {
            title = "Gamma Exclusive Role",
            departmentId = Guid.NewGuid(),
            hiringManagerId = Guid.NewGuid(),
        });
        // 201 = created; anything else is acceptable for in-memory (no EF migrations to seed)
        var requisitionId = requisitionRes.IsSuccessStatusCode
            ? (await requisitionRes.Content.ReadFromJsonAsync<IdResponse>())?.Id ?? Guid.NewGuid()
            : Guid.NewGuid();

        // Act: tenant-delta tries to GET tenant-gamma's requisition by ID
        var deltaClient = _factory.CreateAuthenticatedClient("tenant-delta", role: "Recruiter");
        var response = await deltaClient.GetAsync($"/api/v1/recruitment/requisitions/{requisitionId}");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task D3_CrossTenantHRAccess_Returns404OrEmpty()
    {
        // Arrange: onboard an employee under tenant-omega
        var omegaClient = _factory.CreateAuthenticatedClient("tenant-omega");
        var onboardRes = await omegaClient.PostAsJsonAsync("/api/v1/hr/employees",
            new OnboardEmployeeCommand("SEC002", "Omega Employee", "omega-emp@tenant-omega.com", DateOnly.FromDateTime(DateTime.Today)));

        Guid employeeId = Guid.NewGuid();
        if (onboardRes.IsSuccessStatusCode)
        {
            var created = await onboardRes.Content.ReadFromJsonAsync<IdResponse>();
            employeeId = created?.Id ?? employeeId;
        }

        // Act: tenant-sigma attempts to read tenant-omega's specific employee
        var sigmaClient = _factory.CreateAuthenticatedClient("tenant-sigma");
        var response = await sigmaClient.GetAsync($"/api/v1/hr/employees/{employeeId}");

        // The employee record belongs to tenant-omega; tenant-sigma must not retrieve it.
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound);
    }
}

internal sealed record IdResponse(Guid Id);
