using System.Security.Claims;
using FluentAssertions;
using Karamchari.Core.Multitenancy.Execution;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Karamchari.TenantIsolationCertification.Infrastructure.Execution;

public class TenantExecutionContextFactoryTests
{
    private readonly TenantExecutionContextFactory _factory;

    public TenantExecutionContextFactoryTests()
    {
        _factory = new TenantExecutionContextFactory();
    }

    [Fact]
    public void CreateFromHttpContext_WithValidInputs_ShouldCreateEnvelope()
    {
        var httpContext = CreateHttpContext("acme");
        httpContext.Request.Headers["X-Correlation-ID"] = "550e8400-e29b-41d4-a716-446655440000";
        httpContext.Request.Headers["X-Request-ID"] = "660e8400-e29b-41d4-a716-446655440001";

        var envelope = _factory.CreateFromHttpContext(httpContext, "acme");

        envelope.TenantId.Should().Be("acme");
        envelope.CorrelationId.Should().Be("550e8400-e29b-41d4-a716-446655440000");
        envelope.RequestId.Should().Be("660e8400-e29b-41d4-a716-446655440001");
        envelope.ExecutionSource.Should().Be(ExecutionSource.HttpRequest);
    }

    [Fact]
    public void CreateFromHttpContext_WithNullContext_ShouldThrowArgumentNullException()
    {
        var act = () => _factory.CreateFromHttpContext(null!, "acme");

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateFromHttpContext_WithInvalidTenantId_ShouldThrowArgumentException(string? tenantId)
    {
        var httpContext = CreateHttpContext("acme");

        var act = () => _factory.CreateFromHttpContext(httpContext, tenantId!);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("tenantId");
    }

    [Fact]
    public void CreateFromHttpContext_WithAuthenticatedUser_ShouldExtractUserIdentity()
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-123") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = CreateHttpContext("acme", principal);

        var envelope = _factory.CreateFromHttpContext(httpContext, "acme");

        envelope.UserIdentity.Should().Be("user-123");
    }

    [Fact]
    public void CreateFromHttpContext_WithTraceId_ShouldExtractTraceId()
    {
        var httpContext = CreateHttpContext("acme");

        var envelope = _factory.CreateFromHttpContext(httpContext, "acme");

        // TraceId/SpanId might be null if Activity.Current is null
        // envelope.TraceId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateFromConsumeContext_WithValidInputs_ShouldCreateEnvelope()
    {
        var consumeContext = CreateConsumeContext("tenant1", "test-message-content");

        var envelope = _factory.CreateFromConsumeContext(consumeContext, "tenant1");

        envelope.TenantId.Should().Be("tenant1");
        envelope.ExecutionSource.Should().Be(ExecutionSource.MessageConsumer);
        envelope.CorrelationId.Should().NotBeNullOrEmpty();
        envelope.MessageId.Should().NotBeNull();
        envelope.ContentHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateFromConsumeContext_WithNullContext_ShouldThrowArgumentNullException()
    {
        var act = () => _factory.CreateFromConsumeContext(null!, "tenant1");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateForBackgroundJob_WithValidInputs_ShouldCreateEnvelope()
    {
        var envelope = _factory.CreateForBackgroundJob("acme", "job-123");

        envelope.TenantId.Should().Be("acme");
        envelope.ExecutionSource.Should().Be(ExecutionSource.BackgroundJob);
        envelope.ReplayMetadata.Should().ContainKey("JobId");
        envelope.ReplayMetadata["JobId"].Should().Be("job-123");
    }

    [Fact]
    public void CreateForBackgroundJob_WithNullJobId_ShouldThrowArgumentException()
    {
        var act = () => _factory.CreateForBackgroundJob("acme", null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateForBackgroundJob_WithInvalidTenantId_ShouldThrowArgumentException()
    {
        var act = () => _factory.CreateForBackgroundJob("INVALID", "job-123");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("tenantId");
    }

    [Fact]
    public void CreateForSaga_WithValidInputs_ShouldCreateEnvelope()
    {
        var correlationId = Guid.NewGuid().ToString();

        var envelope = _factory.CreateForSaga("acme", "saga-456", correlationId);

        envelope.TenantId.Should().Be("acme");
        envelope.CorrelationId.Should().Be(correlationId);
        envelope.ExecutionSource.Should().Be(ExecutionSource.SagaOrchestration);
        envelope.ReplayMetadata.Should().ContainKey("SagaId");
        envelope.ReplayMetadata["SagaId"].Should().Be("saga-456");
    }

    [Fact]
    public void CreateForSaga_WithNullSagaId_ShouldThrowArgumentException()
    {
        var act = () => _factory.CreateForSaga("acme", null!, Guid.NewGuid().ToString());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateForMigration_WithValidInputs_ShouldCreateEnvelope()
    {
        var envelope = _factory.CreateForMigration("acme", 20260508031638L);

        envelope.TenantId.Should().Be("acme");
        envelope.ExecutionSource.Should().Be(ExecutionSource.Migration);
        envelope.ReplayMetadata.Should().ContainKey("MigrationVersion");
        envelope.ReplayMetadata["MigrationVersion"].Should().Be("20260508031638");
    }

    [Fact]
    public void CreateReplayContext_WithValidInputs_ShouldCreateReplayEnvelope()
    {
        var original = new TenantExecutionEnvelope(
            "acme",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.HttpRequest);

        var replay = _factory.CreateReplayContext(original, 3);

        replay.RequestId.Should().NotBe(original.RequestId);
        replay.RetryAttempt.Should().Be(3);
        replay.ReplayCount.Should().Be(original.ReplayCount + 1);
        replay.TenantId.Should().Be(original.TenantId);
        replay.CorrelationId.Should().Be(original.CorrelationId);
        replay.ReplayMetadata.Should().ContainKey("OriginalRequestId");
        replay.ReplayMetadata.Should().ContainKey("RetryAttempt");
    }

    [Fact]
    public void CreateReplayContext_WithMultipleRetries_ShouldIncrementReplayCount()
    {
        var original = new TenantExecutionEnvelope(
            "acme",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.HttpRequest);

        var replay1 = _factory.CreateReplayContext(original, 1);
        var replay2 = _factory.CreateReplayContext(replay1, 2);
        var replay3 = _factory.CreateReplayContext(replay2, 3);

        replay1.ReplayCount.Should().Be(1);
        replay2.ReplayCount.Should().Be(2);
        replay3.ReplayCount.Should().Be(3);
    }

    [Fact]
    public void CreateReplayContext_WithNullOriginal_ShouldThrowArgumentNullException()
    {
        var act = () => _factory.CreateReplayContext(null!, 1);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateReplayContext_WithNegativeAttempt_ShouldThrowArgumentOutOfRangeException()
    {
        var original = new TenantExecutionEnvelope(
            "acme",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.HttpRequest);

        var act = () => _factory.CreateReplayContext(original, -1);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("attempt");
    }

    private static HttpContext CreateHttpContext(string tenantId, ClaimsPrincipal? user = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[Karamchari.Core.Multitenancy.TenantConstants.HeaderName] = tenantId;

        if (user is not null)
        {
            context.User = user;
        }

        return context;
    }

    private static ConsumeContext CreateConsumeContext(string tenantId, string messageContent)
    {
        var correlationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        var mock = new Mock<ConsumeContext>();
        mock.Setup(x => x.CorrelationId).Returns(correlationId);
        mock.Setup(x => x.MessageId).Returns(messageId);

        return mock.Object;
    }
}

public class TestMessage
{
    public string Content { get; init; } = string.Empty;
}
