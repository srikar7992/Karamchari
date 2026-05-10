using FluentAssertions;
using Karamchari.Core.Messaging.Tenant;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Karamchari.TenantIsolationCertification.Messaging;

public sealed class ReplayProtectionServiceTests
{
    private readonly ILogger<ReplayProtectionService> _logger;
    private readonly ReplayProtectionService _service;

    public ReplayProtectionServiceTests()
    {
        _logger = Substitute.For<ILogger<ReplayProtectionService>>();
        _service = new ReplayProtectionService(_logger, TimeSpan.FromHours(24));
    }

    [Fact]
    public void IsReplay_WithNewMessage_ShouldReturnFalse()
    {
        var messageId = Guid.NewGuid().ToString();

        var result = _service.IsReplay(messageId);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsReplay_WithPreviouslyProcessedMessage_ShouldReturnTrue()
    {
        var messageId = Guid.NewGuid().ToString();

        _service.RecordProcessed(messageId);
        var result = _service.IsReplay(messageId);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsReplay_WithContentHash_ShouldTrackSeparately()
    {
        var messageId = Guid.NewGuid().ToString();
        var hash1 = ReplayProtectionService.ComputeContentHash("content1");
        var hash2 = ReplayProtectionService.ComputeContentHash("content2");

        _service.RecordProcessed(messageId, hash1);
        var result1 = _service.IsReplay(messageId, hash1);
        var result2 = _service.IsReplay(messageId, hash2);

        result1.Should().BeTrue("same message and hash should be detected as replay");
        result2.Should().BeFalse("same message with different hash should not be detected");
    }

    [Fact]
    public void IsReplay_WithNullMessageId_ShouldReturnFalse()
    {
        var result = _service.IsReplay(null!);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsReplay_WithEmptyMessageId_ShouldReturnFalse()
    {
        var result = _service.IsReplay(string.Empty);

        result.Should().BeFalse();
    }

    [Fact]
    public void RecordProcessed_ShouldTrackMessageCount()
    {
        var countBefore = _service.TrackedMessageCount;

        _service.RecordProcessed(Guid.NewGuid().ToString());
        _service.RecordProcessed(Guid.NewGuid().ToString());

        _service.TrackedMessageCount.Should().BeGreaterThan(countBefore);
    }

    [Fact]
    public void ComputeContentHash_ShouldReturnConsistentHash()
    {
        var content = "test message content";

        var hash1 = ReplayProtectionService.ComputeContentHash(content);
        var hash2 = ReplayProtectionService.ComputeContentHash(content);

        hash1.Should().Be(hash2);
        hash1.Should().HaveLength(64);
    }

    [Fact]
    public void ComputeContentHash_DifferentContent_ShouldReturnDifferentHash()
    {
        var hash1 = ReplayProtectionService.ComputeContentHash("content1");
        var hash2 = ReplayProtectionService.ComputeContentHash("content2");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ValidateContentHash_ValidHash_ShouldReturnTrue()
    {
        var content = "test message";
        var hash = ReplayProtectionService.ComputeContentHash(content);

        var result = ReplayProtectionService.ValidateContentHash(hash, content);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateContentHash_InvalidHash_ShouldReturnFalse()
    {
        var content = "test message";

        var result = ReplayProtectionService.ValidateContentHash("invalid_hash", content);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateContentHash_EmptyProvidedHash_ShouldReturnFalse()
    {
        var result = ReplayProtectionService.ValidateContentHash(string.Empty, "content");

        result.Should().BeFalse();
    }

    [Fact]
    public void ClearExpiredEntries_ShouldRemoveOldEntries()
    {
        var shortWindowService = new ReplayProtectionService(_logger, TimeSpan.FromMilliseconds(1));
        var messageId = Guid.NewGuid().ToString();

        shortWindowService.RecordProcessed(messageId);
        Thread.Sleep(10);
        shortWindowService.ClearExpiredEntries();

        var result = shortWindowService.IsReplay(messageId);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsReplay_WithStaleEntry_ShouldTreatAsNew()
    {
        var shortWindowService = new ReplayProtectionService(_logger, TimeSpan.FromMilliseconds(1));
        var messageId = Guid.NewGuid().ToString();

        shortWindowService.RecordProcessed(messageId);
        Thread.Sleep(10);

        var result = shortWindowService.IsReplay(messageId);

        result.Should().BeFalse();
    }
}

public sealed class TenantMessageHeaderKeysTests
{
    [Fact]
    public void HeaderKeys_ShouldHaveExpectedValues()
    {
        TenantMessageHeaderKeys.TenantId.Should().Be("X-Tenant-Id");
        TenantMessageHeaderKeys.CorrelationId.Should().Be("X-Correlation-Id");
        TenantMessageHeaderKeys.RequestId.Should().Be("X-Request-Id");
        TenantMessageHeaderKeys.ExecutionSource.Should().Be("X-Execution-Source");
        TenantMessageHeaderKeys.RetryAttempt.Should().Be("X-Retry-Attempt");
        TenantMessageHeaderKeys.ReplayCount.Should().Be("X-Replay-Count");
        TenantMessageHeaderKeys.TraceId.Should().Be("X-Trace-Id");
        TenantMessageHeaderKeys.SpanId.Should().Be("X-Span-Id");
        TenantMessageHeaderKeys.ContentHash.Should().Be("X-Content-Hash");
        TenantMessageHeaderKeys.Timestamp.Should().Be("X-Timestamp");
        TenantMessageHeaderKeys.UserIdentity.Should().Be("X-User-Identity");
        TenantMessageHeaderKeys.OriginalMessageId.Should().Be("X-Original-Message-Id");
        TenantMessageHeaderKeys.ExecutionEnvelope.Should().Be("X-Execution-Envelope");
    }

    [Fact]
    public void HeaderKeys_ShouldBeConsistent()
    {
        var keys = new[]
        {
            TenantMessageHeaderKeys.TenantId,
            TenantMessageHeaderKeys.CorrelationId,
            TenantMessageHeaderKeys.RequestId,
            TenantMessageHeaderKeys.ExecutionSource,
            TenantMessageHeaderKeys.RetryAttempt,
            TenantMessageHeaderKeys.ReplayCount,
            TenantMessageHeaderKeys.TraceId,
            TenantMessageHeaderKeys.SpanId,
            TenantMessageHeaderKeys.ContentHash,
            TenantMessageHeaderKeys.Timestamp,
            TenantMessageHeaderKeys.UserIdentity,
            TenantMessageHeaderKeys.OriginalMessageId,
            TenantMessageHeaderKeys.ExecutionEnvelope
        };

        keys.Should().OnlyHaveUniqueItems();
    }
}

public sealed class MalformedTenantMessageExceptionTests
{
    [Fact]
    public void Exception_ShouldHaveDefaultReason()
    {
        var ex = new MalformedTenantMessageException();

        ex.FailureReason.Should().Be(TenantMessageFailureReason.Unspecified);
    }

    [Fact]
    public void Exception_ShouldStoreTenantId()
    {
        var ex = new MalformedTenantMessageException("test", "tenant123", TenantMessageFailureReason.MissingTenantId);

        ex.TenantId.Should().Be("tenant123");
        ex.FailureReason.Should().Be(TenantMessageFailureReason.MissingTenantId);
    }

    [Fact]
    public void Exception_ShouldPreserveMessage()
    {
        var ex = new MalformedTenantMessageException("Custom error message");

        ex.Message.Should().Be("Custom error message");
    }
}
