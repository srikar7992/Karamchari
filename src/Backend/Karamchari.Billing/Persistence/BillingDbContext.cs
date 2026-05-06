using Karamchari.Billing.Domain.BillableEntries;
using Karamchari.Billing.Domain.Contracts;
using Karamchari.Billing.Domain.Invoices;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Billing.Persistence;

public sealed class BillingDbContext : DbContext
{
    public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options) { }

    public DbSet<BillingContract> Contracts => Set<BillingContract>();
    public DbSet<RateCard> RateCards => Set<RateCard>();
    public DbSet<EmployeeRole> EmployeeRoles => Set<EmployeeRole>();
    public DbSet<BillableEntry> BillableEntries => Set<BillableEntry>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Karamchari.Billing.Domain.Collections.CollectionCase> CollectionCases => Set<Karamchari.Billing.Domain.Collections.CollectionCase>();
    public DbSet<Karamchari.Billing.Domain.Collections.CollectionPolicy> CollectionPolicies => Set<Karamchari.Billing.Domain.Collections.CollectionPolicy>();
    public DbSet<Karamchari.Billing.Domain.Analytics.ProcessedEventLog> ProcessedEventLogs => Set<Karamchari.Billing.Domain.Analytics.ProcessedEventLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BillingContract>(b =>
        {
            b.ToTable("Billing_Contracts");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.Currency).HasMaxLength(10);
            b.HasMany(x => x.RateCards).WithOne().HasForeignKey(x => x.ContractId);
            b.Property(x => x.RowVersion).IsRowVersion();
            
            b.HasIndex(x => new { x.TenantId, x.ProjectId });
        });

        modelBuilder.Entity<RateCard>(b =>
        {
            b.ToTable("Billing_RateCards");
            b.HasKey(x => x.Id);
            b.Property(x => x.Rate).HasPrecision(18, 2);
            b.HasIndex(x => new { x.ContractId, x.RoleId, x.EffectiveFrom });
        });

        modelBuilder.Entity<EmployeeRole>(b =>
        {
            b.ToTable("Billing_EmployeeRoles");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.ProjectId, x.EffectiveFrom });
        });

        modelBuilder.Entity<BillableEntry>(b =>
        {
            b.ToTable("Billing_BillableEntries");
            b.HasKey(x => x.Id);
            b.Property(x => x.Hours).HasPrecision(9, 2);
            b.Property(x => x.Rate).HasPrecision(18, 2);
            b.Property(x => x.Amount).HasPrecision(18, 2);
            
            b.HasIndex(x => new { x.TenantId, x.ProjectId, x.WorkDate });
            b.HasIndex(x => x.TimesheetEntryId).IsUnique().HasFilter("[IsVoided] = 0");
        });

        modelBuilder.Entity<Invoice>(b =>
        {
            b.ToTable("Billing_Invoices");
            b.HasKey(x => x.Id);
            b.Property(x => x.TotalAmount).HasPrecision(18, 2);
            b.Property(x => x.TaxAmount).HasPrecision(18, 2);
            b.OwnsMany(x => x.Lines, l =>
            {
                l.ToTable("Billing_InvoiceLines");
                l.WithOwner().HasForeignKey("InvoiceId");
                l.Property(x => x.Quantity).HasPrecision(9, 2);
                l.Property(x => x.Rate).HasPrecision(18, 2);
                l.Property(x => x.Amount).HasPrecision(18, 2);
            });
            b.HasMany(x => x.Payments).WithOne().HasForeignKey(x => x.InvoiceId);
        });

        modelBuilder.Entity<Payment>(b =>
        {
            b.ToTable("Billing_Payments");
            b.HasKey(x => x.Id);
            b.Property(x => x.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Karamchari.Billing.Domain.Collections.CollectionCase>(b =>
        {
            b.ToTable("Billing_CollectionCases");
            b.HasKey(x => x.Id);
            b.Property(x => x.RowVersion).IsRowVersion();
            b.Property(x => x.OutstandingAmount).HasPrecision(18, 2);
            b.HasIndex(x => new { x.TenantId, x.InvoiceId }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.Status, x.DaysOutstanding });
        });

        modelBuilder.Entity<Karamchari.Billing.Domain.Collections.CollectionPolicy>(b =>
        {
            b.ToTable("Billing_CollectionPolicies");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.TenantId).IsUnique();
        });

        modelBuilder.Entity<Karamchari.Billing.Domain.Analytics.ProcessedEventLog>(b =>
        {
            b.ToTable("Billing_ProcessedEventLog");
            b.HasKey(x => new { x.EventId, x.ConsumerName });
            b.HasIndex(x => new { x.EventId, x.ConsumerName }).IsUnique();
        });
    }
}
