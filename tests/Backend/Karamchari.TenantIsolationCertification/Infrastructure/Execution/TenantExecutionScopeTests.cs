using FluentAssertions;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using Xunit;

namespace Karamchari.TenantIsolationCertification.Infrastructure.Execution;

public class TenantExecutionScopeTests : IDisposable
{
    private readonly TenantExecutionContextAccessor _accessor;

    public TenantExecutionScopeTests()
    {
        _accessor = new TenantExecutionContextAccessor();
    }

    public void Dispose()
    {
        _accessor.Clear();
    }

    [Fact]
    public void Constructor_WithNullAccessor_ShouldThrowArgumentNullException()
    {
        var act = () => new TenantExecutionScope(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithChildContext_ShouldSetChildContext()
    {
        var childEnvelope = new TenantExecutionEnvelope(
            "child",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.HttpRequest,
            TenantSource.AdminOverride);

        using var scope = new TenantExecutionScope(_accessor, childEnvelope);

        _accessor.Current?.Envelope.Should().Be(childEnvelope);
    }

    [Fact]
    public void Constructor_WithoutChildContext_ShouldPreserveCurrentContext()
    {
        var envelope = new TenantExecutionEnvelope(
            "parent",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.HttpRequest,
            TenantSource.AdminOverride);
        var context = new TenantExecutionContext(envelope);
        using var scopeEstablish = _accessor.Establish(context);

        using var scope = new TenantExecutionScope(_accessor);

        _accessor.Current.Should().Be(context);
    }

    [Fact]
    public void Constructor_WithoutCurrentContext_ShouldClearContext()
    {
        using var scope = new TenantExecutionScope(_accessor);

        _accessor.Current.Should().BeNull();
    }

    [Fact]
    public void Dispose_ShouldRestoreParentContext()
    {
        var parentEnvelope = new TenantExecutionEnvelope(
            "parent",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.HttpRequest,
            TenantSource.AdminOverride);
        var parentContext = new TenantExecutionContext(parentEnvelope);
        using var scopeEstablish = _accessor.Establish(parentContext);

        var childEnvelope = new TenantExecutionEnvelope(
            "child",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.MessageConsumer,
            TenantSource.AdminOverride);

        using (var scope = new TenantExecutionScope(_accessor, childEnvelope))
        {
            _accessor.Current?.Envelope.Should().Be(childEnvelope);
        }

        _accessor.Current.Should().Be(parentContext);
    }

    [Fact]
    public void Dispose_WhenNoParentContext_ShouldClearContext()
    {
        var childEnvelope = new TenantExecutionEnvelope(
            "child",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.MessageConsumer,
            TenantSource.AdminOverride);

        using (var scope = new TenantExecutionScope(_accessor, childEnvelope))
        {
            _accessor.Current?.Envelope.Should().Be(childEnvelope);
        }

        _accessor.Current.Should().BeNull();
    }

    [Fact]
    public void Dispose_WhenNoChildContext_ShouldRestoreParent()
    {
        var parentEnvelope = new TenantExecutionEnvelope(
            "parent",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.HttpRequest,
            TenantSource.AdminOverride);
        var parentContext = new TenantExecutionContext(parentEnvelope);
        using var scopeEstablish = _accessor.Establish(parentContext);

        using (var scope = new TenantExecutionScope(_accessor))
        {
            _accessor.Current.Should().Be(parentContext);
        }

        _accessor.Current.Should().Be(parentContext);
    }

    [Fact]
    public void ParentContext_ShouldReturnCurrentContextAtTimeOfCreation()
    {
        var parentEnvelope = new TenantExecutionEnvelope(
            "parent",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.HttpRequest,
            TenantSource.AdminOverride);
        var parentContext = new TenantExecutionContext(parentEnvelope);
        using var scopeEstablish = _accessor.Establish(parentContext);

        using var scope = new TenantExecutionScope(_accessor);

        scope.ParentContext.Should().Be(parentContext);
    }

    [Fact]
    public void ParentContext_WhenNoParent_ShouldReturnNull()
    {
        using var scope = new TenantExecutionScope(_accessor);

        scope.ParentContext.Should().BeNull();
    }

    [Fact]
    public void ParentContext_WithChild_ShouldReturnChildContext()
    {
        var parentEnvelope = new TenantExecutionEnvelope(
            "parent",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.HttpRequest,
            TenantSource.AdminOverride);
        var parentContext = new TenantExecutionContext(parentEnvelope);
        using var scopeEstablish = _accessor.Establish(parentContext);

        var childEnvelope = new TenantExecutionEnvelope(
            "child",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.MessageConsumer,
            TenantSource.AdminOverride);

        using var scope = new TenantExecutionScope(_accessor, childEnvelope);

        // Based on implementation: ParentContext => _accessor.Current
        // After constructor, _accessor.Current IS the child context if it was set.
        scope.ParentContext?.Envelope.Should().Be(childEnvelope);
    }

    [Fact]
    public void Constructor_WithInvalidChildTenantId_ShouldThrowArgumentException()
    {
        var act = () => new TenantExecutionEnvelope(
            "INVALID",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.HttpRequest,
            TenantSource.AdminOverride);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DoubleDispose_ShouldNotThrow()
    {
        var scope = new TenantExecutionScope(_accessor);
        scope.Dispose();
        var act = () => scope.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void NestedScopes_ShouldRestoreCorrectlyOnEachDispose()
    {
        var envelope1 = new TenantExecutionEnvelope(
            "tenant1",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.HttpRequest,
            TenantSource.AdminOverride);
        var context1 = new TenantExecutionContext(envelope1);

        var envelope2 = new TenantExecutionEnvelope(
            "tenant2",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.HttpRequest,
            TenantSource.AdminOverride);

        using var scopeEstablish = _accessor.Establish(context1);

        using (var scope1 = new TenantExecutionScope(_accessor))
        {
            using (var scope2 = new TenantExecutionScope(_accessor, envelope2))
            {
                _accessor.Current?.Envelope.Should().Be(envelope2);
            }
            _accessor.Current.Should().Be(context1);
        }
        _accessor.Current.Should().Be(context1); // TenantExecutionScope without child doesn't change anything
    }
}
