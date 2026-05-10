using System.Diagnostics;
using FluentAssertions;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Observability.Tenant;
using Xunit;

namespace Karamchari.TenantIsolationCertification.Observability;

public sealed class TenantTelemetryTagsTests
{
    [Fact]
    public void TenantIdTag_ShouldHaveCorrectValue()
    {
        TenantTelemetryTags.TenantId.Should().Be("tenant.id");
    }

    [Fact]
    public void CorrelationIdTag_ShouldHaveCorrectValue()
    {
        TenantTelemetryTags.CorrelationId.Should().Be("tenant.correlation_id");
    }

    [Fact]
    public void ExecutionSourceTag_ShouldHaveCorrectValue()
    {
        TenantTelemetryTags.ExecutionSource.Should().Be("tenant.execution_source");
    }

    [Fact]
    public void RetryAttemptTag_ShouldHaveCorrectValue()
    {
        TenantTelemetryTags.RetryAttempt.Should().Be("tenant.retry_attempt");
    }

    [Fact]
    public void ReplayCountTag_ShouldHaveCorrectValue()
    {
        TenantTelemetryTags.ReplayCount.Should().Be("tenant.replay_count");
    }
}

public sealed class TenantActivitySourceTests : IDisposable
{
    private readonly ActivityListener _listener;

    public TenantActivitySourceTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == TenantActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    [Fact]
    public void StartActivity_ShouldReturnActivityWithTenantTags()
    {
        var source = new TenantActivitySource();
        var tenantContext = new TenantExecutionEnvelope("acme", "corr-123", "req-123", ExecutionSource.HttpRequest, TenantSource.TrustedHeader);

        using var activity = source.StartActivity(
            "TestOperation",
            tenantContext,
            ExecutionSource.HttpRequest,
            "correlation-123");

        activity.Should().NotBeNull();
        activity!.Tags.FirstOrDefault(t => t.Key == TenantTelemetryTags.TenantId).Value.Should().Be("acme");
        activity.Tags.FirstOrDefault(t => t.Key == TenantTelemetryTags.ExecutionSource).Value.Should().Be("HttpRequest");
        activity.Tags.FirstOrDefault(t => t.Key == TenantTelemetryTags.CorrelationId).Value.Should().Be("correlation-123");
    }

    [Fact]
    public void StartRetryActivity_ShouldIncludeRetryAttemptTag()
    {
        var source = new TenantActivitySource();
        var tenantContext = new TenantExecutionEnvelope("globex", "corr-456", "req-456", ExecutionSource.HttpRequest, TenantSource.TrustedHeader);

        using var activity = source.StartRetryActivity(
            "RetryOperation",
            tenantContext,
            ExecutionSource.HttpRequest,
            3,
            "retry-correlation");

        activity.Should().NotBeNull();
        activity!.Tags.FirstOrDefault(t => t.Key == TenantTelemetryTags.RetryAttempt).Value.Should().Be("3");
    }

    [Fact]
    public void StartReplayActivity_ShouldIncludeReplayCountTag()
    {
        var source = new TenantActivitySource();
        var tenantContext = new TenantExecutionEnvelope("initech", "corr-789", "req-789", ExecutionSource.MessageConsumer, TenantSource.TrustedHeader);

        using var activity = source.StartReplayActivity(
            "ReplayOperation",
            tenantContext,
            ExecutionSource.MessageConsumer,
            2);

        activity.Should().NotBeNull();
        activity!.Tags.FirstOrDefault(t => t.Key == TenantTelemetryTags.ReplayCount).Value.Should().Be("2");
    }

    public void Dispose()
    {
        _listener.Dispose();
    }
}

public sealed class PiiMaskingFormatterTests
{
    [Fact]
    public void MaskEmail_ShouldMaskCorrectly()
    {
        var formatter = new PiiMaskingFormatter(PiiMaskingPolicy.Strict);

        var result = formatter.Mask("user@example.com");

        result.Should().MatchRegex(@"u\*\*\*.*@\*\*\*\.\w+");
    }

    [Fact]
    public void MaskPhone_ShouldMaskCorrectly()
    {
        var formatter = new PiiMaskingFormatter(PiiMaskingPolicy.Strict);

        var result = formatter.Mask("Contact: 555-123-4567");

        result.Should().MatchRegex(@"\*\*\*-.*-4567");
    }

    [Fact]
    public void MaskEmployeeId_ShouldMaskCorrectly()
    {
        var formatter = new PiiMaskingFormatter(PiiMaskingPolicy.Strict);

        var result = formatter.Mask("Employee: EMP-12345");

        result.Should().Contain("***-EMP-***");
    }

    [Fact]
    public void MaskSalary_ShouldRedact()
    {
        var formatter = new PiiMaskingFormatter(PiiMaskingPolicy.Strict);

        var result = formatter.Mask("Salary: $75,000 per year");

        result.Should().Contain("[REDACTED]");
    }

    [Fact]
    public void MaskSsn_ShouldMaskCorrectly()
    {
        var formatter = new PiiMaskingFormatter(PiiMaskingPolicy.Strict);

        var result = formatter.Mask("SSN: 123-45-6789");

        result.Should().MatchRegex(@"\*\*\*-.*-6789");
    }

    [Fact]
    public void MaskCreditCard_ShouldMaskCorrectly()
    {
        var formatter = new PiiMaskingFormatter(PiiMaskingPolicy.Strict);

        var result = formatter.Mask("Card: 4111-1111-1111-1111");

        result.Should().Contain("****-****-****-1111");
    }

    [Fact]
    public void BasicPolicy_ShouldNotMaskSalaryOrSsn()
    {
        var formatter = new PiiMaskingFormatter(PiiMaskingPolicy.Basic);

        var result = formatter.Mask("Salary: $75,000");

        result.Should().Contain("75,000");
    }

    [Fact]
    public void NonePolicy_ShouldNotMaskAnything()
    {
        var formatter = new PiiMaskingFormatter(PiiMaskingPolicy.None);

        var result = formatter.Mask("user@example.com");

        result.Should().Be("user@example.com");
    }

    [Theory]
    [InlineData(SensitiveDataType.Email, "test@example.com")]
    [InlineData(SensitiveDataType.Phone, "555-123-4567")]
    [InlineData(SensitiveDataType.EmployeeId, "EMP-12345")]
    [InlineData(SensitiveDataType.Salary, "75000")]
    [InlineData(SensitiveDataType.Ssn, "123-45-6789")]
    [InlineData(SensitiveDataType.CreditCard, "4111111111111111")]
    public void CreateMaskedDisplay_ShouldReturnMaskedValue(SensitiveDataType dataType, string value)
    {
        var result = PiiMaskingFormatter.CreateMaskedDisplay(value, dataType);

        result.Should().NotBe(value);
        result.Should().NotContain(value);
    }
}

public sealed class TenantCorrelationPropagatorTests
{
    [Fact]
    public void ExtractFromHttpHeaders_ShouldExtractTenantAndCorrelation()
    {
        var source = new TenantActivitySource();
        var propagator = new TenantCorrelationPropagator(source);

        var headers = new Dictionary<string, string?>
        {
            ["x-tenant-id"] = "acme",
            ["x-correlation-id"] = "corr-123",
            ["traceparent"] = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01"
        };

        var (tenantId, correlationId, traceContext) = propagator.ExtractFromHttpHeaders(headers);

        tenantId.Should().Be("acme");
        correlationId.Should().Be("corr-123");
        traceContext.Should().NotBeNull();
    }

    [Fact]
    public void InjectIntoHttpHeaders_ShouldSetRequiredHeaders()
    {
        var source = new TenantActivitySource();
        var propagator = new TenantCorrelationPropagator(source);

        var tenantContext = new TenantExecutionEnvelope("globex", "corr-123", "req-123", ExecutionSource.HttpRequest, TenantSource.TrustedHeader);
        var headers = new Dictionary<string, string?>();

        propagator.InjectIntoHttpHeaders(headers, tenantContext, "test-correlation");

        headers["x-tenant-id"].Should().Be("globex");
        headers["x-correlation-id"].Should().Be("test-correlation");
    }

    [Fact]
    public void ExtractFromMessageHeaders_ShouldExtractTenantContext()
    {
        var source = new TenantActivitySource();
        var propagator = new TenantCorrelationPropagator(source);

        var headers = new Dictionary<string, object?>
        {
            ["tenant-id"] = "initech",
            ["correlation-id"] = "msg-corr-456",
            ["traceparent"] = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01"
        };

        var (tenantId, correlationId, traceContext) = propagator.ExtractFromMessageHeaders(headers);

        tenantId.Should().Be("initech");
        correlationId.Should().Be("msg-corr-456");
    }

    [Fact]
    public void InjectIntoMessageHeaders_ShouldSetTenantHeaders()
    {
        var source = new TenantActivitySource();
        var propagator = new TenantCorrelationPropagator(source);

        var tenantContext = new TenantExecutionEnvelope("acme", "corr-123", "req-123", ExecutionSource.MessageConsumer, TenantSource.TrustedHeader);
        var headers = new Dictionary<string, object?>();

        propagator.InjectIntoMessageHeaders(headers, tenantContext, "msg-corr");

        headers["tenant-id"].Should().Be("acme");
        headers["tenant-schema"].Should().Be("tenant_acme");
        headers["correlation-id"].Should().Be("msg-corr");
    }
}

public sealed class TenantMetricsCollectorTests : IDisposable
{
    private readonly TenantMetricsCollector _collector;

    public TenantMetricsCollectorTests()
    {
        _collector = new TenantMetricsCollector();
    }

    [Fact]
    public void RecordIsolationViolation_ShouldNotThrow()
    {
        var action = () => _collector.RecordIsolationViolation("acme", "CrossTenantAccess", "Repository");

        action.Should().NotThrow();
    }

    [Fact]
    public void RecordRlsQueryDuration_ShouldNotThrow()
    {
        var action = () => _collector.RecordRlsQueryDuration("acme", 45.5, "SELECT");

        action.Should().NotThrow();
    }

    [Fact]
    public void RecordPoolContamination_ShouldNotThrow()
    {
        var action = () => _collector.RecordPoolContamination("acme", "globex", "Warning");

        action.Should().NotThrow();
    }

    [Fact]
    public void RecordReplayDetection_ShouldNotThrow()
    {
        var action = () => _collector.RecordReplayDetection("acme", "PaymentProcessing", 3);

        action.Should().NotThrow();
    }

    [Fact]
    public void RecordRetryStorm_ShouldNotThrow()
    {
        var action = () => _collector.RecordRetryStorm("globex", "DatabaseOperation", 5);

        action.Should().NotThrow();
    }

    [Fact]
    public void RecordContextResolution_ShouldNotThrow()
    {
        var action = () => _collector.RecordContextResolution("acme", true, "HttpRequest");

        action.Should().NotThrow();
    }

    [Fact]
    public void RecordOperationDuration_ShouldNotThrow()
    {
        var action = () => _collector.RecordOperationDuration("acme", "Query", "HttpRequest", 123.4);

        action.Should().NotThrow();
    }

    public void Dispose()
    {
        _collector.Dispose();
    }
}
