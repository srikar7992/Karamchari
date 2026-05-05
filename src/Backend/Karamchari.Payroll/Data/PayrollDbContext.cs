using Karamchari.Core.Multitenancy;
using Karamchari.Payroll.Domain;
using Karamchari.Payroll.Domain.SalaryStructures;
using Karamchari.Payroll.Domain.Statutory;
using Karamchari.Payroll.StateMachines;
using Karamchari.Payroll.Domain.Compliance;
using Microsoft.EntityFrameworkCore;

using Karamchari.Core.Persistence;

namespace Karamchari.Payroll.Data;

/// <summary>
/// Database context for the Payroll bounded context.
/// </summary>
public class PayrollDbContext : KaramchariDbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PayrollDbContext"/> class.
    /// </summary>
    /// <param name="options">The context options.</param>
    /// <param name="tenantProvider">The tenant provider.</param>
    public PayrollDbContext(DbContextOptions<PayrollDbContext> options, ITenantProvider tenantProvider) 
        : base(options, tenantProvider)
    {
    }

    /// <summary>
    /// Gets the payroll profiles set.
    /// </summary>
    public DbSet<PayrollProfile> PayrollProfiles => Set<PayrollProfile>();
    /// <summary>
    /// Gets the payroll run states set (Saga state).
    /// </summary>
    public DbSet<PayrollRunState> PayrollRunStates => Set<PayrollRunState>();
    /// <summary>
    /// Gets the payroll deductions set.
    /// </summary>
    public DbSet<PayrollDeduction> PayrollDeductions => Set<PayrollDeduction>();
    /// <summary>
    /// Gets the payroll schedules set.
    /// </summary>
    public DbSet<PayrollSchedule> PayrollSchedules => Set<PayrollSchedule>();

    /// <summary>
    /// Gets the localized timesheet ledger.
    /// </summary>
    public DbSet<PayrollTimesheetLedger> TimesheetLedger => Set<PayrollTimesheetLedger>();

    /// <summary>
    /// Gets the master salary components set.
    /// </summary>
    public DbSet<SalaryComponent> SalaryComponents => Set<SalaryComponent>();

    /// <summary>
    /// Gets the salary templates set.
    /// </summary>
    public DbSet<SalaryTemplate> SalaryTemplates => Set<SalaryTemplate>();

    /// <summary>
    /// Gets the Professional Tax slabs set.
    /// </summary>
    public DbSet<ProfessionalTaxSlab> ProfessionalTaxSlabs => Set<ProfessionalTaxSlab>();

    /// <summary>
    /// Gets the IT declarations set.
    /// </summary>
    public DbSet<ITDeclaration> ITDeclarations => Set<ITDeclaration>();

    /// <summary>
    /// Gets the payroll ledger entries set.
    /// </summary>
    public DbSet<PayrollLedgerEntry> PayrollLedger => Set<PayrollLedgerEntry>();

    /// <summary>
    /// Gets the compliance snapshots set.
    /// </summary>
    public DbSet<ComplianceSnapshot> ComplianceSnapshots => Set<ComplianceSnapshot>();

    /// <summary>
    /// Gets the compliance filings set.
    /// </summary>
    public DbSet<ComplianceFiling> ComplianceFilings => Set<ComplianceFiling>();

    /// <summary>
    /// Configures the domain model for the Payroll context.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.Entity<PayrollProfile>(b =>
        {
            b.ToTable("PayrollProfiles");
            b.HasKey(x => x.Id);
            // In a real app, you'd apply the RLS function here via EF Core mapping
        });

        modelBuilder.Entity<PayrollRunState>(b =>
        {
            b.ToTable("PayrollRunStates");
            b.HasKey(x => x.CorrelationId);
            b.Property(x => x.CurrentState).HasMaxLength(64);
            // In a real app, you'd apply the RLS function here via EF Core mapping
        });

        modelBuilder.Entity<PayrollDeduction>(b =>
        {
            b.ToTable("PayrollDeductions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<PayrollSchedule>(b =>
        {
            b.ToTable("PayrollSchedules");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(128);
        });

        modelBuilder.Entity<PayrollTimesheetLedger>(b =>
        {
            b.ToTable("PayrollTimesheetLedger");
            b.HasKey(x => x.Id);
            b.Property(x => x.TotalHours).HasPrecision(18, 2);
        });

        modelBuilder.Entity<SalaryComponent>(b =>
        {
            b.ToTable("SalaryComponents");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(128);
        });

        modelBuilder.Entity<SalaryTemplate>(b =>
        {
            b.ToTable("SalaryTemplates");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(128);
            b.OwnsMany(x => x.Components, c => c.ToJson());
        });

        modelBuilder.Entity<ProfessionalTaxSlab>(b =>
        {
            b.ToTable("ProfessionalTaxSlabs");
            b.HasKey(x => x.Id);
            b.Property(x => x.StateCode).HasMaxLength(10);
            b.Property(x => x.MinGross).HasPrecision(18, 2);
            b.Property(x => x.MaxGross).HasPrecision(18, 2);
            b.Property(x => x.MonthlyTaxAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ITDeclaration>(b =>
        {
            b.ToTable("ITDeclarations");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.EmployeeId, x.FinancialYear });
            b.HasIndex(x => x.Status);

            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.ClaimedAmount).HasPrecision(18, 2);
            b.Property(x => x.ApprovedAmount).HasPrecision(18, 2);

            b.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<PayrollLedgerEntry>(b =>
        {
            b.ToTable("PayrollLedger");
            b.HasKey(x => x.Id);
            // Filtered index for YTD queries (most common read path).
            b.HasIndex(x => new { x.EmployeeId, x.FinancialYearStart });
            // Unique composite index: prevents duplicate ledger entries for the same
            // employee within a single payroll run. This is the database-level idempotency
            // guard that closes the race window between the idempotency check in
            // PayrollBatchConsumer and the BulkInsertAsync call.
            b.HasIndex(x => new { x.RunId, x.EmployeeId }).IsUnique();
            b.Property(x => x.MonthlyGross).HasPrecision(18, 2);
            b.Property(x => x.TdsDeducted).HasPrecision(18, 2);
            b.Property(x => x.NetPay).HasPrecision(18, 2);
            b.OwnsOne(x => x.Deductions, d => d.ToJson());
        });

        modelBuilder.Entity<ComplianceSnapshot>(b =>
        {
            b.ToTable("ComplianceSnapshots");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.PayrollRunId, x.Type });
        });

        modelBuilder.Entity<ComplianceFiling>(b =>
        {
            b.ToTable("ComplianceFilings");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.PayrollRunId, x.Type });
            b.Property(x => x.Status).HasConversion<string>();
        });
    }
}
