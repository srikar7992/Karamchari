// -----------------------------------------------------------------------
// <copyright file="TenantExecutionContextAccessorTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Core.Multitenancy;
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
    public void Establish_ShouldUpdateCurrentContext()
    {
        var envelope = new TenantExecutionEnvelope(
            "acme",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.HttpRequest,
            TenantSource.AdminOverride);
        var context = new TenantExecutionContext(envelope);

        using var scope = _accessor.Establish(context);

        _accessor.Current.Should().Be(context);
        _accessor.GetCurrent().Should().Be(context);
        _accessor.Current?.Envelope.Should().Be(envelope);
    }

    [Fact]
    public void Establish_WithNull_ShouldThrowArgumentNullException()
    {
        var act = () => _accessor.Establish(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Clear_ShouldRemoveCurrentContext()
    {
        var envelope = new TenantExecutionEnvelope(
            "acme",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.HttpRequest,
            TenantSource.AdminOverride);
        var context = new TenantExecutionContext(envelope);

        using (var scope = _accessor.Establish(context))
        {
            _accessor.Current.Should().NotBeNull();
        }

        _accessor.Clear();

        _accessor.Current.Should().BeNull();
    }

    [Fact]
    public async Task Establish_ShouldPropagateAcrossAsyncBoundary()
    {
        var envelope = new TenantExecutionEnvelope(
            "acme",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.HttpRequest,
            TenantSource.AdminOverride);
        var context = new TenantExecutionContext(envelope);

        using var scope = _accessor.Establish(context);

        await Task.Delay(1);

        _accessor.Current.Should().Be(context);
    }

    [Fact]
    public async Task Clear_ShouldNotPropagateAcrossAsyncBoundary()
    {
        var envelope = new TenantExecutionEnvelope(
            "acme",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.HttpRequest,
            TenantSource.AdminOverride);
        var context = new TenantExecutionContext(envelope);

        using (var scope = _accessor.Establish(context))
        {
            _accessor.Clear();
        }

        await Task.Delay(1);

        _accessor.Current.Should().BeNull();
    }

    [Fact]
    public async Task ParallelContexts_ShouldMaintainSeparateContexts()
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
            ExecutionSource.MessageConsumer,
            TenantSource.AdminOverride);
        var context2 = new TenantExecutionContext(envelope2);

        using var scope1 = _accessor.Establish(context1);

        var task1 = Task.Run(() =>
        {
            using var scope2 = _accessor.Establish(context2);
            return _accessor.Current;
        });

        var task2 = Task.Run(() =>
        {
            return _accessor.Current;
        });

        await Task.WhenAll(task1, task2);

        var result1 = await task1;
        var result2 = await task2;

        result1.Should().Be(context2);
        result2.Should().Be(context1);
    }
}
