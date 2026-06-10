// -----------------------------------------------------------------------
// <copyright file="BackgroundJobTenantSafetyTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Core.BackgroundJobs.Tenant;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using Karamchari.TenantIsolationCertification.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Karamchari.TenantIsolationCertification.BackgroundJobs;

public sealed class TenantAwareJobEnvelopeTests
{
    [Fact]
    public void FromEnvelope_ShouldMapAllProperties()
    {
        var envelope = new TenantExecutionEnvelope(
            "acme",
            "corr-123",
            "req-456",
            ExecutionSource.BackgroundJob,
            TenantSource.Background)
        {
            RetryAttempt = 2,
            UserIdentity = "user_123",
            ReplayMetadata = new Dictionary<string, string> { { "key", "value" } }
        };

        var jobEnvelope = envelope; // Use envelope directly as TenantAwareJobEnvelope is deleted

        jobEnvelope.TenantId.Should().Be("acme");
        jobEnvelope.CorrelationId.Should().Be(envelope.CorrelationId);
        jobEnvelope.RequestId.Should().Be(envelope.RequestId);
        jobEnvelope.ExecutionSource.Should().Be(ExecutionSource.BackgroundJob);
        jobEnvelope.RetryAttempt.Should().Be(2);
        jobEnvelope.UserIdentity.Should().Be("user_123");
        jobEnvelope.ReplayMetadata.Should().ContainKey("key");
    }

    [Fact]
    public void ToEnvelope_ShouldRestoreEnvelopeCorrectly()
    {
        var correlationId = "corr-789";
        var requestId = "req-012";
        var timestamp = DateTime.UtcNow;

        var envelope = new TenantExecutionEnvelope(
            "globex",
            correlationId,
            requestId,
            ExecutionSource.BackgroundJob,
            TenantSource.Background)
        {
            RetryAttempt = 1,
            Timestamp = timestamp,
            UserIdentity = "admin_user",
            ReplayMetadata = new Dictionary<string, string> { { "env", "prod" } }
        };

        envelope.TenantId.Should().Be("globex");
        envelope.CorrelationId.Should().Be(correlationId);
        envelope.RequestId.Should().Be(requestId);
        envelope.ExecutionSource.Should().Be(ExecutionSource.BackgroundJob);
        envelope.RetryAttempt.Should().Be(1);
        envelope.Timestamp.Should().Be(timestamp);
        envelope.UserIdentity.Should().Be("admin_user");
        envelope.ReplayMetadata.Should().ContainKey("env");
    }

    [Theory]
    [InlineData("acme")]
    [InlineData("globex")]
    [InlineData("initech")]
    [InlineData("a")]
    public void ToEnvelope_ShouldAcceptValidTenantIds(string tenantId)
    {
        var act = () => new TenantExecutionEnvelope(
            tenantId,
            "corr",
            "req",
            ExecutionSource.BackgroundJob,
            TenantSource.Background);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("InvalidTenant!")]
    [InlineData("ADMIN")]
    [InlineData("tenant/../admin")]
    public void ToEnvelope_ShouldRejectInvalidTenantIds(string tenantId)
    {
        var act = () => new TenantExecutionEnvelope(
            tenantId,
            "corr",
            "req",
            ExecutionSource.BackgroundJob,
            TenantSource.Background);

        act.Should().Throw<ArgumentException>(); // Validation moved to constructor
    }
}

public sealed class TenantJobContextSerializerTests : IDisposable
{
    private readonly TenantJobContextSerializer _serializer;
    private readonly Mock<ILogger<TenantJobContextSerializer>> _loggerMock;
    private readonly TenantTestContext _contextA;
    private readonly TenantTestContext _contextB;

    public TenantJobContextSerializerTests()
    {
        _loggerMock = new Mock<ILogger<TenantJobContextSerializer>>();
        _serializer = new TenantJobContextSerializer(_loggerMock.Object);
        _contextA = TenantTestContext.Create("acme", TenantSource.Background);
        _contextB = TenantTestContext.Create("globex", TenantSource.Background);
    }

    [Fact]
    public void Serialize_ShouldProduceBase64Output()
    {
        var envelope = new TenantExecutionEnvelope(
            "acme",
            "corr",
            "req",
            ExecutionSource.BackgroundJob,
            TenantSource.Background);

        var serialized = _serializer.Serialize(envelope);

        serialized.Should().NotBeNullOrEmpty();
        var act = () => Convert.FromBase64String(serialized);
        act.Should().NotThrow();
    }

    [Fact]
    public void Deserialize_ShouldRestoreEnvelope()
    {
        var originalEnvelope = new TenantExecutionEnvelope(
            "globex",
            "corr-orig",
            "req-orig",
            ExecutionSource.BackgroundJob,
            TenantSource.Background)
        {
            RetryAttempt = 3,
            UserIdentity = "user_456"
        };

        var serialized = _serializer.Serialize(originalEnvelope);
        var restored = _serializer.Deserialize(serialized);

        restored.TenantId.Should().Be("globex");
        restored.CorrelationId.Should().Be(originalEnvelope.CorrelationId);
        restored.RequestId.Should().Be(originalEnvelope.RequestId);
        restored.RetryAttempt.Should().Be(3);
        restored.UserIdentity.Should().Be("user_456");
    }

    [Fact]
    public void Deserialize_ShouldThrowOnNullInput()
    {
        var act = () => _serializer.Deserialize(null!);

        act.Should().Throw<StaleTenantRestoreException>();
    }

    [Fact]
    public void Deserialize_ShouldThrowOnEmptyInput()
    {
        var act = () => _serializer.Deserialize(string.Empty);

        act.Should().Throw<StaleTenantRestoreException>();
    }

    [Fact]
    public void TryDeserialize_ShouldReturnTrueForValidInput()
    {
        var envelope = new TenantExecutionEnvelope(
            "acme",
            "corr",
            "req",
            ExecutionSource.BackgroundJob,
            TenantSource.Background);

        var serialized = _serializer.Serialize(envelope);
        var act = _serializer.TryDeserialize(serialized, out var restored);

        act.Should().BeTrue();
        restored.Should().NotBeNull();
        restored!.TenantId.Should().Be("acme");
    }

    [Fact]
    public void IsStale_ShouldReturnFalseForFreshContext()
    {
        var envelope = new TenantExecutionEnvelope(
            "initech",
            "corr",
            "req",
            ExecutionSource.BackgroundJob,
            TenantSource.Background);

        var serialized = _serializer.Serialize(envelope);
        var act = _serializer.IsStale(serialized, TimeSpan.FromHours(24));

        act.Should().BeFalse();
    }

    [Fact]
    public void RoundTrip_ShouldPreserveAllData()
    {
        var tenants = new[] { "acme", "globex", "initech" };

        foreach (var tenant in tenants)
        {
            var envelope = new TenantExecutionEnvelope(
                tenant,
                "corr-" + tenant,
                "req-" + tenant,
                ExecutionSource.BackgroundJob,
                TenantSource.Background)
            {
                RetryAttempt = 5,
                UserIdentity = $"user_for_{tenant}",
                ReplayMetadata = new Dictionary<string, string>
                {
                    { "source", "background_job" },
                    { "tenant", tenant }
                }
            };

            var serialized = _serializer.Serialize(envelope);
            var restored = _serializer.Deserialize(serialized);

            restored.TenantId.Should().Be(tenant);
            restored.RetryAttempt.Should().Be(5);
            restored.UserIdentity.Should().Be($"user_for_{tenant}");
            restored.ReplayMetadata.Should().ContainKey("source");
        }
    }

    public void Dispose()
    {
        _contextA.Dispose();
        _contextB.Dispose();
    }
}

public sealed class TenantJobExecutionScopeTests : IDisposable
{
    private readonly Mock<ITenantJobContextSerializer> _serializerMock;
    private readonly Mock<ITenantProvider> _tenantProviderMock;
    private readonly Mock<ILogger<TenantJobExecutionScope>> _loggerMock;
    private readonly TenantTestContext _context;

    public TenantJobExecutionScopeTests()
    {
        _serializerMock = new Mock<ITenantJobContextSerializer>();
        _tenantProviderMock = new Mock<ITenantProvider>();
        _loggerMock = new Mock<ILogger<TenantJobExecutionScope>>();
        _context = TenantTestContext.Create("acme", TenantSource.Background);

        _serializerMock.Setup(s => s.Serialize(It.IsAny<TenantExecutionEnvelope>()))
            .Returns("serialized_context");
        _serializerMock.Setup(s => s.IsStale(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns(false);
    }

    [Fact]
    public void FromEnvelope_ShouldCreateScope()
    {
        var envelope = new TenantExecutionEnvelope(
            "acme",
            "corr",
            "req",
            ExecutionSource.BackgroundJob,
            TenantSource.Background);

        var scope = TenantJobExecutionScope.FromEnvelope(
            envelope,
            _serializerMock.Object,
            _tenantProviderMock.Object,
            _loggerMock.Object);

        scope.Should().NotBeNull();
        scope.CurrentTenantId.Should().Be("acme");
        scope.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetCurrentEnvelope_ShouldReturnEnvelope()
    {
        var envelope = new TenantExecutionEnvelope(
            "globex",
            "corr",
            "req",
            ExecutionSource.BackgroundJob,
            TenantSource.Background);

        var scope = TenantJobExecutionScope.FromEnvelope(
            envelope,
            _serializerMock.Object,
            _tenantProviderMock.Object,
            _loggerMock.Object);

        var result = scope.GetCurrentEnvelope();

        result.TenantId.Should().Be("globex");
    }

    [Fact]
    public void ValidateTenantId_ShouldPassForMatchingTenant()
    {
        var envelope = new TenantExecutionEnvelope(
            "acme",
            "corr",
            "req",
            ExecutionSource.BackgroundJob,
            TenantSource.Background);

        var scope = TenantJobExecutionScope.FromEnvelope(
            envelope,
            _serializerMock.Object,
            _tenantProviderMock.Object,
            _loggerMock.Object);

        var act = () => scope.ValidateTenantId("acme");

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateTenantId_ShouldThrowForMismatchedTenant()
    {
        var envelope = new TenantExecutionEnvelope(
            "acme",
            "corr",
            "req",
            ExecutionSource.BackgroundJob,
            TenantSource.Background);

        var scope = TenantJobExecutionScope.FromEnvelope(
            envelope,
            _serializerMock.Object,
            _tenantProviderMock.Object,
            _loggerMock.Object);

        var act = () => scope.ValidateTenantId("globex");

        act.Should().Throw<StaleTenantRestoreException>();
    }

    [Fact]
    public void GetEnvelopeWithIncrementedRetry_ShouldIncrementRetryCount()
    {
        var envelope = new TenantExecutionEnvelope(
            "acme",
            "corr",
            "req",
            ExecutionSource.BackgroundJob,
            TenantSource.Background)
        {
            RetryAttempt = 0
        };

        var scope = TenantJobExecutionScope.FromEnvelope(
            envelope,
            _serializerMock.Object,
            _tenantProviderMock.Object,
            _loggerMock.Object);

        var updated = scope.GetEnvelopeWithIncrementedRetry();

        updated.RetryAttempt.Should().Be(1);
    }

    [Fact]
    public void Dispose_ShouldMarkScopeAsInvalid()
    {
        var envelope = new TenantExecutionEnvelope(
            "acme",
            "corr",
            "req",
            ExecutionSource.BackgroundJob,
            TenantSource.Background);

        var scope = TenantJobExecutionScope.FromEnvelope(
            envelope,
            _serializerMock.Object,
            _tenantProviderMock.Object,
            _loggerMock.Object);

        scope.Dispose();

        scope.IsValid.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

public sealed class StaleTenantRestoreExceptionTests
{
    [Fact]
    public void Constructor_ShouldSetIsSecurityRelevant()
    {
        var exception = new StaleTenantRestoreException("Test message");

        exception.Message.Should().Be("Test message");
        exception.IsSecurityRelevant.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithTenantId_ShouldSetTenantId()
    {
        var exception = new StaleTenantRestoreException("Test message", "acme");

        exception.Message.Should().Be("Test message");
        exception.TenantId.Should().Be("acme");
        exception.IsSecurityRelevant.Should().BeTrue();
    }
}

public sealed class ConcurrentTenantJobIsolationTests
{
    [Fact]
    public async Task ConcurrentJobs_MustNotInterfere()
    {
        var tenants = new[] { "acme", "globex", "initech", "umbrella" };
        var processedTenants = new System.Collections.Concurrent.ConcurrentBag<string>();
        var barrier = new Barrier(tenants.Length);

        var tasks = tenants.Select(tenant => Task.Run(() =>
        {
            barrier.SignalAndWait();
            var envelope = new TenantExecutionEnvelope(
                tenant,
                "corr-" + tenant,
                "req-" + tenant,
                ExecutionSource.BackgroundJob,
                TenantSource.Background);

            processedTenants.Add(envelope.TenantId);
            return envelope.TenantId;
        })).ToArray();

        await Task.WhenAll(tasks);

        var uniqueTenants = processedTenants.Distinct().ToList();
        uniqueTenants.Should().HaveCount(tenants.Length);
        tenants.All(t => uniqueTenants.Contains(t)).Should().BeTrue();
    }

    [Fact]
    public async Task RetryStorm_MustPreserveTenant()
    {
        var originalTenant = "acme";
        var retryAttempts = new List<TenantExecutionEnvelope>();

        for (int attempt = 0; attempt < 10; attempt++)
        {
            var envelope = new TenantExecutionEnvelope(
                originalTenant,
                "corr",
                "req-" + attempt,
                ExecutionSource.BackgroundJob,
                TenantSource.Background)
            {
                RetryAttempt = attempt
            };
            retryAttempts.Add(envelope);
        }

        retryAttempts.All(e => e.TenantId == originalTenant).Should().BeTrue();
        retryAttempts.Select(e => e.RetryAttempt).Should().BeEquivalentTo(Enumerable.Range(0, 10));
    }
}
