// -----------------------------------------------------------------------
// <copyright file="PSADbContext.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.PSA.Domain;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.PSA.Persistence;

/// <summary>
/// Database context for the PSA bounded context.
/// Tenant isolation is enforced via Row-Level Security (same pattern as Payroll).
/// </summary>
public sealed class PSADbContext : KaramchariDbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PSADbContext"/> class.
    /// </summary>
    public PSADbContext(DbContextOptions<PSADbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    /// <summary>Gets the clients set.</summary>
    public DbSet<Client> Clients => Set<Client>();

    /// <summary>Gets the client projects set.</summary>
    public DbSet<ClientProject> Projects => Set<ClientProject>();

    /// <summary>Gets the project resource (rate card) assignments.</summary>
    public DbSet<ProjectResource> ProjectResources => Set<ProjectResource>();

    /// <summary>Gets the unbilled revenue ledger.</summary>
    public DbSet<UnbilledRevenue> UnbilledRevenue => Set<UnbilledRevenue>();

    /// <summary>Gets the generated invoices.</summary>
    public DbSet<Invoice> Invoices => Set<Invoice>();

    /// <summary>Gets the employee cost snapshots.</summary>
    public DbSet<EmployeeCostSnapshot> EmployeeCostSnapshots => Set<EmployeeCostSnapshot>();

    /// <summary>Gets the raw project profit ledger.</summary>
    public DbSet<ProjectProfitLedger> ProfitLedger => Set<ProjectProfitLedger>();

    /// <summary>Gets the pre-aggregated monthly metrics (dashboard read path).</summary>
    public DbSet<ProjectMonthlyMetrics> MonthlyMetrics => Set<ProjectMonthlyMetrics>();

    /// <inheritdoc/>
    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);


        modelBuilder.Entity<Client>(b =>
        {
            b.ToTable("PSA_Clients");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(256).IsRequired();
            b.Property(x => x.Gstin).HasMaxLength(15);
            b.Property(x => x.Currency).HasMaxLength(3);
            b.HasIndex(x => x.TenantId);
        });

        modelBuilder.Entity<ClientProject>(b =>
        {
            b.ToTable("PSA_Projects");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(256).IsRequired();
            b.Property(x => x.BillingType).HasConversion<string>();
            // Fast lookup of all projects for a client
            b.HasIndex(x => x.ClientId);
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        });

        modelBuilder.Entity<ProjectResource>(b =>
        {
            b.ToTable("PSA_ProjectResources");
            b.HasKey(x => x.Id);
            b.Property(x => x.BillableRate).HasPrecision(18, 4);
            b.Property(x => x.Currency).HasMaxLength(3);

            // Critical performance index: rate lookup is always by (employee, project, date).
            b.HasIndex(x => new { x.EmployeeId, x.ProjectId, x.EffectiveFrom });

            // Secondary index for "all resources on a project" admin queries.
            b.HasIndex(x => x.ProjectId);
        });

        modelBuilder.Entity<UnbilledRevenue>(b =>
        {
            b.ToTable("PSA_UnbilledRevenue");
            b.HasKey(x => x.Id);
            b.Property(x => x.BillableHours).HasPrecision(18, 2);
            b.Property(x => x.BillableRate).HasPrecision(18, 4);
            b.Property(x => x.Amount).HasPrecision(18, 2);
            b.Property(x => x.Currency).HasMaxLength(3);

            // DB-level idempotency: one revenue row per employee per project per day per timesheet.
            b.HasIndex(x => new { x.TimesheetId, x.EmployeeId, x.ProjectId, x.WorkDate }).IsUnique();

            // Fast lookups for invoice generation: all unbilled entries for a given project.
            b.HasIndex(x => new { x.ProjectId, x.IsInvoiced });
        });

        modelBuilder.Entity<Invoice>(b =>
        {
            b.ToTable("PSA_Invoices");
            b.HasKey(x => x.Id);
            b.Property(x => x.InvoiceNumber).HasMaxLength(64).IsRequired();
            b.Property(x => x.SubTotal).HasPrecision(18, 2);
            b.Property(x => x.Currency).HasMaxLength(3);
            b.HasIndex(x => new { x.ClientId, x.InvoiceDate });

            // Store line items as JSON for easy retrieval without joins.
            b.OwnsMany(x => x.Lines, l =>
            {
                l.ToJson();
                l.Property(x => x.Hours).HasPrecision(18, 2);
                l.Property(x => x.Rate).HasPrecision(18, 4);
                l.Property(x => x.Amount).HasPrecision(18, 2);
            });
        });

        // â”€â”€ Profitability Engine tables â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        modelBuilder.Entity<EmployeeCostSnapshot>(b =>
        {
            b.ToTable("PSA_EmployeeCostSnapshots");
            b.HasKey(x => x.Id);
            b.Property(x => x.MonthlyCost).HasPrecision(18, 2);
            b.Property(x => x.CostPerHour).HasPrecision(18, 4);
            b.Property(x => x.StandardMonthlyHours).HasPrecision(10, 2);

            // One snapshot per employee per month â€” upsert-safe.
            b.HasIndex(x => new { x.EmployeeId, x.Year, x.Month }).IsUnique();
        });

        modelBuilder.Entity<ProjectProfitLedger>(b =>
        {
            b.ToTable("PSA_ProfitLedger");
            b.HasKey(x => x.Id);
            b.Property(x => x.Hours).HasPrecision(18, 2);
            b.Property(x => x.Revenue).HasPrecision(18, 2);
            b.Property(x => x.Cost).HasPrecision(18, 2);
            b.Property(x => x.BillableRate).HasPrecision(18, 4);
            b.Property(x => x.CostPerHour).HasPrecision(18, 4);
            b.Property(x => x.Currency).HasMaxLength(3);
            // Computed columns ignored by EF (they use expression bodies)
            b.Ignore(x => x.Profit);
            b.Ignore(x => x.MarginPercent);

            // DB-level idempotency: one row per timesheet per employee per project per date.
            b.HasIndex(x => new { x.TimesheetId, x.EmployeeId, x.ProjectId, x.WorkDate }).IsUnique();

            // Fast: all profit rows for a project in a date range.
            b.HasIndex(x => new { x.ProjectId, x.WorkDate });
        });

        modelBuilder.Entity<ProjectMonthlyMetrics>(b =>
        {
            b.ToTable("PSA_ProjectMonthlyMetrics");
            b.HasKey(x => x.Id);
            b.Property(x => x.TotalRevenue).HasPrecision(18, 2);
            b.Property(x => x.TotalCost).HasPrecision(18, 2);
            b.Property(x => x.BillableHours).HasPrecision(18, 2);
            b.Property(x => x.TotalHours).HasPrecision(18, 2);
            // Computed columns ignored by EF.
            b.Ignore(x => x.TotalProfit);
            b.Ignore(x => x.MarginPercent);
            b.Ignore(x => x.UtilizationPercent);
            b.Ignore(x => x.BurnRate);

            // One row per project per month â€” upsert target.
            b.HasIndex(x => new { x.ProjectId, x.Year, x.Month }).IsUnique();

            // Fast: "all projects this year" for the dashboard landing.
            b.HasIndex(x => new { x.Year, x.Month });
        });
    }
}

