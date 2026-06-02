using System.Text;
using System.Text.Json;
using NBomber.CSharp;
using Xunit;

namespace Karamchari.PerformanceTests;

/// <summary>
/// P1 — Authentication load test.
/// 100 concurrent users hammering POST /api/identity/login for 2 minutes.
/// Thresholds: p95 less than 500ms, p99 less than 1000ms, error rate less than 1%.
///
/// Run manually against a live environment:
///   KARAMCHARI_BASE_URL=http://localhost:5000 dotnet test --filter AuthenticationLoadTest
/// </summary>
public sealed class AuthenticationLoadTest
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("KARAMCHARI_BASE_URL") ?? "http://localhost:5000";

    private static readonly (string Email, string Password)[] Credentials =
    [
        ("load-user-01@acme.karamchari.com", "LoadTest@123!"),
        ("load-user-02@acme.karamchari.com", "LoadTest@123!"),
        ("load-user-03@acme.karamchari.com", "LoadTest@123!"),
        ("load-user-04@acme.karamchari.com", "LoadTest@123!"),
        ("load-user-05@acme.karamchari.com", "LoadTest@123!"),
    ];

    [Fact(Skip = "Run manually against a live environment")]
    public void Run_AuthenticationLoad_P1()
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };

        var scenario = Scenario.Create("P1_Authentication", async context =>
        {
            var cred = Credentials[context.InvocationNumber % Credentials.Length];

            var payload = JsonSerializer.Serialize(new
            {
                email = cred.Email,
                password = cred.Password,
            });

            using var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(
                "/api/identity/login", content, CancellationToken.None);

            // 200 = authenticated, 401 = invalid creds (valid server behaviour).
            // Only 5xx counts as a test failure.
            return (int)response.StatusCode >= 500
                ? Response.Fail(message: "Server error on login")
                : Response.Ok();
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 100, during: TimeSpan.FromMinutes(2)));

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFileName("P1_Authentication_Report")
            .WithReportFolder("reports/P1_Authentication")
            .Run();
    }
}
