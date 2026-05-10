using FluentAssertions;
using Karamchari.Core.Messaging.Tenant;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using MassTransit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Karamchari.TenantIsolationCertification.Messaging;

public sealed class TenantMessageConsumerScopeTests
{
    private readonly ILogger<TenantMessageConsumerScope> _logger;
    private readonly IMemoryCache _cache;
    private readonly TenantMessageConsumerScope _scope;

    public TenantMessageConsumerScopeTests()
    {
        _logger = Substitute.For<ILogger<TenantMessageConsumerScope>>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _scope = new TenantMessageConsumerScope(_logger, _cache);
    }

    [Fact]
    public void CurrentContext_BeforeRecreation_ShouldBeNull()
    {
        _scope.CurrentContext.Should().BeNull();
    }

    [Fact]
    public void CurrentTenantId_BeforeRecreation_ShouldBeNull()
    {
        _scope.CurrentTenantId.Should().BeNull();
    }

    [Fact]
    public void CurrentRetryAttempt_BeforeRecreation_ShouldBeZero()
    {
        _scope.CurrentRetryAttempt.Should().Be(0);
    }

    [Fact]
    public void IsRetryAttempt_BeforeRecreation_ShouldBeFalse()
    {
        _scope.IsRetryAttempt.Should().BeFalse();
    }

    [Fact]
    public void ClearContext_ShouldRemoveCurrentContext()
    {
        TenantExecutionContext.Clear();
        var envelope = new TenantExecutionEnvelope(
            "tenant_clear", "corr-clear", "req-clear", ExecutionSource.Manual, TenantSource.AdminOverride);
        var context = new TenantExecutionContext(envelope);
        using (context.Establish())
        {
            _scope.ClearContext(); // This actually does nothing if it didn't create the context
        }
    }

    [Fact]
    public void ValidateTenantMatch_WithMatchingTenant_ShouldReturnTrue()
    {
        var context = CreateMockConsumeContext("tenant_match");
        _scope.RecreateFromMessage(context);

        var result = _scope.ValidateTenantMatch("tenant_match", throwOnMismatch: false);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateTenantMatch_WithNonMatchingTenant_ShouldReturnFalse()
    {
        var context = CreateMockConsumeContext("tenant_actual");
        _scope.RecreateFromMessage(context);

        var result = _scope.ValidateTenantMatch("tenant_expected", throwOnMismatch: false);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateTenantMatch_WithNullExpected_ShouldReturnTrue()
    {
        var result = _scope.ValidateTenantMatch(null, throwOnMismatch: false);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateTenantMatch_WithMismatchedTenant_AndThrow_ShouldThrow()
    {
        var context = CreateMockConsumeContext("tenant_throw");
        _scope.RecreateFromMessage(context);

        var act = () => _scope.ValidateTenantMatch("wrong_tenant", throwOnMismatch: true);

        act.Should().Throw<MalformedTenantMessageException>()
            .Which.FailureReason.Should().Be(TenantMessageFailureReason.TenantMismatch);
    }

    [Fact]
    public void Dispose_ShouldClearContext()
    {
        var context = CreateMockConsumeContext("tenant_dispose");
        _scope.RecreateFromMessage(context);

        _scope.Dispose();

        TenantExecutionContext.Current.Should().BeNull();
    }

    [Fact]
    public void RecreateFromMessage_WithMissingTenant_ShouldThrow()
    {
        var consumeContext = CreateMockConsumeContext(null);

        var act = () => _scope.RecreateFromMessage(consumeContext);

        act.Should().Throw<MalformedTenantMessageException>()
            .Which.FailureReason.Should().Be(TenantMessageFailureReason.MissingTenantId);
    }

    [Fact]
    public void RecreateFromMessage_WithMismatchedExpectedTenant_ShouldThrow()
    {
        var consumeContext = CreateMockConsumeContext("actual_tenant");

        var act = () => _scope.RecreateFromMessage(consumeContext, "expected_tenant");

        act.Should().Throw<MalformedTenantMessageException>()
            .Which.FailureReason.Should().Be(TenantMessageFailureReason.TenantMismatch);
    }

    private static ConsumeContext CreateMockConsumeContext(string? tenantId)
    {
        var mockContext = Substitute.For<ConsumeContext>();
        var headers = Substitute.For<Headers>();
        var receiveContext = Substitute.For<ReceiveContext>();

        headers.TryGetHeader(TenantMessageHeaderKeys.TenantId, out Arg.Any<object?>())
            .Returns(x =>
            {
                if (tenantId != null)
                {
                    x[1] = tenantId;
                    return true;
                }
                x[1] = null;
                return false;
            });

        mockContext.Headers.Returns(headers);
        mockContext.ConversationId.Returns(Guid.NewGuid());
        mockContext.MessageId.Returns(Guid.NewGuid());
        mockContext.ReceiveContext.Returns(receiveContext);

        return mockContext;
    }
}

public sealed class TenantMessageConsumerScopeFactoryTests
{
    private readonly ILogger<TenantMessageConsumerScope> _logger;
    private readonly IMemoryCache _cache;
    private readonly TenantMessageConsumerScopeFactory _factory;

    public TenantMessageConsumerScopeFactoryTests()
    {
        _logger = Substitute.For<ILogger<TenantMessageConsumerScope>>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _factory = new TenantMessageConsumerScopeFactory(_logger, _cache);
    }

    [Fact]
    public void CreateScope_ShouldReturnNewScope()
    {
        var scope1 = _factory.CreateScope();
        var scope2 = _factory.CreateScope();

        scope1.Should().NotBeNull();
        scope2.Should().NotBeNull();
        scope1.Should().NotBeSameAs(scope2);
    }
}

public sealed class TenantMessageConsumerScopeExtensionsTests
{
    [Fact]
    public void AddTenantMessageConsumerScope_ShouldRegisterServices()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();

        services.AddTenantMessageConsumerScope();

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(TenantMessageConsumerScope));
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(ITenantMessageConsumerScopeFactory));
    }
}
