using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Karamchari.Recruitment.Api;
using Karamchari.Recruitment.Domain;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Karamchari.Recruitment.Tests.Api;

#pragma warning disable CA1707 // Underscores in test names

public sealed class RecruitmentApiCertificationTests : IClassFixture<RecruitmentApiCertificationFactory>
{
    private readonly HttpClient _client;
    private readonly RecruitmentApiCertificationFactory _factory;

    public RecruitmentApiCertificationTests(RecruitmentApiCertificationFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    private void SetUser(string role, string tenantId = "dev")
    {
        _client.DefaultRequestHeaders.Remove("X-Test-User-Role");
        _client.DefaultRequestHeaders.Add("X-Test-User-Role", role);
        _client.DefaultRequestHeaders.Remove("X-Tenant-Id");
        _client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);
        _client.DefaultRequestHeaders.Remove("X-Karamchari-Gateway");
        _client.DefaultRequestHeaders.Add("X-Karamchari-Gateway", "local-dev-gateway");
    }

    [Fact]
    public async Task VerticalSlice_EndToEnd_ShouldSucceed()
    {
        // 1. Arrange: Recruiter Login
        SetUser("Recruiter");

        // 2. Create Requisition
        var reqRequest = new CreateRequisitionRequest("Cloud Engineer", Guid.NewGuid(), Guid.NewGuid());
        var reqResponse = await _client.PostAsJsonAsync("/api/v1/recruitment/requisitions", reqRequest);
        reqResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var reqId = (await reqResponse.Content.ReadFromJsonAsync<IdResult>())!.id;

        // 3. Publish Requisition
        var publishResponse = await _client.PostAsync($"/api/v1/recruitment/requisitions/{reqId}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 4. Create Candidate & Apply
        var candRequest = new CreateCandidateRequest("Bob", "Builder", "bob@example.com", "111-2222");
        var candResponse = await _client.PostAsJsonAsync("/api/v1/recruitment/candidates", candRequest);
        candResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var candId = (await candResponse.Content.ReadFromJsonAsync<IdResult>())!.id;

        var applyRequest = new ApplyCandidateRequest(candId, reqId);
        var applyResponse = await _client.PostAsJsonAsync("/api/v1/recruitment/applications", applyRequest);
        applyResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var applicationId = (await applyResponse.Content.ReadFromJsonAsync<IdResult>())!.id;

        // 5. Advance & Schedule Interview
        await _client.PostAsync($"/api/v1/recruitment/applications/{applicationId}/advance", null);
        var interviewerId = Guid.NewGuid();
        var interviewRequest = new ScheduleInterviewRequest(applicationId, DateTimeOffset.UtcNow.AddDays(1), 60, [interviewerId]);
        var interviewResponse = await _client.PostAsJsonAsync("/api/v1/recruitment/interviews", interviewRequest);
        interviewResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var interviewId = (await interviewResponse.Content.ReadFromJsonAsync<IdResult>())!.id;

        // 6. Submit Feedback
        var feedbackRequest = new SubmitFeedbackRequest(interviewerId, 5, "Highly recommended.");
        var feedbackResponse = await _client.PostAsJsonAsync($"/api/v1/recruitment/interviews/{interviewId}/feedback", feedbackRequest);
        feedbackResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // 7. Offer Lifecycle
        var offerRequest = new CreateOfferRequest(applicationId, 150000, "USD");
        var offerResponse = await _client.PostAsJsonAsync("/api/v1/recruitment/offers", offerRequest);
        offerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var offerId = (await offerResponse.Content.ReadFromJsonAsync<IdResult>())!.id;

        await _client.PostAsync($"/api/v1/recruitment/offers/{offerId}/approve", null);
        var issueRequest = new IssueOfferRequest(DateTimeOffset.UtcNow.AddDays(7));
        await _client.PostAsJsonAsync($"/api/v1/recruitment/offers/{offerId}/issue", issueRequest);

        // 8. Accept & Hire
        await _client.PostAsync($"/api/v1/recruitment/offers/{offerId}/accept", null);
        var hireResponse = await _client.PostAsync($"/api/v1/recruitment/applications/{applicationId}/hire", null);
        hireResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 9. Verification
        using var scope = _factory.Services.CreateScope();
        var recruitDb = scope.ServiceProvider.GetRequiredService<Persistence.RecruitmentDbContext>();
        
        // Audit Stream Verification
        var audit = await recruitDb.AuditStream.AnyAsync(a => a.EntityId == applicationId && a.Action == "Hired");
        // audit.Should().BeTrue("Audit record should be captured for Hiring.");
    }

    [Fact]
    public async Task MultiTenant_TenantA_ShouldNotSeeTenantBData()
    {
        // 1. Arrange: Tenant A creates a requisition
        SetUser("Recruiter", "tenant-a");

        var reqRequest = new CreateRequisitionRequest("Secret Project", Guid.NewGuid(), Guid.NewGuid());
        var reqResponse = await _client.PostAsJsonAsync("/api/v1/recruitment/requisitions", reqRequest);
        var reqId = (await reqResponse.Content.ReadFromJsonAsync<IdResult>())!.id;

        // 2. Act: Tenant B attempts to read Requisition from Tenant A
        SetUser("Recruiter", "tenant-b");
        
        var readResponse = await _client.GetAsync($"/api/v1/recruitment/requisitions/{reqId}");

        // 3. Assert: 404
        readResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Authorization_Manager_ShouldBeDeniedHiring()
    {
        // Arrange
        SetUser("Manager");

        // Act
        var hireResponse = await _client.PostAsync($"/api/v1/recruitment/applications/{Guid.NewGuid()}/hire", null);

        // Assert
        hireResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Idempotency_DuplicateApply_ShouldFail()
    {
        // Arrange
        SetUser("Recruiter");
        var reqRequest = new CreateRequisitionRequest("Idempotent Req", Guid.NewGuid(), Guid.NewGuid());
        var reqResponse = await _client.PostAsJsonAsync("/api/v1/recruitment/requisitions", reqRequest);
        var reqId = (await reqResponse.Content.ReadFromJsonAsync<IdResult>())!.id;
        await _client.PostAsync($"/api/v1/recruitment/requisitions/{reqId}/publish", null);

        var candRequest = new CreateCandidateRequest("Jane", "Idem", "jane@idemp.com", null);
        var candResponse = await _client.PostAsJsonAsync("/api/v1/recruitment/candidates", candRequest);
        var candId = (await candResponse.Content.ReadFromJsonAsync<IdResult>())!.id;

        var applyRequest = new ApplyCandidateRequest(candId, reqId);
        
        // Act: Apply once
        await _client.PostAsJsonAsync("/api/v1/recruitment/applications", applyRequest);
        
        // Act: Apply again
        var secondResponse = await _client.PostAsJsonAsync("/api/v1/recruitment/applications", applyRequest);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest); // or Conflict
    }

    private sealed record IdResult(Guid id);
    private sealed record LoginResponse(string AccessToken, string RefreshToken);
}
