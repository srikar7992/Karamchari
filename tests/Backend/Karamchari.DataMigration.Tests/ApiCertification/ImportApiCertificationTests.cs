using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FluentAssertions;
using Karamchari.DataMigration.Tests.Integration.Fixtures;
using Xunit;

namespace Karamchari.DataMigration.Tests.ApiCertification;

/// <summary>
/// Live API certification tests for the DataMigration endpoints.
/// Uses a real SQL Server (Testcontainers) for DataMigrationDbContext and
/// InMemory databases for all other module contexts.
///
/// These tests certify that all 7 endpoints:
///   POST   /api/v1/migration/imports
///   GET    /api/v1/migration/imports/{id}
///   GET    /api/v1/migration/imports/{id}/preview
///   POST   /api/v1/migration/imports/{id}/validate
///   POST   /api/v1/migration/imports/{id}/execute
///   POST   /api/v1/migration/imports/{id}/cancel
///   GET    /api/v1/migration/imports/{id}/error-report
/// respond with correct HTTP status codes and enforce authentication.
/// </summary>
[Collection(nameof(ApiCertificationCollection))]
public sealed class ImportApiCertificationTests(ApiCertificationFactory factory)
{
    private const string BaseUrl = "/api/v1/migration/imports";

    // ── Helpers ──────────────────────────────────────────────────────────────

    private HttpClient CreateClient(string? token = null)
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        if (token is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static MultipartFormDataContent BuildUploadContent(
        string importType = "EmployeeImport",
        string csv = DefaultEmployeeCsv)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(importType), "importType");
        content.Add(new StringContent("1.0"), "templateVersion");
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "import.csv");
        return content;
    }

    private const string DefaultEmployeeCsv = """
        Employee Number,First Name,Last Name,Email,Department,Designation,Manager Employee Number,Date Of Joining,Employment Type
        CERT001,Alice,Smith,alice@cert.test,,,,2024-01-01,Full-Time
        CERT002,Bob,Jones,bob@cert.test,,,,2024-02-01,Full-Time
        """;

    private const string InvalidEmployeeCsv = """
        Employee Number,First Name,Last Name,Email,Department,Designation,Manager Employee Number,Date Of Joining,Employment Type
        ,Alice,Smith,alice@cert.test,,,,2024-01-01,Full-Time
        CERT003,,Jones,,,,,2024-01-01,Full-Time
        """;

    // ── Authentication / Authorization ────────────────────────────────────────

    [DockerRequiredFact]
    public async Task Upload_Unauthenticated_Returns401()
    {
        using var client = CreateClient(token: null);

        var response = await client.PostAsync(BaseUrl, BuildUploadContent());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DockerRequiredFact]
    public async Task GetStatus_Unauthenticated_Returns401()
    {
        using var client = CreateClient(token: null);

        var response = await client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Upload (POST /) ───────────────────────────────────────────────────────

    [DockerRequiredFact]
    public async Task Upload_WithAdminToken_Returns201()
    {
        using var client = CreateClient(ApiCertificationFactory.CreateAdminToken());

        var response = await client.PostAsync(BaseUrl, BuildUploadContent());
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"expected 201 Created but got {(int)response.StatusCode}: {body}");
        response.Headers.Location.Should().NotBeNull("201 must include a Location header");
    }

    [DockerRequiredFact]
    public async Task Upload_EmptyFile_Returns400()
    {
        using var client = CreateClient(ApiCertificationFactory.CreateAdminToken());

        // Send multipart with empty file content
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("EmployeeImport"), "importType");
        var emptyFile = new ByteArrayContent(Array.Empty<byte>());
        emptyFile.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(emptyFile, "file", "empty.csv");

        var response = await client.PostAsync(BaseUrl, content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GetStatus (GET /{id}) ─────────────────────────────────────────────────

    [DockerRequiredFact]
    public async Task GetStatus_AfterUpload_Returns200WithJobDetails()
    {
        using var client = CreateClient(ApiCertificationFactory.CreateAdminToken());

        var upload = await client.PostAsync(BaseUrl, BuildUploadContent());
        var id = ExtractId(upload.Headers.Location!);

        var response = await client.GetAsync($"{BaseUrl}/{id}");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"expected 200 OK but got {(int)response.StatusCode}: {body}");
        body.Should().Contain(id.ToString(), "response should echo the job ID");
    }

    [DockerRequiredFact]
    public async Task GetStatus_UnknownId_Returns404()
    {
        using var client = CreateClient(ApiCertificationFactory.CreateAdminToken());

        var response = await client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Preview (GET /{id}/preview) ───────────────────────────────────────────

    [DockerRequiredFact]
    public async Task Preview_AfterUpload_Returns200WithRows()
    {
        using var client = CreateClient(ApiCertificationFactory.CreateAdminToken());

        var upload = await client.PostAsync(BaseUrl, BuildUploadContent());
        var id = ExtractId(upload.Headers.Location!);

        var response = await client.GetAsync($"{BaseUrl}/{id}/preview");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"expected 200 OK but got {(int)response.StatusCode}: {body}");
        body.Should().Contain("CERT001", "preview should include row data");
    }

    // ── Validate (POST /{id}/validate) ────────────────────────────────────────

    [DockerRequiredFact]
    public async Task Validate_ValidCsv_Returns200WithValidSummary()
    {
        using var client = CreateClient(ApiCertificationFactory.CreateAdminToken());

        var upload = await client.PostAsync(BaseUrl, BuildUploadContent());
        var id = ExtractId(upload.Headers.Location!);

        var response = await client.PostAsync($"{BaseUrl}/{id}/validate", content: null);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"expected 200 OK but got {(int)response.StatusCode}: {body}");
        body.Should().Contain("\"validRows\"", "summary must include validRows");
    }

    [DockerRequiredFact]
    public async Task Validate_InvalidCsv_Returns200WithErrorRows()
    {
        using var client = CreateClient(ApiCertificationFactory.CreateAdminToken());

        var upload = await client.PostAsync(BaseUrl, BuildUploadContent(csv: InvalidEmployeeCsv));
        var id = ExtractId(upload.Headers.Location!);

        var response = await client.PostAsync($"{BaseUrl}/{id}/validate", content: null);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"expected 200 OK but got {(int)response.StatusCode}: {body}");
        body.Should().Contain("\"invalidRows\"", "summary must include invalidRows");
    }

    // ── Execute (POST /{id}/execute) ──────────────────────────────────────────

    [DockerRequiredFact]
    public async Task Execute_OnValidatedJob_Returns202()
    {
        using var client = CreateClient(ApiCertificationFactory.CreateAdminToken());

        // Upload → Validate → Execute
        var upload = await client.PostAsync(BaseUrl, BuildUploadContent());
        var id = ExtractId(upload.Headers.Location!);
        await client.PostAsync($"{BaseUrl}/{id}/validate", content: null);

        var response = await client.PostAsync($"{BaseUrl}/{id}/execute", content: null);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Accepted,
            $"expected 202 Accepted but got {(int)response.StatusCode}: {body}");
    }

    [DockerRequiredFact]
    public async Task Execute_OnNotYetValidatedJob_Returns400()
    {
        using var client = CreateClient(ApiCertificationFactory.CreateAdminToken());

        var upload = await client.PostAsync(BaseUrl, BuildUploadContent());
        var id = ExtractId(upload.Headers.Location!);

        // Execute WITHOUT validating first
        var response = await client.PostAsync($"{BaseUrl}/{id}/execute", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "executing an unvalidated job must be rejected");
    }

    // ── Cancel (POST /{id}/cancel) ────────────────────────────────────────────

    [DockerRequiredFact]
    public async Task Cancel_OnPendingJob_Returns200()
    {
        using var client = CreateClient(ApiCertificationFactory.CreateAdminToken());

        var upload = await client.PostAsync(BaseUrl, BuildUploadContent());
        var id = ExtractId(upload.Headers.Location!);

        var response = await client.PostAsync($"{BaseUrl}/{id}/cancel", content: null);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"expected 200 OK but got {(int)response.StatusCode}: {body}");
        // Status is serialized as an integer enum; just verify the response has the job ID.
        body.Should().Contain(id.ToString(), "response should echo the cancelled job ID");
    }

    // ── Error Report (GET /{id}/error-report) ─────────────────────────────────

    [DockerRequiredFact]
    public async Task ErrorReport_Returns200WithCsvContent()
    {
        using var client = CreateClient(ApiCertificationFactory.CreateAdminToken());

        var upload = await client.PostAsync(BaseUrl, BuildUploadContent());
        var id = ExtractId(upload.Headers.Location!);

        var response = await client.GetAsync($"{BaseUrl}/{id}/error-report");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"expected 200 OK but got {(int)response.StatusCode}: {body}");
        body.Should().Contain("RowNumber,ErrorMessage",
            "error report must be CSV with header row");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Guid ExtractId(Uri location)
    {
        // WebApplicationFactory returns relative Location URIs; handle both cases.
        var path = location.IsAbsoluteUri ? location.AbsolutePath : location.OriginalString;
        var segment = path.Split('/').Last(s => !string.IsNullOrWhiteSpace(s));
        return Guid.Parse(segment);
    }
}
