// -----------------------------------------------------------------------
// <copyright file="PayrollDbContext.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Payroll.Domain;
using Karamchari.Payroll.Domain.Adjustments;
using Karamchari.Payroll.Domain.Arrears;
using Karamchari.Payroll.Domain.Audit;
using Karamchari.Payroll.Domain.Calculation;
using Karamchari.Payroll.Domain.Compensation;
using Karamchari.Payroll.Domain.Compliance;
using Karamchari.Payroll.Domain.Corrections;
using Karamchari.Payroll.Domain.DeductionRules;
using Karamchari.Payroll.Domain.Disbursement;
using Karamchari.Payroll.Domain.FnF;
using Karamchari.Payroll.Domain.Loans;
using Karamchari.Payroll.Domain.PayPeriods;
using Karamchari.Payroll.Domain.Payslips;
using Karamchari.Payroll.Domain.Reconciliation;
using Karamchari.Payroll.Domain.Reimbursements;
using Karamchari.Payroll.Domain.Results;
using Karamchari.Payroll.Domain.Runs;
using Karamchari.Payroll.Domain.SalaryRevisions;
using Karamchari.Payroll.Domain.SalaryStructures;
using Karamchari.Payroll.Domain.Simulation;
using Karamchari.Payroll.Domain.Statutory;
using Karamchari.Payroll.Domain.VariablePay;
using Karamchari.Payroll.Domain.WorkRules;
using Karamchari.Payroll.StateMachines;
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
    /// Gets the India statutory profiles set.
    /// </summary>
    public DbSet<IndiaPayrollProfile> IndiaPayrollProfiles => Set<IndiaPayrollProfile>();
    /// <summary>
    /// Gets the payroll run states set (Saga state).
    /// </summary>
    public DbSet<PayrollRunState> PayrollRunStates => Set<PayrollRunState>();

    // ── Phase 4: Pay Periods ────────────────────────────────────────────────
    public DbSet<PayPeriod> PayPeriods => Set<PayPeriod>();

    // ── Phase 4: Payroll Runs (domain aggregate) ───────────────────────────
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrollLock> PayrollLocks => Set<PayrollLock>();

    // ── Phase 4: Compensation Profile (versioned) ──────────────────────────
    public DbSet<CompensationProfile> CompensationProfiles => Set<CompensationProfile>();

    // ── Phase 4: Work Rules ────────────────────────────────────────────────
    public DbSet<OvertimePolicy> OvertimePolicies => Set<OvertimePolicy>();
    public DbSet<ShiftPremiumRule> ShiftPremiumRules => Set<ShiftPremiumRule>();

    // ── Phase 4: Calculation ───────────────────────────────────────────────
    public DbSet<PayrollCalculationSnapshot> PayrollCalculationSnapshots => Set<PayrollCalculationSnapshot>();
    public DbSet<PayrollEarning> PayrollEarnings => Set<PayrollEarning>();
    public DbSet<TaxRuleVersion> TaxRuleVersions => Set<TaxRuleVersion>();

    // ── Phase 4: Deduction Rules ───────────────────────────────────────────
    public DbSet<DeductionRule> DeductionRules => Set<DeductionRule>();

    // ── Phase 4: Adjustments ───────────────────────────────────────────────
    public DbSet<PayrollAdjustment> PayrollAdjustments => Set<PayrollAdjustment>();
    public DbSet<RetroPayrollAdjustment> RetroPayrollAdjustments => Set<RetroPayrollAdjustment>();

    // ── Phase 4: Payslips ──────────────────────────────────────────────────
    public DbSet<Payslip> Payslips => Set<Payslip>();

    // ── Phase 4: Audit Trail ───────────────────────────────────────────────
    public DbSet<PayrollAuditEvent> PayrollAuditEvents => Set<PayrollAuditEvent>();

    // ── Phase 4: Employee Payroll Result (per-employee financial record) ───
    public DbSet<EmployeePayrollResult> EmployeePayrollResults => Set<EmployeePayrollResult>();

    // ── Phase 4: Approval + Publication artifacts ──────────────────────────
    public DbSet<PayrollApproval> PayrollApprovals => Set<PayrollApproval>();
    public DbSet<PayrollPublication> PayrollPublications => Set<PayrollPublication>();

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
        base.OnDomainModelCreating(modelBuilder);


        modelBuilder.Entity<PayrollProfile>(b =>
        {
            b.ToTable("PayrollProfiles");
            b.HasKey(x => x.Id);
            b.HasOne(x => x.India)
             .WithOne()
             .HasForeignKey<IndiaPayrollProfile>(x => x.PayrollProfileId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IndiaPayrollProfile>(b =>
        {
            b.ToTable("IndiaPayrollProfiles");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.PayrollProfileId).IsUnique();
            b.Property(x => x.StateCode).HasMaxLength(10);
            b.Property(x => x.Pan).HasMaxLength(10);
            b.Property(x => x.Uan).HasMaxLength(20);
            b.Property(x => x.EsicNumber).HasMaxLength(20);
            b.Property(x => x.TaxRegime).HasConversion<string>();
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

            b.Property(x => x.Deductions)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, decimal>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, decimal>())
                .HasColumnType("nvarchar(max)")
                .Metadata.SetValueComparer(ValueComparers.ReadOnlyDictionaryComparer);

            b.Property(x => x.Earnings)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, decimal>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, decimal>())
                .HasColumnType("nvarchar(max)")
                .Metadata.SetValueComparer(ValueComparers.ReadOnlyDictionaryComparer);
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

            b.Property(x => x.PeriodDiffs)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<ArrearPeriodDiff>>(v, (JsonSerializerOptions?)null) ?? new List<ArrearPeriodDiff>())
                .HasColumnType("nvarchar(max)")
                .Metadata.SetValueComparer(ValueComparers.ReadOnlyCollectionComparer<ArrearPeriodDiff>());
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
        });

        // â”€â”€ Phase 1A: Variable Pay â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        modelBuilder.Entity<VariablePayAllocation>(b =>
        {
            b.ToTable("VariablePayAllocations");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId });
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Type).HasConversion<string>();
            b.Property(x => x.Taxability).HasConversion<string>();
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

            b.Property(x => x.Results)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<SimulationEmployeeResult>>(v, (JsonSerializerOptions?)null) ?? new List<SimulationEmployeeResult>())
                .HasColumnType("nvarchar(max)")
                .Metadata.SetValueComparer(ValueComparers.ReadOnlyCollectionComparer<SimulationEmployeeResult>());
        });

        // â”€â”€ Phase 1A: Reconciliation â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        modelBuilder.Entity<ReconciliationJob>(b =>
        {
            b.ToTable("ReconciliationJobs");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.PeriodName });
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.AnomalyScore).HasPrecision(5, 2);

            b.Property(x => x.Anomalies)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<PayrollAnomaly>>(v, (JsonSerializerOptions?)null) ?? new List<PayrollAnomaly>())
                .HasColumnType("nvarchar(max)")
                .Metadata.SetValueComparer(ValueComparers.ReadOnlyCollectionComparer<PayrollAnomaly>());
        });

        // ── Phase 4: Pay Periods ─────────────────────────────────────────────────────────────────────

        modelBuilder.Entity<PayPeriod>(b =>
        {
            b.ToTable("PayPeriods");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.StartDate, x.EndDate });
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Frequency).HasConversion<string>();
            b.Property(x => x.Name).HasMaxLength(128);
        });

        // ── Phase 4: Payroll Runs (domain aggregate) ─────────────────────────────────────────────────

        modelBuilder.Entity<PayrollRun>(b =>
        {
            b.ToTable("PayrollRuns");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.PayPeriodId });
            b.HasIndex(x => x.Status);
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.PeriodName).HasMaxLength(128);
            b.Property(x => x.TotalGross).HasPrecision(18, 2);
            b.Property(x => x.TotalDeductions).HasPrecision(18, 2);
            b.Property(x => x.TotalNet).HasPrecision(18, 2);
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<PayrollLock>(b =>
        {
            b.ToTable("PayrollLocks");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.PayrollRunId).IsUnique();
            b.Property(x => x.LockedBy).HasMaxLength(256);
        });

        // ── Phase 4: Compensation Profile ────────────────────────────────────────────────────────────

        modelBuilder.Entity<CompensationProfile>(b =>
        {
            b.ToTable("CompensationProfiles");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.EffectiveFrom });
            b.HasIndex(x => new { x.EmployeeId, x.IsActive });
            b.Property(x => x.HourlyRate).HasPrecision(18, 4);
            b.Property(x => x.MonthlySalary).HasPrecision(18, 2);
            b.Property(x => x.OvertimeMultiplier).HasPrecision(5, 2);
            b.Property(x => x.Currency).HasMaxLength(10);
        });

        // ── Phase 4: Work Rules ───────────────────────────────────────────────────────────────────────

        modelBuilder.Entity<OvertimePolicy>(b =>
        {
            b.ToTable("OvertimePolicies");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.IsActive });
            b.Property(x => x.Name).HasMaxLength(128);
        });

        modelBuilder.Entity<ShiftPremiumRule>(b =>
        {
            b.ToTable("ShiftPremiumRules");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.IsActive });
            b.Property(x => x.Name).HasMaxLength(128);
            b.Property(x => x.PremiumPercentage).HasPrecision(5, 2);
        });

        // ── Phase 4: Calculation Snapshot + Earnings ──────────────────────────────────────────────────

        modelBuilder.Entity<PayrollCalculationSnapshot>(b =>
        {
            b.ToTable("PayrollCalculationSnapshots");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.PayrollRunId).IsUnique();
            b.Property(x => x.SerializedData).HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<PayrollEarning>(b =>
        {
            b.ToTable("PayrollEarnings");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.PayrollRunId, x.EmployeeId });
            b.HasIndex(x => new { x.TenantId, x.EmployeeId });
            b.Property(x => x.Type).HasConversion<string>();
            b.Property(x => x.Amount).HasPrecision(18, 2);
            b.Property(x => x.Description).HasMaxLength(256);
        });

        modelBuilder.Entity<TaxRuleVersion>(b =>
        {
            b.ToTable("TaxRuleVersions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.FinancialYear, x.Regime });
            b.Property(x => x.Regime).HasMaxLength(32);
            b.Property(x => x.JsonDefinition).HasColumnType("nvarchar(max)");
        });

        // ── Phase 4: Deduction Rules ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<DeductionRule>(b =>
        {
            b.ToTable("DeductionRules");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Type, x.IsActive });
            b.Property(x => x.Type).HasConversion<string>();
            b.Property(x => x.Percentage).HasPrecision(5, 4);
            b.Property(x => x.CapAmount).HasPrecision(18, 2);
            b.Property(x => x.MinGrossForApplicability).HasPrecision(18, 2);
        });

        // ── Phase 4: Adjustments ──────────────────────────────────────────────────────────────────────

        modelBuilder.Entity<PayrollAdjustment>(b =>
        {
            b.ToTable("PayrollAdjustments");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId });
            b.HasIndex(x => x.Status);
            b.Property(x => x.Type).HasConversion<string>();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Amount).HasPrecision(18, 2);
            b.Property(x => x.Reason).HasMaxLength(512);
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<RetroPayrollAdjustment>(b =>
        {
            b.ToTable("RetroPayrollAdjustments");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.EmployeeId });
            b.HasIndex(x => x.SourcePayrollRunId);
            b.HasIndex(x => x.Status);
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.AmountPaid).HasPrecision(18, 2);
            b.Property(x => x.AmountShouldBePaid).HasPrecision(18, 2);
            b.Property(x => x.Difference).HasPrecision(18, 2);
            b.Property(x => x.Reason).HasMaxLength(512);
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        // ── Phase 4: Payslips ─────────────────────────────────────────────────────────────────────────

        modelBuilder.Entity<Payslip>(b =>
        {
            b.ToTable("Payslips");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.PayrollRunId, x.EmployeeId }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.EmployeeId });
            // FK to EmployeePayrollResult — Payslip is a projection; result is the source of truth
            b.HasIndex(x => x.PayrollResultId);
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.EmployeeName).HasMaxLength(256);
            b.Property(x => x.PeriodName).HasMaxLength(128);
            b.Property(x => x.GrossPay).HasPrecision(18, 2);
            b.Property(x => x.TotalDeductions).HasPrecision(18, 2);
            b.Property(x => x.NetPay).HasPrecision(18, 2);
            b.Property(x => x.StoragePath).HasMaxLength(1024);
        });

        // ── Phase 4: Audit Trail ──────────────────────────────────────────────────────────────────────

        modelBuilder.Entity<PayrollAuditEvent>(b =>
        {
            b.ToTable("PayrollAuditEvents");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.PayrollRunId);
            b.HasIndex(x => new { x.TenantId, x.Timestamp });
            b.Property(x => x.Action).HasMaxLength(128);
            b.Property(x => x.Actor).HasMaxLength(256);
            b.Property(x => x.Payload).HasColumnType("nvarchar(max)");
        });

        // ── Phase 4: Employee Payroll Result ──────────────────────────────────────────────────────────

        modelBuilder.Entity<EmployeePayrollResult>(b =>
        {
            b.ToTable("EmployeePayrollResults");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.PayrollRunId, x.EmployeeId }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.EmployeeId });
            b.HasIndex(x => x.Status);
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.EmployeeName).HasMaxLength(256);
            b.Property(x => x.PeriodName).HasMaxLength(128);
            b.Property(x => x.SnapshotId).HasMaxLength(64);
            b.Property(x => x.BasePay).HasPrecision(18, 2);
            b.Property(x => x.OvertimePay).HasPrecision(18, 2);
            b.Property(x => x.HolidayPay).HasPrecision(18, 2);
            b.Property(x => x.ShiftPremium).HasPrecision(18, 2);
            b.Property(x => x.AllowancePay).HasPrecision(18, 2);
            b.Property(x => x.BonusPay).HasPrecision(18, 2);
            b.Property(x => x.GrossPay).HasPrecision(18, 2);
            b.Property(x => x.ProvidentFund).HasPrecision(18, 2);
            b.Property(x => x.EmployeeStateInsurance).HasPrecision(18, 2);
            b.Property(x => x.ProfessionalTax).HasPrecision(18, 2);
            b.Property(x => x.TaxDeductedAtSource).HasPrecision(18, 2);
            b.Property(x => x.OtherDeductions).HasPrecision(18, 2);
            b.Property(x => x.TotalDeductions).HasPrecision(18, 2);
            b.Property(x => x.RetroAdjustments).HasPrecision(18, 2);
            b.Property(x => x.ManualAdjustments).HasPrecision(18, 2);
            b.Property(x => x.NetPay).HasPrecision(18, 2);
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        // ── Phase 4: Payroll Approval ─────────────────────────────────────────────────────────────────

        modelBuilder.Entity<PayrollApproval>(b =>
        {
            b.ToTable("PayrollApprovals");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.PayrollRunId).IsUnique();
            b.Property(x => x.ApprovedBy).HasMaxLength(256);
            b.Property(x => x.TotalGrossApproved).HasPrecision(18, 2);
            b.Property(x => x.TotalNetApproved).HasPrecision(18, 2);
            b.Property(x => x.Comments).HasMaxLength(1024);
        });

        // ── Phase 4: Payroll Publication ──────────────────────────────────────────────────────────────

        modelBuilder.Entity<PayrollPublication>(b =>
        {
            b.ToTable("PayrollPublications");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.PayrollRunId).IsUnique();
            b.Property(x => x.PublishedBy).HasMaxLength(256);
            b.Property(x => x.BankFileId).HasMaxLength(256);
            b.Property(x => x.BankFileReference).HasMaxLength(512);
            b.Property(x => x.TotalAmountDispatched).HasPrecision(18, 2);
        });

        // ── Phase 4: OvertimePolicy tiered rules (owned collection → JSON) ───────────────────────────

        modelBuilder.Entity<OvertimePolicy>(b =>
        {
            b.OwnsMany(x => x.Rules, r => r.ToJson());
        });
    }
}
