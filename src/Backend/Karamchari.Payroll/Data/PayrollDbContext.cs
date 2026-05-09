using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Payroll.Domain;
using Karamchari.Payroll.Domain.Arrears;
using Karamchari.Payroll.Domain.Compliance;
using Karamchari.Payroll.Domain.Corrections;
using Karamchari.Payroll.Domain.Disbursement;
using Karamchari.Payroll.Domain.FnF;
using Karamchari.Payroll.Domain.Loans;
using Karamchari.Payroll.Domain.Reconciliation;
using Karamchari.Payroll.Domain.Reimbursements;
using Karamchari.Payroll.Domain.SalaryRevisions;
using Karamchari.Payroll.Domain.SalaryStructures;
using Karamchari.Payroll.Domain.Simulation;
using Karamchari.Payroll.Domain.Statutory;
using Karamchari.Payroll.Domain.VariablePay;
using Karamchari.Payroll.StateMachines;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

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

    // Phase 1A new DbSets
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<FnFSettlement> FnFSettlements => Set<FnFSettlement>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<ArrearCalculation> ArrearCalculations => Set<ArrearCalculation>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<PayrollCorrection> PayrollCorrections => Set<PayrollCorrection>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<ReimbursementClaim> ReimbursementClaims => Set<ReimbursementClaim>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<EmployeeLoan> EmployeeLoans => Set<EmployeeLoan>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<VariablePayAllocation> VariablePayAllocations => Set<VariablePayAllocation>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<SalaryRevision> SalaryRevisions => Set<SalaryRevision>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<DisbursementBatch> DisbursementBatches => Set<DisbursementBatch>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<PayrollSimulation> PayrollSimulations => Set<PayrollSimulation>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<ReconciliationJob> ReconciliationJobs => Set<ReconciliationJob>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<FnFSettlementState> FnFSettlementStates => Set<FnFSettlementState>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<DisbursementBatchState> DisbursementBatchStates => Set<DisbursementBatchState>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<PayrollCorrectionState> PayrollCorrectionStates => Set<PayrollCorrectionState>();

    /// <summary>
    /// Configures the domain model for the Payroll context.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        const string MessagingSchema = "dbo";

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<InboxState>(b => b.ToTable("InboxState", MessagingSchema));
        modelBuilder.Entity<OutboxMessage>(b => b.ToTable("OutboxMessage", MessagingSchema));
        modelBuilder.Entity<OutboxState>(b => b.ToTable("OutboxState", MessagingSchema));

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

        // â”€â”€ Phase 1A: FnF â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        modelBuilder.Entity<FnFSettlement>(b =>
        {
            b.ToTable("FnFSettlements");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId });
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.ExitType).HasConversion<string>();
            b.Property(x => x.TotalEarnings).HasPrecision(18, 2);
            b.Property(x => x.TotalDeductions).HasPrecision(18, 2);
            b.Property(x => x.NetSettlementAmount).HasPrecision(18, 2);
            b.Property(x => x.RowVersion).IsRowVersion();
            b.OwnsMany(x => x.LineItems, li =>
            {
                li.ToTable("FnFLineItems");
                li.HasKey(x => x.Id);
                li.Property(x => x.Type).HasConversion<string>();
                li.Property(x => x.Amount).HasPrecision(18, 2);
            });
        });

        modelBuilder.Entity<FnFSettlementState>(b =>
        {
            b.ToTable("FnFSettlementStates");
            b.HasKey(x => x.CorrelationId);
            b.Property(x => x.CurrentState).HasMaxLength(64);
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        // â”€â”€ Phase 1A: Arrears â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        modelBuilder.Entity<ArrearCalculation>(b =>
        {
            b.ToTable("ArrearCalculations");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId });
            b.HasIndex(x => x.TriggerReference);
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.TriggerType).HasConversion<string>();
            b.Property(x => x.TotalGrossDelta).HasPrecision(18, 2);
            b.Property(x => x.TotalNetDelta).HasPrecision(18, 2);
            b.Property(x => x.TotalTdsDelta).HasPrecision(18, 2);
            b.Property(x => x.RowVersion).IsRowVersion();
            b.OwnsMany(x => x.PeriodDiffs, d => d.ToJson());
        });

        // â”€â”€ Phase 1A: Corrections â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        modelBuilder.Entity<PayrollCorrection>(b =>
        {
            b.ToTable("PayrollCorrections");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId });
            b.HasIndex(x => x.IdempotencyKey).IsUnique();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Type).HasConversion<string>();
            b.Property(x => x.Scope).HasConversion<string>();
            b.Property(x => x.DifferentialAmount).HasPrecision(18, 2);
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<PayrollCorrectionState>(b =>
        {
            b.ToTable("PayrollCorrectionStates");
            b.HasKey(x => x.CorrelationId);
            b.Property(x => x.CurrentState).HasMaxLength(64);
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        // â”€â”€ Phase 1A: Reimbursements â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        modelBuilder.Entity<ReimbursementClaim>(b =>
        {
            b.ToTable("ReimbursementClaims");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId });
            b.HasIndex(x => x.AttachmentHash);
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Category).HasConversion<string>();
            b.Property(x => x.Taxability).HasConversion<string>();
            b.Property(x => x.FraudIndicator).HasConversion<string>();
            b.Property(x => x.ClaimedAmount).HasPrecision(18, 2);
            b.Property(x => x.ApprovedAmount).HasPrecision(18, 2);
            b.Property(x => x.PolicyLimit).HasPrecision(18, 2);
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        // â”€â”€ Phase 1A: Loans â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        modelBuilder.Entity<EmployeeLoan>(b =>
        {
            b.ToTable("EmployeeLoans");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId });
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Type).HasConversion<string>();
            b.Property(x => x.InterestType).HasConversion<string>();
            b.Property(x => x.PrincipalAmount).HasPrecision(18, 2);
            b.Property(x => x.OutstandingBalance).HasPrecision(18, 2);
            b.Property(x => x.MonthlyEmi).HasPrecision(18, 2);
            b.Property(x => x.InterestRatePercent).HasPrecision(5, 2);
            b.Property(x => x.RowVersion).IsRowVersion();
            b.OwnsMany(x => x.Installments, i =>
            {
                i.ToTable("LoanInstallments");
                i.HasKey(x => x.Id);
                i.Property(x => x.Status).HasConversion<string>();
                i.Property(x => x.PrincipalAmount).HasPrecision(18, 2);
                i.Property(x => x.InterestAmount).HasPrecision(18, 2);
                i.Property(x => x.OutstandingAfter).HasPrecision(18, 2);
            });

            b.OwnsMany(x => x.ApprovalChain, a =>
            {
                a.ToTable("LoanApprovalSteps");
                a.WithOwner().HasForeignKey("LoanId");
                a.Property<Guid>("Id");
                a.HasKey("Id");
                a.Property(x => x.Status).HasConversion<string>();
            });
        });

        // â”€â”€ Phase 1A: Variable Pay â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        modelBuilder.Entity<VariablePayAllocation>(b =>
        {
            b.ToTable("VariablePayAllocations");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId });
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Type).HasConversion<string>();
            b.Property(x => x.TaxTreatment).HasConversion<string>();
            b.Property(x => x.AllocatedAmount).HasPrecision(18, 2);
            b.Property(x => x.ProratedAmount).HasPrecision(18, 2);
            b.Property(x => x.PaidAmount).HasPrecision(18, 2);
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        // â”€â”€ Phase 1A: Salary Revisions â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        modelBuilder.Entity<SalaryRevision>(b =>
        {
            b.ToTable("SalaryRevisions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId });
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Type).HasConversion<string>();
            b.Property(x => x.PreviousCTC).HasPrecision(18, 2);
            b.Property(x => x.NewCTC).HasPrecision(18, 2);
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        // â”€â”€ Phase 1A: Disbursement â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        modelBuilder.Entity<DisbursementBatch>(b =>
        {
            b.ToTable("DisbursementBatches");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.RunId).IsUnique();  // deduplication guard
            b.HasIndex(x => x.PeriodName);
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.BankProvider).HasConversion<string>();
            b.Property(x => x.TotalAmount).HasPrecision(18, 2);
            b.Property(x => x.RowVersion).IsRowVersion();
            b.OwnsMany(x => x.Entries, e =>
            {
                e.ToTable("DisbursementEntries");
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.IdempotencyKey).IsUnique();
                e.Property(x => x.Status).HasConversion<string>();
                e.Property(x => x.Amount).HasPrecision(18, 2);
            });
        });

        modelBuilder.Entity<DisbursementBatchState>(b =>
        {
            b.ToTable("DisbursementBatchStates");
            b.HasKey(x => x.CorrelationId);
            b.Property(x => x.CurrentState).HasMaxLength(64);
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        // â”€â”€ Phase 1A: Simulation â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        modelBuilder.Entity<PayrollSimulation>(b =>
        {
            b.ToTable("PayrollSimulations");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.ExpiresAtUtc);
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Type).HasConversion<string>();
            b.Property(x => x.TotalProjectedGross).HasPrecision(18, 2);
            b.Property(x => x.TotalProjectedNet).HasPrecision(18, 2);
            b.Property(x => x.TotalProjectedDelta).HasPrecision(18, 2);
            b.OwnsMany(x => x.Results, r => r.ToJson());
        });

        // â”€â”€ Phase 1A: Reconciliation â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        modelBuilder.Entity<ReconciliationJob>(b =>
        {
            b.ToTable("ReconciliationJobs");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.PeriodName });
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.AnomalyScore).HasPrecision(5, 2);
            b.OwnsMany(x => x.Anomalies, a =>
            {
                a.ToTable("PayrollAnomalies");
                a.HasKey(x => x.Id);
                a.Property(x => x.Type).HasConversion<string>();
                a.Property(x => x.Severity).HasConversion<string>();
                a.Property(x => x.ResolutionStatus).HasConversion<string>();
                a.Property(x => x.AnomalyScore).HasPrecision(5, 2);
            });
        });
    }
}
