// -----------------------------------------------------------------------
// <copyright file="RlsConnectionGuardTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data;
using System.Data.Common;
using FluentAssertions;
using Karamchari.Core.Persistence.Tenant;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Karamchari.TenantIsolationCertification.RLS;

public class RlsConnectionGuardTests
{
    [Fact]
    public void Acquire_SetsSessionContext()
    {
        var connection = CreateMockConnection(ConnectionState.Open);
        // Setup validation query to succeed
        var cmd = connection.CreateCommand();
        cmd.ExecuteScalar().Returns("tenant-acme");

        var guard = RlsConnectionGuard.Acquire(connection, "tenant-acme");

        guard.Should().NotBeNull();
    }

    [Fact]
    public void ValidateSessionContext_WhenMatches_DoesNotThrow()
    {
        var connection = CreateMockConnection(ConnectionState.Open);
        var cmd = connection.CreateCommand();
        cmd.ExecuteScalar().Returns("tenant-acme");

        var guard = RlsConnectionGuard.Acquire(connection, "tenant-acme");

        var act = () => guard.ValidateSessionContext("tenant-acme");

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateSessionContext_WhenMismatched_ThrowsException()
    {
        var connection = CreateMockConnection(ConnectionState.Open);
        var cmd = connection.CreateCommand();
        // First call for Acquire validation, second for test validation
        cmd.ExecuteScalar().Returns("tenant-acme", "tenant-acme");

        var guard = RlsConnectionGuard.Acquire(connection, "tenant-acme");

        // Force a mismatch for the next call
        cmd.ExecuteScalar().Returns("tenant-wrong");

        var act = () => guard.ValidateSessionContext("tenant-other");

        act.Should().Throw<TenantSessionContextMismatchException>()
            .Which.ExpectedTenantId.Should().Be("tenant-other");
    }

    [Fact]
    public void ClearSessionContext_ClearsSessionContext()
    {
        var connection = CreateMockConnection(ConnectionState.Open);
        var cmd = connection.CreateCommand();
        cmd.ExecuteScalar().Returns("tenant-acme");

        var guard = RlsConnectionGuard.Acquire(connection, "tenant-acme");
        guard.ClearSessionContext();

        // Should have received at least one NonQuery for clearing
        cmd.Received().ExecuteNonQuery();
    }

    [Fact]
    public void Dispose_ClearsSessionContext()
    {
        var connection = CreateMockConnection(ConnectionState.Open);
        var cmd = connection.CreateCommand();
        cmd.ExecuteScalar().Returns("tenant-acme");

        var guard = RlsConnectionGuard.Acquire(connection, "tenant-acme");
        guard.Dispose();

        cmd.Received().ExecuteNonQuery();
    }

    [Fact]
    public void Acquire_OnClosedConnection_ShouldThrow()
    {
        var connection = CreateMockConnection(ConnectionState.Closed);

        var act = () => RlsConnectionGuard.Acquire(connection, "tenant-acme");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*closed connection*");
    }

    private static DbConnection CreateMockConnection(ConnectionState state)
    {
        var connection = Substitute.For<DbConnection>();
        connection.State.Returns(state);

        var cmd = Substitute.For<DbCommand>();
        var param = Substitute.For<DbParameter>();
        var parameters = Substitute.For<DbParameterCollection>();

        cmd.CreateParameter().Returns(param);
        cmd.Parameters.Returns(parameters);
        connection.CreateCommand().Returns(cmd);

        return connection;
    }
}
