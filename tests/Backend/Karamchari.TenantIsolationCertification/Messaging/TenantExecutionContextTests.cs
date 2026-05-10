using FluentAssertions;
using Karamchari.Core.Multitenancy.Execution;
using Xunit;

namespace Karamchari.TenantIsolationCertification.Messaging;

public sealed class TenantExecutionEnvelopeTests
{
    [Fact]
    public void ToJson_ShouldSerializeCorrectly()
    {
        var envelope = new TenantExecutionEnvelope(
            "acme",
            "correlation-123",
            "request-456",
            ExecutionSource.HttpRequest)
        {
            TraceId = "trace-456",
            SpanId = "span-789",
            UserIdentity = "user@example.com",
            RetryAttempt = 2,
            MessageId = "original-msg-1",
            Timestamp = DateTime.UtcNow
        };

        var json = envelope.ToJson();

        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("acme");
        json.Should().Contain("correlation-123");
    }

    [Fact]
    public void FromJson_ShouldDeserializeCorrectly()
    {
        var original = new TenantExecutionEnvelope(
            "globex",
            "correlation-456",
            "request-789",
            ExecutionSource.BackgroundJob)
        {
            TraceId = "trace-789",
            SpanId = "span-012",
            UserIdentity = "admin@example.com",
            RetryAttempt = 3,
            MessageId = "original-msg-2",
            Timestamp = DateTime.UtcNow
        };

        var json = original.ToJson();
        var deserialized = TenantExecutionEnvelope.FromJson(json);

        deserialized.Should().NotBeNull();
        deserialized!.TenantId.Should().Be(original.TenantId);
        deserialized.CorrelationId.Should().Be(original.CorrelationId);
        deserialized.TraceId.Should().Be(original.TraceId);
        deserialized.SpanId.Should().Be(original.SpanId);
        deserialized.UserIdentity.Should().Be(original.UserIdentity);
        deserialized.ExecutionSource.Should().Be(original.ExecutionSource);
        deserialized.MessageId.Should().Be(original.MessageId);
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
            "init",
            "corr-123",
            "req-abc",
            ExecutionSource.MessageConsumer)
        {
            TraceId = "trace-abc",
            SpanId = "span-def",
            UserIdentity = "service@system.com",
            MessageId = "msg-ref-1",
            RetryAttempt = 5,
            Timestamp = DateTime.UtcNow
        };

        var json = original.ToJson();
        var restored = TenantExecutionEnvelope.FromJson(json);

        restored.Should().NotBeNull();
        restored!.TenantId.Should().Be(original.TenantId);
        restored.CorrelationId.Should().Be(original.CorrelationId);
        restored.TraceId.Should().Be(original.TraceId);
        restored.SpanId.Should().Be(original.SpanId);
        restored.UserIdentity.Should().Be(original.UserIdentity);
        restored.ExecutionSource.Should().Be(original.ExecutionSource);
        restored.MessageId.Should().Be(original.MessageId);
        restored.RetryAttempt.Should().Be(original.RetryAttempt);
        restored.Timestamp.Should().BeCloseTo(original.Timestamp, TimeSpan.FromSeconds(1));
    }
}

public sealed class TenantExecutionContextTests
{
    [Fact]
    public void SetAsCurrent_ShouldMakeContextAvailable()
    {
        var envelope = new TenantExecutionEnvelope(
            "test",
            "corr-1",
            "req-1",
            ExecutionSource.Manual);

        var context = new TenantExecutionContext(envelope);
        using var scope = context.Establish();

        TenantExecutionContext.Current.Should().NotBeNull();
        TenantExecutionContext.Current!.TenantId.Should().Be("test");
    }

    [Fact]
    public void Clear_ShouldRemoveContext()
    {
        var envelope = new TenantExecutionEnvelope(
            "clear",
            "corr-2",
            "req-2",
            ExecutionSource.Manual);

        var context = new TenantExecutionContext(envelope);
        using (context.Establish())
        {
            TenantExecutionContext.Clear();
            TenantExecutionContext.Current.Should().BeNull();
        }
    }

    [Fact]
    public void CurrentTenantId_ShouldReflectCurrentContext()
    {
        var envelope = new TenantExecutionEnvelope(
            "current",
            "corr-current",
            "req-current",
            ExecutionSource.Manual);

        var context = new TenantExecutionContext(envelope);
        using (context.Establish())
        {
            TenantExecutionContext.CurrentTenantId.Should().Be("current");
        }

        TenantExecutionContext.CurrentTenantId.Should().BeNull();
    }

    [Fact]
    public void MultipleContexts_ShouldNotInterfere()
    {
        var envelope1 = new TenantExecutionEnvelope("tenant_a", "corr-a", "req-a", ExecutionSource.Manual);
        var envelope2 = new TenantExecutionEnvelope("tenant_b", "corr-b", "req-b", ExecutionSource.Manual);

        var context1 = new TenantExecutionContext(envelope1);
        var context2 = new TenantExecutionContext(envelope2);

        using (context1.Establish())
        {
            TenantExecutionContext.CurrentTenantId.Should().Be("tenant_a");

            using (context2.Establish())
            {
                TenantExecutionContext.CurrentTenantId.Should().Be("tenant_b");
            }

            TenantExecutionContext.CurrentTenantId.Should().Be("tenant_a");
        }
    }
}
