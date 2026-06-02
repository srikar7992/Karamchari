using System.Text;
using System.Text.Json;
using NBomber.CSharp;
using Xunit;

namespace Karamchari.PerformanceTests;

/// <summary>
/// P2 — Full recruitment journey load test.
/// Each virtual user executes the complete hiring pipeline in sequence.
/// 100 concurrent users for 5 minutes.
///
/// Run manually against a live environment:
///   KARAMCHARI_BASE_URL=http://localhost:5000 dotnet test --filter RecruitmentJourneyLoadTest
/// </summary>
public sealed class RecruitmentJourneyLoadTest
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("KARAMCHARI_BASE_URL") ?? "http://localhost:5000";

    private static readonly string TenantHost =
        Environment.GetEnvironmentVariable("KARAMCHARI_TENANT_HOST") ?? "acme.karamchari.com";

    private static readonly string RecruiterEmail =
        Environment.GetEnvironmentVariable("KARAMCHARI_RECRUITER_EMAIL") ?? "recruiter@acme.karamchari.com";

    private static readonly string RecruiterPassword =
        Environment.GetEnvironmentVariable("KARAMCHARI_RECRUITER_PASSWORD") ?? "Recruiter@123!";

    private static readonly Guid DepartmentId = new("00000000-0000-0000-0000-000000000001");
    private static readonly Guid ManagerId = new("00000000-0000-0000-0000-000000000002");

    [Fact(Skip = "Run manually against a live environment")]
    public async Task Run_RecruitmentJourney_P2()
    {
        var token = await ObtainTokenAsync();

        using var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        httpClient.DefaultRequestHeaders.Add("Host", TenantHost);
        if (!string.IsNullOrEmpty(token))
        {
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        var scenario = Scenario.Create("P2_RecruitmentJourney", async context =>
        {
            // Step 1: Create requisition
            var reqStep = await Step.Run<Guid>("create_requisition", context, async () =>
            {
                var body = Serialize(new
                {
                    title = $"Load Role VU{context.InvocationNumber}",
                    departmentId = DepartmentId,
                    hiringManagerId = ManagerId,
                });
                var res = await httpClient.PostAsync("/api/v1/recruitment/requisitions", body, CancellationToken.None);
                return res.IsSuccessStatusCode
                    ? Response.Ok(payload: await ParseIdAsync(res), statusCode: ((int)res.StatusCode).ToString())
                    : Response.Fail<Guid>(message: "create_requisition failed");
            });

            if (reqStep.IsError) return Response.Fail(message: "create_requisition failed");
            var requisitionId = reqStep.Payload.Value;

            // Step 2: Create candidate
            var candStep = await Step.Run<Guid>("create_candidate", context, async () =>
            {
                var body = Serialize(new
                {
                    firstName = $"Cand{context.InvocationNumber}",
                    lastName = "LoadTest",
                    email = $"cand-{context.InvocationNumber}-{Guid.NewGuid():N}@loadtest.example.com",
                    phoneNumber = (string?)null,
                });
                var res = await httpClient.PostAsync("/api/v1/recruitment/candidates", body, CancellationToken.None);
                return res.IsSuccessStatusCode
                    ? Response.Ok(payload: await ParseIdAsync(res), statusCode: ((int)res.StatusCode).ToString())
                    : Response.Fail<Guid>(message: "create_candidate failed");
            });

            if (candStep.IsError) return Response.Fail(message: "create_candidate failed");
            var candidateId = candStep.Payload.Value;

            // Step 3: Apply
            var appStep = await Step.Run<Guid>("apply_candidate", context, async () =>
            {
                var body = Serialize(new { candidateId, requisitionId });
                var res = await httpClient.PostAsync("/api/v1/recruitment/applications", body, CancellationToken.None);
                return res.IsSuccessStatusCode
                    ? Response.Ok(payload: await ParseIdAsync(res), statusCode: ((int)res.StatusCode).ToString())
                    : Response.Fail<Guid>(message: "apply_candidate failed");
            });

            if (appStep.IsError) return Response.Fail(message: "apply_candidate failed");
            var applicationId = appStep.Payload.Value;

            // Step 4: Advance application
            await Step.Run("advance_application", context, async () =>
            {
                var res = await httpClient.PostAsync(
                    $"/api/v1/recruitment/applications/{applicationId}/advance",
                    Serialize(new { }),
                    CancellationToken.None);
                return res.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail(message: "advance_application failed");
            });

            // Step 5: Schedule interview
            await Step.Run("schedule_interview", context, async () =>
            {
                var body = Serialize(new
                {
                    applicationId,
                    scheduledAt = DateTimeOffset.UtcNow.AddDays(1),
                    durationMinutes = 60,
                    interviewerIds = new[] { ManagerId },
                });
                var res = await httpClient.PostAsync("/api/v1/recruitment/interviews", body, CancellationToken.None);
                return res.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail(message: "schedule_interview failed");
            });

            // Step 6: Create offer
            var offerStep = await Step.Run<Guid>("create_offer", context, async () =>
            {
                var body = Serialize(new { applicationId, baseSalary = 80000m, currency = "INR" });
                var res = await httpClient.PostAsync("/api/v1/recruitment/offers", body, CancellationToken.None);
                return res.IsSuccessStatusCode
                    ? Response.Ok(payload: await ParseIdAsync(res), statusCode: ((int)res.StatusCode).ToString())
                    : Response.Fail<Guid>(message: "create_offer failed");
            });

            if (offerStep.IsError) return Response.Fail(message: "create_offer failed");
            var offerId = offerStep.Payload.Value;

            // Step 7: Approve offer
            await Step.Run("approve_offer", context, async () =>
            {
                var res = await httpClient.PostAsync(
                    $"/api/v1/recruitment/offers/{offerId}/approve",
                    Serialize(new { }),
                    CancellationToken.None);
                return res.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail(message: "approve_offer failed");
            });

            // Step 8: Issue offer
            await Step.Run("issue_offer", context, async () =>
            {
                var body = Serialize(new { expiresAt = DateTimeOffset.UtcNow.AddDays(7) });
                var res = await httpClient.PostAsync(
                    $"/api/v1/recruitment/offers/{offerId}/issue",
                    body,
                    CancellationToken.None);
                return res.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail(message: "issue_offer failed");
            });

            // Step 9: Accept offer
            await Step.Run("accept_offer", context, async () =>
            {
                var res = await httpClient.PostAsync(
                    $"/api/v1/recruitment/offers/{offerId}/accept",
                    Serialize(new { }),
                    CancellationToken.None);
                return res.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail(message: "accept_offer failed");
            });

            // Step 10: Hire candidate
            return await Step.Run("hire_candidate", context, async () =>
            {
                var res = await httpClient.PostAsync(
                    $"/api/v1/recruitment/applications/{applicationId}/hire",
                    Serialize(new { }),
                    CancellationToken.None);
                return res.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail(message: "hire_candidate failed");
            });
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(15))
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 100, during: TimeSpan.FromMinutes(5)));

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFileName("P2_RecruitmentJourney_Report")
            .WithReportFolder("reports/P2_RecruitmentJourney")
            .Run();
    }

    private static async Task<Guid> ParseIdAsync(HttpResponseMessage response)
    {
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<string> ObtainTokenAsync()
    {
        try
        {
            using var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
            var body = Serialize(new { email = RecruiterEmail, password = RecruiterPassword });
            var res = await httpClient.PostAsync("/api/identity/login", body);
            if (!res.IsSuccessStatusCode) return string.Empty;
            var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            var root = json.RootElement;
            if (root.TryGetProperty("accessToken", out var at)) return at.GetString() ?? string.Empty;
            if (root.TryGetProperty("token", out var t)) return t.GetString() ?? string.Empty;
            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static StringContent Serialize(object obj) =>
        new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");
}
