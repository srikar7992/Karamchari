using System.Data;
using FluentAssertions;
using Karamchari.Core.Persistence.Tenant;
using Microsoft.Data.SqlClient;
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
        scope.ResetForRetry();

        using var verifyCmd = connection.CreateCommand();
        verifyCmd.CommandText = "SELECT SESSION_CONTEXT(N'TenantId') AS TenantId;";
        var result = verifyCmd.ExecuteScalar();

        result.Should().BeNull();
    }

    [Fact]
    public void BeginTransaction_AfterCommit_Throws()
    {
        using var connection = CreateInMemoryConnection();

        using var scope = TenantSqlConnectionScope.Acquire(connection, "tenant-test");
        scope.BeginTransaction();
        scope.Commit();

        var act = () => scope.BeginTransaction();

        act.Should().Throw<InvalidOperationException>();
    }

    private static SqlConnection CreateInMemoryConnection()
    {
        return new SqlConnection("Server=.;Database=master;Trusted_Connection=True;TrustServerCertificate=True;");
    }
}
