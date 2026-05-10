using FluentAssertions;
using Karamchari.Core.Messaging.Tenant;
using Xunit;

namespace Karamchari.TenantIsolationCertification.Messaging;

public sealed class TenantExecutionEnvelopeTests
{
    [Fact]
    public void ToJson_ShouldSerializeCorrectly()
    {
        var envelope = new TenantExecutionEnvelope(
            "tenant_acme",
            "correlation-123",
            "trace-456",
            "span-789",
            "user@example.com",
            "API",
            "original-msg-1",
            2,
            DateTime.UtcNow);

        var json = envelope.ToJson();

        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("tenant_acme");
        json.Should().Contain("correlation-123");
    }

    [Fact]
    public void FromJson_ShouldDeserializeCorrectly()
    {
        var original = new TenantExecutionEnvelope(
            "tenant_globex",
            "correlation-456",
            "trace-789",
            "span-012",
            "admin@example.com",
            "BackgroundJob",
            "original-msg-2",
            3,
            DateTime.UtcNow);

        var json = original.ToJson();
        var deserialized = TenantExecutionEnvelope.FromJson(json);

        deserialized.Should().NotBeNull();
        deserialized!.TenantId.Should().Be(original.TenantId);
        deserialized.CorrelationId.Should().Be(original.CorrelationId);
        deserialized.TraceId.Should().Be(original.TraceId);
        deserialized.SpanId.Should().Be(original.SpanId);
        deserialized.UserIdentity.Should().Be(original.UserIdentity);
        deserialized.ExecutionSource.Should().Be(original.ExecutionSource);
        deserialized.OriginalMessageId.Should().Be(original.OriginalMessageId);
        deserialized.RetryAttempt.Should().Be(original.RetryAttempt);
    }

    [Fact]
    public void FromJson_WithNullJson_ShouldReturnNull()
    {
        var result = TenantExecutionEnvelope.FromJson(null);

        result.Should().BeNull();
    }

    [Fact]
    public void FromJson_WithEmptyJson_ShouldReturnNull()
    {
        var result = TenantExecutionEnvelope.FromJson(string.Empty);

        result.Should().BeNull();
    }

    [Fact]
    public void FromJson_WithInvalidJson_ShouldReturnNull()
    {
        var result = TenantExecutionEnvelope.FromJson("not valid json");

        result.Should().BeNull();
    }

    [Fact]
    public void RoundTrip_ShouldPreserveAllFields()
    {
        var original = new TenantExecutionEnvelope(
            "tenant_init",
            "corr-123",
            "trace-abc",
            "span-def",
            "service@system.com",
            "KafkaConsumer",
            "msg-ref-1",
            5,
            DateTime.UtcNow);

        var json = original.ToJson();
        var restored = TenantExecutionEnvelope.FromJson(json);

        restored.Should().NotBeNull();
        restored!.TenantId.Should().Be(original.TenantId);
        restored.CorrelationId.Should().Be(original.CorrelationId);
        restored.TraceId.Should().Be(original.TraceId);
        restored.SpanId.Should().Be(original.SpanId);
        restored.UserIdentity.Should().Be(original.UserIdentity);
        restored.ExecutionSource.Should().Be(original.ExecutionSource);
        restored.OriginalMessageId.Should().Be(original.OriginalMessageId);
        restored.RetryAttempt.Should().Be(original.RetryAttempt);
        restored.Timestamp.Should().BeCloseTo(original.Timestamp, TimeSpan.FromSeconds(1));
    }
}

public sealed class TenantExecutionContextTests
{
    [Fact]
    public void CreateFromTenantId_ShouldCreateValidContext()
    {
        var context = TenantExecutionContext.CreateFromTenantId("acme");

        context.Should().NotBeNull();
        context!.TenantId.Should().Be("acme");
        context.CorrelationId.Should().NotBeNullOrEmpty();
        context.RetryAttempt.Should().Be(0);
    }

    [Fact]
    public void CreateFromTenantId_WithNull_ShouldReturnNull()
    {
        var context = TenantExecutionContext.CreateFromTenantId(null!);

        context.Should().BeNull();
    }

    [Fact]
    public void CreateFromTenantId_WithEmpty_ShouldReturnNull()
    {
        var context = TenantExecutionContext.CreateFromTenantId(string.Empty);

        context.Should().BeNull();
    }

    [Fact]
    public void CreateFromTenantId_WithWhitespace_ShouldReturnNull()
    {
        var context = TenantExecutionContext.CreateFromTenantId("   ");

        context.Should().BeNull();
    }

    [Fact]
    public void SetAsCurrent_ShouldMakeContextAvailable()
    {
        var envelope = new TenantExecutionEnvelope(
            "tenant_test",
            "corr-1",
            null, null, null, null, null, 0, DateTime.UtcNow);

        var context = new TenantExecutionContext(envelope);
        context.SetAsCurrent();

        TenantExecutionContext.Current.Should().NotBeNull();
        TenantExecutionContext.Current!.TenantId.Should().Be("tenant_test");
    }

    [Fact]
    public void ClearCurrent_ShouldRemoveContext()
    {
        var envelope = new TenantExecutionEnvelope(
            "tenant_clear",
            "corr-2",
            null, null, null, null, null, 0, DateTime.UtcNow);

        var context = new TenantExecutionContext(envelope);
        context.SetAsCurrent();
        TenantExecutionContext.ClearCurrent();

        TenantExecutionContext.Current.Should().BeNull();
    }

    [Fact]
    public void ToEnvelope_ShouldProduceValidEnvelope()
    {
        var envelope = new TenantExecutionEnvelope(
            "tenant_convert",
            "corr-envelope",
            "trace-id",
            "span-id",
            "user@convert.com",
            "Test",
            "orig-msg",
            4,
            DateTime.UtcNow);

        var context = new TenantExecutionContext(envelope);
        var result = context.ToEnvelope();

        result.TenantId.Should().Be("tenant_convert");
        result.CorrelationId.Should().Be("corr-envelope");
        result.TraceId.Should().Be("trace-id");
        result.SpanId.Should().Be("span-id");
        result.UserIdentity.Should().Be("user@convert.com");
        result.ExecutionSource.Should().Be("Test");
        result.OriginalMessageId.Should().Be("orig-msg");
        result.RetryAttempt.Should().Be(4);
    }

    [Fact]
    public void CurrentTenantId_ShouldReflectCurrentContext()
    {
        var envelope = new TenantExecutionEnvelope(
            "tenant_current",
            "corr-current",
            null, null, null, null, null, 0, DateTime.UtcNow);

        var context = new TenantExecutionContext(envelope);
        context.SetAsCurrent();

        TenantExecutionContext.CurrentTenantId.Should().Be("tenant_current");

        TenantExecutionContext.ClearCurrent();
    }

    [Fact]
    public void MultipleContexts_ShouldNotInterfere()
    {
        var envelope1 = new TenantExecutionEnvelope("tenant_a", "corr-a", null, null, null, null, null, 0, DateTime.UtcNow);
        var envelope2 = new TenantExecutionEnvelope("tenant_b", "corr-b", null, null, null, null, null, 0, DateTime.UtcNow);

        var context1 = new TenantExecutionContext(envelope1);
        var context2 = new TenantExecutionContext(envelope2);

        context1.SetAsCurrent();
        TenantExecutionContext.CurrentTenantId.Should().Be("tenant_a");

        context2.SetAsCurrent();
        TenantExecutionContext.CurrentTenantId.Should().Be("tenant_b");

        TenantExecutionContext.ClearCurrent();
    }
}
