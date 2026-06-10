// -----------------------------------------------------------------------
// <copyright file="MigrationSafetyTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Core.Messaging.Inbox;
using Karamchari.Core.Multitenancy;
using Karamchari.Recruitment.Application.Analytics;
using Karamchari.Recruitment.Persistence;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Karamchari.Recruitment.Tests.Migration;

/// <summary>
/// Certifies that EF Core DbContexts can create their full schema against an
/// in-memory database without errors. Tests do not require a live SQL Server;
/// live execution is covered by scripts/run-migrations.ps1.
/// </summary>
public sealed class MigrationSafetyTests
{
    private const string TestTenant = "migration-test-tenant";

    private static RecruitmentDbContext BuildRecruitmentContext(string? dbName = null)
    {
        var provider = Substitute.For<ITenantProvider>();
        provider.GetCurrentTenantId().Returns(TestTenant);

        var options = new DbContextOptionsBuilder<RecruitmentDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .AddInterceptors(new RecruitmentTestStampingInterceptor(provider))
            .Options;

        return new RecruitmentDbContext(options, provider);
    }

    private static InboxDbContext BuildInboxContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<InboxDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new InboxDbContext(options);
    }

    [Fact]
    public async Task RecruitmentSchemaAppliesWithoutError()
    {
        await using var context = BuildRecruitmentContext();
        var created = await context.Database.EnsureCreatedAsync();
        created.Should().BeTrue("RecruitmentDbContext schema should be created fresh");
    }

    [Fact]
    public async Task AnalyticsReadModelTableIsQueryableAfterSchemaCreation()
    {
        await using var context = BuildRecruitmentContext();
        await context.Database.EnsureCreatedAsync();

        var results = await context.AnalyticsReadModels.ToListAsync();
        results.Should().BeEmpty("no rows inserted, but the table must exist");

        var entityType = context.Model.FindEntityType(typeof(AnalyticsReadModel));
        entityType.Should().NotBeNull("AnalyticsReadModel must be registered in EF model");

        var tableName = entityType!.GetTableName();
        tableName.Should().Be("Recruitment_AnalyticsReadModels",
            "migration must map AnalyticsReadModel to Recruitment_AnalyticsReadModels");
    }

    [Fact]
    public async Task InboxMessagesTableIsQueryableAfterSchemaCreation()
    {
        await using var context = BuildInboxContext();
        await context.Database.EnsureCreatedAsync();

        var results = await context.InboxMessages.ToListAsync();
        results.Should().BeEmpty("no messages processed, but the table must exist");

        var entityType = context.Model.FindEntityType(typeof(InboxMessage));
        entityType.Should().NotBeNull("InboxMessage must be registered in InboxDbContext model");

        var tableName = entityType!.GetTableName();
        tableName.Should().Be("InboxMessages",
            "table name must match what InboxService expects");
    }
}
