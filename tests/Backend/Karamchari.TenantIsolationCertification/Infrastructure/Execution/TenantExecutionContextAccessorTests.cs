using FluentAssertions;
using Karamchari.Core.Multitenancy.Execution;
using Xunit;

namespace Karamchari.TenantIsolationCertification.Infrastructure.Execution;

public class TenantExecutionContextAccessorTests : IDisposable
{
    private readonly TenantExecutionContextAccessor _accessor;

    public TenantExecutionContextAccessorTests()
    {
        _accessor = new TenantExecutionContextAccessor();
    }

    public void Dispose()
    {
        _accessor.Clear();
    }

    [Fact]
    public void Current_WhenNoContextSet_ShouldReturnNull()
    {
        _accessor.Current.Should().BeNull();
    }

    [Fact]
    public void GetCurrent_WhenNoContextSet_ShouldThrowInvalidOperationException()
    {
        var act = () => _accessor.GetCurrent();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*no tenant execution context*");
    }

    [Fact]
    public void SetCurrent_ShouldUpdateCurrentContext()
    {
        var envelope = new TenantExecutionEnvelope(
            "acme",
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.HttpRequest);

        _accessor.SetCurrent(envelope);

        _accessor.Current.Should().Be(envelope);
        _accessor.GetCurrent().Should().Be(envelope);
    }

    [Fact]
    public void SetCurrent_WithNull_ShouldThrowArgumentNullException()
    {
        var act = () => _accessor.SetCurrent(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Clear_ShouldRemoveCurrentContext()
    {
        var envelope = new TenantExecutionEnvelope(
            "acme",
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.HttpRequest);

        _accessor.SetCurrent(envelope);
        _accessor.Clear();

        _accessor.Current.Should().BeNull();
    }

    [Fact]
    public void SetCurrent_ShouldIncrementNestingDepth()
    {
        var envelope = new TenantExecutionEnvelope(
            "acme",
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.HttpRequest);

        _accessor.SetCurrent(envelope);
        _accessor.SetCurrent(envelope);
        _accessor.SetCurrent(envelope);

        _accessor.NestingDepth.Should().Be(3);
    }

    [Fact]
    public void Clear_ShouldDecrementNestingDepth()
    {
        var envelope = new TenantExecutionEnvelope(
            "acme",
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.HttpRequest);

        _accessor.SetCurrent(envelope);
        _accessor.SetCurrent(envelope);
        _accessor.Clear();

        _accessor.NestingDepth.Should().Be(1);
    }

    [Fact]
    public async Task SetCurrent_ShouldPropagateAcrossAsyncBoundary()
    {
        var envelope = new TenantExecutionEnvelope(
            "acme",
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.HttpRequest);

        _accessor.SetCurrent(envelope);

        await Task.Delay(1);

        _accessor.Current.Should().Be(envelope);
    }

    [Fact]
    public async Task Clear_ShouldNotPropagateAcrossAsyncBoundary()
    {
        var envelope = new TenantExecutionEnvelope(
            "acme",
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.HttpRequest);

        _accessor.SetCurrent(envelope);

        _accessor.Clear();

        await Task.Delay(1);

        _accessor.Current.Should().BeNull();
    }

    [Fact]
    public async Task ParallelContexts_ShouldMaintainSeparateContexts()
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
            ExecutionSource.MessageConsumer);

        _accessor.SetCurrent(envelope1);

        var task1 = Task.Run(() =>
        {
            _accessor.SetCurrent(envelope2);
            return _accessor.Current;
        });

        var task2 = Task.Run(() =>
        {
            return _accessor.Current;
        });

        await Task.WhenAll(task1, task2);

        var result1 = await task1;
        var result2 = await task2;

        result1.Should().Be(envelope2);
        result2.Should().Be(envelope1);
    }
}
