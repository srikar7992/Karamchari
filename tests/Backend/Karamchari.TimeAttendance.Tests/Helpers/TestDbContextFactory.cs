// -----------------------------------------------------------------------
// <copyright file="TestDbContextFactory.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Tests.Helpers;

using Karamchari.Core.Multitenancy;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Creates an EF Core InMemory DbContext for unit/integration tests that need
/// the full EF object graph without a real SQL Server.
/// Real RowVersion concurrency and SQL-level RLS require Testcontainers (separate suite).
/// </summary>
internal static class TestDbContextFactory
{
    public static TimeAttendanceDbContext Create(string tenantId = "test-tenant")
    {
        var options = new DbContextOptionsBuilder<TimeAttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // isolated per test
            .EnableSensitiveDataLogging()
            .LogTo(_ => { }, LogLevel.None) // suppress test output noise
            .Options;

        return new TimeAttendanceDbContext(options, new FixedTenantProvider(tenantId));
    }

    public static TimeAttendanceDbContext CreateShared(string dbName, string tenantId = "test-tenant")
    {
        // Shared database for multi-instance tests (projection rebuild idempotency etc.)
        var options = new DbContextOptionsBuilder<TimeAttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .EnableSensitiveDataLogging()
            .LogTo(_ => { }, LogLevel.None)
            .Options;

        return new TimeAttendanceDbContext(options, new FixedTenantProvider(tenantId));
    }
}

/// <summary>Returns a fixed tenant ID. Used in tests instead of HttpTenantProvider.</summary>
internal sealed class FixedTenantProvider(string tenantId) : ITenantProvider
{
    private readonly TenantExecutionEnvelope _envelope = new(
        tenantId, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(),
        ExecutionSource.HttpRequest, TenantSource.JwtClaim);

    public string GetCurrentTenantId() => tenantId;

    public bool TryGetCurrentTenantId([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? tid)
    {
        tid = tenantId;
        return true;
    }

    public TenantExecutionEnvelope GetTenant() => _envelope;

    public bool TryGetTenant(out TenantExecutionEnvelope? tenant)
    {
        tenant = _envelope;
        return true;
    }
}
