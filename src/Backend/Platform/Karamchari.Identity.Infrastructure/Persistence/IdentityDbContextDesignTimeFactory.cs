// -----------------------------------------------------------------------
// <copyright file="IdentityDbContextDesignTimeFactory.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Karamchari.Identity.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for <see cref="IdentityDbContext"/>. Used exclusively by the EF Core
/// CLI tools (<c>dotnet ef</c>) and never resolved at runtime by the application's DI container.
/// </summary>
/// <remarks>
/// Commands (from <c>src/Backend/</c>):
/// <code>
/// dotnet ef migrations add &lt;Name&gt; --project Karamchari.Identity.Infrastructure --startup-project Karamchari.Api --context IdentityDbContext
/// </code>
/// The connection string is read from the <c>ConnectionStrings__KaramchariDb</c> environment
/// variable first, then from Karamchari.Api appsettings.
/// </remarks>
public sealed class IdentityDbContextDesignTimeFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    /// <inheritdoc/>
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Karamchari.Api"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__KaramchariDb")
            ?? config.GetConnectionString("KaramchariDb")
            ?? "Server=localhost,1433;Database=Karamchari;User Id=sa;Password=Karamchari@123;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True";

        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new IdentityDbContext(optionsBuilder.Options);
    }
}
