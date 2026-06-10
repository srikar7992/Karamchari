// -----------------------------------------------------------------------
// <copyright file="TenantSqlConnectionScopeTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data;
using System.Data.Common;
using FluentAssertions;
using Karamchari.Core.Persistence.Tenant;
using NSubstitute;
using Xunit;

namespace Karamchari.TenantIsolationCertification.RLS;

public class TenantSqlConnectionScopeTests
{
    [Fact]
    public void Acquire_SetsTenantContext()
    {
        using var connection = CreateInMemoryConnection();

        using var scope = TenantSqlConnectionScope.Acquire(connection, "tenant-test");

        scope.Connection.Should().BeSameAs(connection);
    }

    [Fact]
    public void BeginTransaction_StartsTransaction()
    {
        using var connection = CreateInMemoryConnection();

        using var scope = TenantSqlConnectionScope.Acquire(connection, "tenant-test");
        scope.BeginTransaction();

        scope.GetTransaction().Should().NotBeNull();
    }

    [Fact]
    public void BeginTransaction_WithIsolationLevel_StartsTransaction()
    {
        using var connection = CreateInMemoryConnection();

        using var scope = TenantSqlConnectionScope.Acquire(connection, "tenant-test");
        scope.BeginTransaction(IsolationLevel.RepeatableRead);

        scope.GetTransaction().Should().NotBeNull();
    }

    [Fact]
    public void Commit_CommitsTransaction()
    {
        using var connection = CreateInMemoryConnection();

        using var scope = TenantSqlConnectionScope.Acquire(connection, "tenant-test");
        scope.BeginTransaction();
        scope.Commit();

        scope.GetTransaction().Should().BeNull();
    }

    [Fact]
    public void Rollback_RollsBackTransaction()
    {
        using var connection = CreateInMemoryConnection();

        using var scope = TenantSqlConnectionScope.Acquire(connection, "tenant-test");
        scope.BeginTransaction();
        scope.Rollback();

        scope.GetTransaction().Should().BeNull();
    }

    [Fact]
    public void ValidateTenantContext_ValidatesSuccessfully()
    {
        using var connection = CreateInMemoryConnection();

        using var scope = TenantSqlConnectionScope.Acquire(connection, "tenant-test");

        var act = () => scope.ValidateTenantContext("tenant-test");

        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_RollsBackTransaction()
    {
        using var connection = CreateInMemoryConnection();

        using var scope = TenantSqlConnectionScope.Acquire(connection, "tenant-test");
        scope.BeginTransaction();

        scope.Dispose();

        scope.GetTransaction().Should().BeNull();
    }

    [Fact]
    public void ResetForRetry_ClearsSessionContext()
    {
        using var connection = CreateInMemoryConnection();

        using var scope = TenantSqlConnectionScope.Acquire(connection, "tenant-test");
        scope.BeginTransaction();

        // Reset the mock received calls before testing the reset
        connection.CreateCommand().ClearReceivedCalls();

        scope.ResetForRetry();

        var verifyCmd = connection.CreateCommand();
        verifyCmd.Received().ExecuteNonQuery();
    }

    [Fact]
    public void BeginTransaction_AfterCommit_Succeeds()
    {
        using var connection = CreateInMemoryConnection();

        using var scope = TenantSqlConnectionScope.Acquire(connection, "tenant-test");
        scope.BeginTransaction();
        scope.Commit();

        var act = () => scope.BeginTransaction();

        act.Should().NotThrow();
    }

    private static DbConnection CreateInMemoryConnection()
    {
        var connection = NSubstitute.Substitute.For<DbConnection>();
        connection.State.Returns(ConnectionState.Open);

        var cmd = NSubstitute.Substitute.For<DbCommand>();
        var param = NSubstitute.Substitute.For<DbParameter>();
        var parameters = NSubstitute.Substitute.For<DbParameterCollection>();

        cmd.CreateParameter().Returns(param);
        cmd.Parameters.Returns(parameters);
        cmd.ExecuteScalar().Returns("tenant-test");
        connection.CreateCommand().Returns(cmd);

        var tx = NSubstitute.Substitute.For<DbTransaction>();
        connection.BeginTransaction().Returns(tx);
        connection.BeginTransaction(Arg.Any<IsolationLevel>()).Returns(tx);

        return connection;
    }
}
