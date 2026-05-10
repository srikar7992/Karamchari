using FluentAssertions;
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
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.HttpRequest);

        using var scope = new TenantExecutionScope(_accessor, childEnvelope);

        _accessor.Current.Should().Be(childEnvelope);
    }

    [Fact]
    public void Constructor_WithoutChildContext_ShouldPreserveCurrentContext()
    {
        var envelope = new TenantExecutionEnvelope(
            "parent",
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.HttpRequest);
        _accessor.SetCurrent(envelope);

        using var scope = new TenantExecutionScope(_accessor);

        _accessor.Current.Should().Be(envelope);
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
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.HttpRequest);
        _accessor.SetCurrent(parentEnvelope);

        var childEnvelope = new TenantExecutionEnvelope(
            "child",
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.MessageConsumer);
        using (var scope = new TenantExecutionScope(_accessor, childEnvelope))
        {
            _accessor.Current.Should().Be(childEnvelope);
        }

        _accessor.Current.Should().Be(parentEnvelope);
    }

    [Fact]
    public void Dispose_WhenNoParentContext_ShouldClearContext()
    {
        var childEnvelope = new TenantExecutionEnvelope(
            "child",
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.MessageConsumer);

        using (var scope = new TenantExecutionScope(_accessor, childEnvelope))
        {
            _accessor.Current.Should().Be(childEnvelope);
        }

        _accessor.Current.Should().BeNull();
    }

    [Fact]
    public void Dispose_WhenNoChildContext_ShouldRestoreParent()
    {
        var parentEnvelope = new TenantExecutionEnvelope(
            "parent",
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.HttpRequest);
        _accessor.SetCurrent(parentEnvelope);

        using (var scope = new TenantExecutionScope(_accessor))
        {
            _accessor.Current.Should().Be(parentEnvelope);
        }

        _accessor.Current.Should().Be(parentEnvelope);
    }

    [Fact]
    public void Depth_ShouldTrackNestingLevel()
    {
        var envelope = new TenantExecutionEnvelope(
            "acme",
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.HttpRequest);

        _accessor.SetCurrent(envelope);

        using (var scope1 = new TenantExecutionScope(_accessor))
        {
            scope1.Depth.Should().Be(1);

            using (var scope2 = new TenantExecutionScope(_accessor))
            {
                scope2.Depth.Should().Be(2);

                using (var scope3 = new TenantExecutionScope(_accessor))
                {
                    scope3.Depth.Should().Be(3);
                }

                scope2.Depth.Should().Be(2);
            }

            scope1.Depth.Should().Be(1);
        }
    }

    [Fact]
    public void SavedContext_ShouldReturnParentContext()
    {
        var parentEnvelope = new TenantExecutionEnvelope(
            "parent",
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.HttpRequest);
        _accessor.SetCurrent(parentEnvelope);

        using var scope = new TenantExecutionScope(_accessor);

        scope.SavedContext.Should().Be(parentEnvelope);
    }

    [Fact]
    public void SavedContext_WhenNoParent_ShouldReturnNull()
    {
        using var scope = new TenantExecutionScope(_accessor);

        scope.SavedContext.Should().BeNull();
    }

    [Fact]
    public void SavedContext_WithChild_ShouldReturnParentBeforeChildWasSet()
    {
        var parentEnvelope = new TenantExecutionEnvelope(
            "parent",
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.HttpRequest);
        _accessor.SetCurrent(parentEnvelope);

        var childEnvelope = new TenantExecutionEnvelope(
            "child",
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.MessageConsumer);

        TenantExecutionEnvelope? capturedParent = null;
        using var scope = new TenantExecutionScope(_accessor, childEnvelope);

        capturedParent = scope.SavedContext;

        capturedParent.Should().Be(parentEnvelope);
    }

    [Fact]
    public void Constructor_WithInvalidChildTenantId_ShouldThrowArgumentException()
    {
        var childEnvelope = new TenantExecutionEnvelope(
            "INVALID",
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.HttpRequest);

        var act = () => new TenantExecutionScope(_accessor, childEnvelope);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("tenantId");
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
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.HttpRequest);
        var envelope2 = new TenantExecutionEnvelope(
            "tenant2",
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.HttpRequest);

        _accessor.SetCurrent(envelope1);

        using (var scope1 = new TenantExecutionScope(_accessor))
        {
            using (var scope2 = new TenantExecutionScope(_accessor, envelope2))
            {
                _accessor.Current.Should().Be(envelope2);
            }
            _accessor.Current.Should().Be(envelope1);
        }
        _accessor.Current.Should().BeNull();
    }
}
