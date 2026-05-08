using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Karamchari.Core.Messaging.Outbox;

/// <summary>
/// Design-time factory for <see cref="OutboxRelayDbContext"/>.
/// Used exclusively by the EF Core CLI tools (<c>dotnet ef</c>) — never resolved
/// at runtime by the application's DI container.
/// <para>
/// <b>Commands (from <c>src/Backend/</c>)</b>
/// <code>
/// dotnet ef migrations add &lt;Name&gt; --context OutboxRelayDbContext --project Karamchari.Core --startup-project Karamchari.Api
/// dotnet ef database update            --context OutboxRelayDbContext --project Karamchari.Core --startup-project Karamchari.Api
/// </code>
/// </para>
/// </summary>
public sealed class OutboxRelayDbContextDesignTimeFactory : IDesignTimeDbContextFactory<OutboxRelayDbContext>
{
    /// <inheritdoc/>
    public OutboxRelayDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Karamchari.Api"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        var connectionString = config.GetConnectionString("KaramchariDb")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:KaramchariDb not found in Karamchari.Api/appsettings.Development.json.");

        var options = new DbContextOptionsBuilder<OutboxRelayDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new OutboxRelayDbContext(options);
    }
}
