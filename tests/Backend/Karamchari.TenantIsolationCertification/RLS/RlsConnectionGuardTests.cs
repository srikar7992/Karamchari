using FluentAssertions;
using Karamchari.Core.Persistence.Tenant;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Karamchari.TenantIsolationCertification.RLS;

public class RlsConnectionGuardTests
{
    [Fact]
    public void Acquire_SetsSessionContext()
    {
        using var connection = CreateInMemoryConnection();

        var guard = RlsConnectionGuard.Acquire(connection, "tenant-acme");

        guard.Should().NotBeNull();
    }

    [Fact]
    public void ValidateSessionContext_WhenMatches_DoesNotThrow()
    {
        using var connection = CreateInMemoryConnection();

        var guard = RlsConnectionGuard.Acquire(connection, "tenant-acme");

        var act = () => guard.ValidateSessionContext("tenant-acme");

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateSessionContext_WhenMismatched_ThrowsException()
    {
        using var connection = CreateInMemoryConnection();

        var guard = RlsConnectionGuard.Acquire(connection, "tenant-acme");

        var act = () => guard.ValidateSessionContext("tenant-other");

        act.Should().Throw<TenantSessionContextMismatchException>()
            .Which.ExpectedTenantId.Should().Be("tenant-other");
    }

    [Fact]
    public void ClearSessionContext_ClearsSessionContext()
    {
        using var connection = CreateInMemoryConnection();

        var guard = RlsConnectionGuard.Acquire(connection, "tenant-acme");
        guard.ClearSessionContext();

        using var verifyCmd = connection.CreateCommand();
        verifyCmd.CommandText = "SELECT SESSION_CONTEXT(N'TenantId') AS TenantId;";
        var result = verifyCmd.ExecuteScalar();

        result.Should().BeNull();
    }

    [Fact]
    public void Dispose_ClearsSessionContext()
    {
        using var connection = CreateInMemoryConnection();

        var guard = RlsConnectionGuard.Acquire(connection, "tenant-acme");
        guard.Dispose();

        using var verifyCmd = connection.CreateCommand();
        verifyCmd.CommandText = "SELECT SESSION_CONTEXT(N'TenantId') AS TenantId;";
        var result = verifyCmd.ExecuteScalar();

        result.Should().BeNull();
    }

    [Fact]
    public void Acquire_ClearsExistingSessionContext()
    {
        using var connection = CreateInMemoryConnection();

        using (var firstGuard = RlsConnectionGuard.Acquire(connection, "tenant-first"))
        {
            firstGuard.Dispose();
        }

        var guard = RlsConnectionGuard.Acquire(connection, "tenant-second");

        guard.ValidateSessionContext("tenant-second");
    }

    private static SqlConnection CreateInMemoryConnection()
    {
        return new SqlConnection("Server=.;Database=master;Trusted_Connection=True;TrustServerCertificate=True;");
    }
}
