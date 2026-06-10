// -----------------------------------------------------------------------
// <copyright file="RecruitmentIntegrationTestBase.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;
using Karamchari.Recruitment.Persistence;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Karamchari.Recruitment.Tests;

public abstract class RecruitmentIntegrationTestBase : IDisposable
{
    protected RecruitmentDbContext DbContext { get; }
    protected ITenantProvider TenantProvider { get; } = Substitute.For<ITenantProvider>();
    protected const string TestTenantId = "test-tenant";

    protected RecruitmentIntegrationTestBase()
    {
        TenantProvider.GetCurrentTenantId().Returns(TestTenantId);

        var envelope = new TenantExecutionEnvelope(
            TestTenantId,
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.Test,
            TenantSource.AdminOverride);

        TenantProvider.GetTenant().Returns(envelope);

        var options = new DbContextOptionsBuilder<RecruitmentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new RecruitmentTestStampingInterceptor(TenantProvider))
            .Options;

        DbContext = new RecruitmentDbContext(options, TenantProvider);
    }

    public void Dispose()
    {
        DbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
