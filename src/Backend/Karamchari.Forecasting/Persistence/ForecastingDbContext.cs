using Karamchari.Forecasting.Domain;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Forecasting.Persistence;

public sealed class ForecastingDbContext : DbContext
{
    public ForecastingDbContext(DbContextOptions<ForecastingDbContext> options) : base(options) { }

    public DbSet<ForecastMetrics> ForecastMetrics => Set<ForecastMetrics>();
    public DbSet<ClientPaymentProfile> ClientPaymentProfiles => Set<ClientPaymentProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
