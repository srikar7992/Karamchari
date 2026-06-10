// -----------------------------------------------------------------------
// <copyright file="HRDbContextFactory.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;
using Karamchari.HR.Persistence;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Karamchari.HR.Tests.Integration;

/// <summary>
/// Factory that creates isolated <see cref="HRDbContext"/> instances for in-memory integration tests.
/// Each call produces a context backed by a fresh, uniquely-named in-memory database,
/// ensuring full test isolation without requiring a real SQL Server instance.
/// </summary>
internal static class HRDbContextFactory
{
    /// <summary>
    /// Creates an <see cref="HRDbContext"/> backed by an in-memory database stamped with
    /// the supplied <paramref name="tenantId"/>.
    /// </summary>
    /// <param name="tenantId">The tenant identifier to associate with this context.</param>
    /// <param name="databaseName">
    /// Optional shared database name. Use the same name across multiple contexts to share data.
    /// Omit to get a fresh isolated database.
    /// </param>
    public static HRDbContext Create(string tenantId, string? databaseName = null)
    {
        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentTenantId().Returns(tenantId);

        var envelope = new TenantExecutionEnvelope(
            tenantId,
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.Test,
            TenantSource.AdminOverride);

        tenantProvider.GetTenant().Returns(envelope);
        tenantProvider.TryGetCurrentTenantId(out Arg.Any<string?>()).Returns(x =>
        {
            x[0] = tenantId;
            return true;
        });

        var options = new DbContextOptionsBuilder<HRDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .AddInterceptors(new HRTestStampingInterceptor(tenantProvider))
            .Options;

        return new HRDbContext(options, tenantProvider);
    }

    /// <summary>
    /// Creates a pair of <see cref="HRDbContext"/> instances that share the same underlying
    /// in-memory database but are associated with different tenant identifiers.
    /// Useful for asserting cross-tenant isolation via the global query filter.
    /// </summary>
    public static (HRDbContext TenantA, HRDbContext TenantB) CreatePair(
        string tenantAId, string tenantBId)
    {
        var sharedDbName = Guid.NewGuid().ToString();
        return (Create(tenantAId, sharedDbName), Create(tenantBId, sharedDbName));
    }
}
