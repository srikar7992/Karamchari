// -----------------------------------------------------------------------
// <copyright file="TenantFiltersTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Core.Messaging.Tenant;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Karamchari.TenantIsolationCertification.Messaging;

public sealed class ReplayProtectionServiceTests
{
    [Fact]
    public void IsReplay_ShouldReturnFalse_ForFirstSeenMessage()
    {
        var service = new ReplayProtectionService(NullLogger<ReplayProtectionService>.Instance);
        var messageId = Guid.NewGuid().ToString();

        var result = service.IsReplay(messageId, null);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsReplay_ShouldReturnTrue_ForSecondSeenMessage()
    {
        var service = new ReplayProtectionService(NullLogger<ReplayProtectionService>.Instance);
        var messageId = Guid.NewGuid().ToString();

        service.RecordProcessed(messageId, null);
        var result = service.IsReplay(messageId, null);

        result.Should().BeTrue();
    }
}

public sealed class TenantConsumeFilterTests
{
    private readonly ILogger<TenantConsumeFilter<object>> _logger = Substitute.For<ILogger<TenantConsumeFilter<object>>>();
    private readonly ReplayProtectionService _replayProtection = new(NullLogger<ReplayProtectionService>.Instance);
    private readonly Karamchari.Core.Observability.Tenant.TenantMetricsCollector _metrics = new();
    private readonly ExecutionContextSigner _signer = new("test-secret");
    private readonly IConfiguration _config = new ConfigurationBuilder().Build();

    [Fact]
    public void Constructor_ShouldThrow_WhenLoggerIsNull()
    {
        var action = () => new TenantConsumeFilter<object>(null!, _replayProtection, _metrics, _signer, _config);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenReplayProtectionIsNull()
    {
        var action = () => new TenantConsumeFilter<object>(_logger, null!, _metrics, _signer, _config);

        action.Should().Throw<ArgumentNullException>();
    }
}

public sealed class ExecutionContextHeadersTests
{
    [Fact]
    public void HeaderKeys_ShouldHaveExpectedValues()
    {
        ExecutionContextHeaders.TenantId.Should().Be("MT-Tenant-Id");
        ExecutionContextHeaders.CorrelationId.Should().Be("MT-Correlation-Id");
        ExecutionContextHeaders.ExecutionContextSignature.Should().Be("MT-Context-Signature");
        ExecutionContextHeaders.TraceId.Should().Be("MT-Trace-Id");
        ExecutionContextHeaders.SpanId.Should().Be("MT-Span-Id");
        ExecutionContextHeaders.RequestId.Should().Be("MT-Request-Id");
        ExecutionContextHeaders.Timestamp.Should().Be("MT-Timestamp");
        ExecutionContextHeaders.SourceService.Should().Be("MT-Source-Service");
        ExecutionContextHeaders.ExecutionContextVersion.Should().Be("MT-Context-Version");
        ExecutionContextHeaders.ExecutionEnvelope.Should().Be("MT-Execution-Envelope");
    }

    [Fact]
    public void HeaderKeys_ShouldBeConsistent()
    {
        var keys = new[]
        {
            ExecutionContextHeaders.TenantId,
            ExecutionContextHeaders.CorrelationId,
            ExecutionContextHeaders.ExecutionContextSignature,
            ExecutionContextHeaders.TraceId,
            ExecutionContextHeaders.SpanId,
            ExecutionContextHeaders.RequestId,
            ExecutionContextHeaders.Timestamp,
            ExecutionContextHeaders.SourceService,
            ExecutionContextHeaders.ExecutionContextVersion,
            ExecutionContextHeaders.ExecutionEnvelope
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
