using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Forecasting.Domain;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Forecasting.Persistence;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class ForecastingDbContext : KaramchariDbContext
{
    public ForecastingDbContext(DbContextOptions<ForecastingDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider) { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<ForecastMetrics> ForecastMetrics => Set<ForecastMetrics>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<ClientPaymentProfile> ClientPaymentProfiles => Set<ClientPaymentProfile>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.Entity<ForecastMetrics>(b =>
        {
            b.ToTable("Forecast_Metrics");
            b.HasKey(x => x.Id);
            b.Property(x => x.ProjectedRevenue).HasPrecision(18, 2);
            b.Property(x => x.ProjectedCash).HasPrecision(18, 2);
            b.Property(x => x.OutstandingAmount).HasPrecision(18, 2);
            b.Property(x => x.RiskAmount).HasPrecision(18, 2);

            b.HasIndex(x => new { x.TenantId, x.Date });
        });

        modelBuilder.Entity<ClientPaymentProfile>(b =>
        {
            b.ToTable("Forecast_ClientPaymentProfiles");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.ClientId }).IsUnique();
        });
    }
}
