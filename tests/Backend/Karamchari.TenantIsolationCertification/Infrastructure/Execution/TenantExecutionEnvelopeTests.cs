using FluentAssertions;
using Karamchari.Core.Multitenancy.Execution;
using Xunit;

namespace Karamchari.TenantIsolationCertification.Infrastructure.Execution;

public class TenantExecutionEnvelopeTests
{
    [Fact]
    public void Constructor_WithValidTenantId_ShouldCreateEnvelope()
    {
        var envelope = new TenantExecutionEnvelope(
            "acme",
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.HttpRequest);

        envelope.TenantId.Should().Be("acme");
        envelope.ExecutionSource.Should().Be(ExecutionSource.HttpRequest);
        envelope.RetryAttempt.Should().Be(0);
        envelope.ReplayCount.Should().Be(0);
        envelope.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("acme")]
    [InlineData("tenant-123")]
    [InlineData("a1b2_c3")]
    [InlineData("my-tenant_with_underscores")]
    public void Constructor_WithValidFormats_ShouldNotThrow(string tenantId)
    {
        var act = () => new TenantExecutionEnvelope(
            tenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.Manual);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidTenantId_ShouldThrowArgumentException(string? tenantId)
    {
        var act = () => new TenantExecutionEnvelope(
            tenantId!,
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.Manual);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("tenantId");
    }

    [Theory]
    [InlineData("ACME")]
    [InlineData("Tenant-123")]
    [InlineData("-acme")]
    [InlineData("acme-")]
    [InlineData("a b")]
    public void Constructor_WithInvalidFormats_ShouldThrowArgumentException(string tenantId)
    {
        var act = () => new TenantExecutionEnvelope(
            tenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.Manual);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("tenantId");
    }

    [Fact]
    public void ComputeContentHash_WithValidContent_ShouldReturnSha256Hash()
    {
        var content = System.Text.Encoding.UTF8.GetBytes("test message content");
        var hash = TenantExecutionEnvelope.ComputeContentHash(content);

        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[a-f0-9]{64}$");
    }

    [Fact]
    public void ComputeContentHash_WithSameContent_ShouldReturnSameHash()
    {
        var content = System.Text.Encoding.UTF8.GetBytes("consistent content");
        var hash1 = TenantExecutionEnvelope.ComputeContentHash(content);
        var hash2 = TenantExecutionEnvelope.ComputeContentHash(content);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeContentHash_WithDifferentContent_ShouldReturnDifferentHash()
    {
        var content1 = System.Text.Encoding.UTF8.GetBytes("content 1");
        var content2 = System.Text.Encoding.UTF8.GetBytes("content 2");
        var hash1 = TenantExecutionEnvelope.ComputeContentHash(content1);
        var hash2 = TenantExecutionEnvelope.ComputeContentHash(content2);

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ComputeContentHash_WithNullContent_ShouldThrowArgumentNullException()
    {
        var act = () => TenantExecutionEnvelope.ComputeContentHash(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void With_ShouldCreateCopyWithModifiedProperties()
    {
        var original = new TenantExecutionEnvelope(
            "acme",
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.HttpRequest);

        var modified = original.With(
            TenantId: "newtenant",
            RetryAttempt: 3);

        modified.TenantId.Should().Be("newtenant");
        modified.RetryAttempt.Should().Be(3);
        original.TenantId.Should().Be("acme");
        original.RetryAttempt.Should().Be(0);
    }

    [Fact]
    public void Envelope_WithAllProperties_ShouldPreserveAllValues()
    {
        var correlationId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var metadata = new Dictionary<string, string> { { "Key", "Value" } };

        var envelope = new TenantExecutionEnvelope(
            "testtenant",
            correlationId,
            requestId,
            ExecutionSource.MessageConsumer)
        {
            RetryAttempt = 2,
            ReplayCount = 1,
            UserIdentity = "user-123",
            TraceId = "trace-abc",
            SpanId = "span-def",
            MessageId = messageId,
            ContentHash = "abc123",
            ReplayMetadata = metadata
        };

        envelope.TenantId.Should().Be("testtenant");
        envelope.CorrelationId.Should().Be(correlationId);
        envelope.RequestId.Should().Be(requestId);
        envelope.RetryAttempt.Should().Be(2);
        envelope.ReplayCount.Should().Be(1);
        envelope.UserIdentity.Should().Be("user-123");
        envelope.TraceId.Should().Be("trace-abc");
        envelope.SpanId.Should().Be("span-def");
        envelope.MessageId.Should().Be(messageId);
        envelope.ContentHash.Should().Be("abc123");
        envelope.ReplayMetadata.Should().ContainKey("Key");
    }
}
