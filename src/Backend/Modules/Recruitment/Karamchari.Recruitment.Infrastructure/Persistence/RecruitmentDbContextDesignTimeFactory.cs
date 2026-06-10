// -----------------------------------------------------------------------
// <copyright file="RecruitmentDbContextDesignTimeFactory.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Karamchari.Recruitment.Persistence;

public sealed class RecruitmentDbContextDesignTimeFactory : IDesignTimeDbContextFactory<RecruitmentDbContext>
{
    public RecruitmentDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<RecruitmentDbContext>();
        var connectionString = configuration.GetConnectionString("KaramchariDb");
        optionsBuilder.UseSqlServer(connectionString);

        return new RecruitmentDbContext(optionsBuilder.Options, new DesignTimeTenantProvider());
    }

    private sealed class DesignTimeTenantProvider : ITenantProvider
    {
        public string GetCurrentTenantId() => "design-time";
        public bool TryGetCurrentTenantId(out string tenantId) { tenantId = "design-time"; return true; }
        public TenantExecutionEnvelope GetTenant() => throw new NotSupportedException();
        public bool TryGetTenant(out TenantExecutionEnvelope? envelope) { envelope = null; return false; }
        public void SetTenant(string tenantId) => throw new NotSupportedException();
    }
}
