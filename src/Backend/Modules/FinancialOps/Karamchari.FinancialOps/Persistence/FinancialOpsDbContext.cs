using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.FinancialOps.Contracts.Primitives;
using Karamchari.FinancialOps.Contracts.Reimbursements;
using Karamchari.FinancialOps.Contracts.Settlements;
using Karamchari.FinancialOps.Domain.Ledger;
using Karamchari.FinancialOps.Domain.Periods;
using Karamchari.FinancialOps.Domain.Reimbursements;
using Karamchari.FinancialOps.Domain.Settlements;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.FinancialOps.Persistence;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class FinancialOpsDbContext : KaramchariDbContext
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public FinancialOpsDbContext(DbContextOptions<FinancialOpsDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<WorkforceFinancialEntry> LedgerEntries => Set<WorkforceFinancialEntry>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<FinancialOperationalPeriod> OperationalPeriods => Set<FinancialOperationalPeriod>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<EmployeeLoan> Loans => Set<EmployeeLoan>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<EmployeeAdvance> Advances => Set<EmployeeAdvance>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<FinancialCompensationOperation> Compensations => Set<FinancialCompensationOperation>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<ReimbursementSettlementBatch> ReimbursementSettlementBatches => Set<ReimbursementSettlementBatch>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<ExpenseClaim> ExpenseClaims => Set<ExpenseClaim>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<FinancialOperationEvent> OperationEvents => Set<FinancialOperationEvent>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<FinancialFinalizationSnapshot> FinalizationSnapshots => Set<FinancialFinalizationSnapshot>();

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.Entity<WorkforceFinancialEntry>(b =>
        {
            b.ToTable("WorkforceFinancialLedger");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasConversion(id => id.Value, value => new WorkforceFinancialEntryId(value));
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value)).IsRequired();
            b.Property(x => x.PeriodId).HasConversion(id => id.Value, value => new FinancialOperationalPeriodId(value)).IsRequired();
            b.Property(x => x.Amount).HasPrecision(18, 4).IsRequired();
            b.Property(x => x.Currency).HasConversion(id => id.Value, value => new CurrencyCode(value)).HasMaxLength(3).IsRequired();
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => x.EmployeeId);

            b.Property(x => x.CorrelationChain)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<FinancialCorrelationChain>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                .HasColumnType("nvarchar(max)");

            b.Property(x => x.ReplayMetadata)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<FinancialReplayMetadata>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                .HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<FinancialOperationalPeriod>(b =>
        {
            b.ToTable("FinancialOperationalPeriods");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasConversion(id => id.Value, value => new FinancialOperationalPeriodId(value));
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
        });

        modelBuilder.Entity<EmployeeLoan>(b =>
        {
            b.ToTable("Financial_EmployeeLoans");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value)).IsRequired();
            b.Property(x => x.PrincipalAmount).HasPrecision(18, 4).IsRequired();
            b.Property(x => x.InterestRate).HasPrecision(5, 2).IsRequired();
            b.Property(x => x.RemainingPrincipal).HasPrecision(18, 4).IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();

            b.Property(x => x.ReplayMetadata)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<FinancialReplayMetadata>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                .HasColumnType("nvarchar(max)");

            b.OwnsMany(x => x.Repayments, rb =>
            {
                rb.ToTable("Financial_EmployeeLoanRepayments");
                rb.WithOwner().HasForeignKey("LoanId");
                rb.HasKey(x => x.Id);
                rb.Property(x => x.Amount).HasPrecision(18, 4).IsRequired();

                rb.Property(x => x.CorrelationChain)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<FinancialCorrelationChain>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                    .HasColumnType("nvarchar(max)");
            });

            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => x.EmployeeId);
        });

        modelBuilder.Entity<EmployeeAdvance>(b =>
        {
            b.ToTable("Financial_EmployeeAdvances");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value)).IsRequired();
            b.Property(x => x.PeriodId).HasConversion(id => id.Value, value => new FinancialOperationalPeriodId(value)).IsRequired();
            b.Property(x => x.Amount).HasPrecision(18, 4).IsRequired();
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => x.EmployeeId);

            b.Property(x => x.ReplayMetadata)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<FinancialReplayMetadata>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                .HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<FinancialCompensationOperation>(b =>
        {
            b.ToTable("FinancialCompensations");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.TargetEntryId).HasConversion(id => id.Value, value => new WorkforceFinancialEntryId(value)).IsRequired();
            b.Property(x => x.Amount).HasPrecision(18, 4).IsRequired();
            b.HasIndex(x => x.TenantId);

            b.Property(x => x.CorrelationChain)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<FinancialCorrelationChain>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                .HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<ReimbursementSettlementBatch>(b =>
        {
            b.ToTable("ReimbursementSettlementBatches");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasConversion(id => id.Value, value => new SettlementBatchId(value));
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.PeriodId).HasConversion(id => id.Value, value => new FinancialOperationalPeriodId(value)).IsRequired();
            
            b.Property(x => x.ClaimIds).HasConversion(
                v => string.Join(',', v.Select(id => id.Value)),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => new ExpenseClaimId(Guid.Parse(s))).ToList().AsReadOnly()
            ).Metadata.SetValueComparer(ValueComparers.ReadOnlyCollectionComparer<ExpenseClaimId>());

            b.HasIndex(x => x.TenantId);
        });

        modelBuilder.Entity<ExpenseClaim>(b =>
        {
            b.ToTable("ExpenseClaims");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasConversion(id => id.Value, value => new ExpenseClaimId(value));
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value)).IsRequired();
            b.Property(x => x.CategoryId).HasConversion(id => id.Value, value => new ExpenseCategoryId(value)).IsRequired();
            b.Property(x => x.ClaimedAmount).HasPrecision(18, 4).IsRequired();
            b.Property(x => x.Currency).HasConversion(id => id.Value, value => new CurrencyCode(value)).HasMaxLength(3).IsRequired();
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => x.EmployeeId);
            b.HasIndex(x => x.ClaimNumber).IsUnique();

            b.Property(x => x.ReplayMetadata)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<FinancialReplayMetadata>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                .HasColumnType("nvarchar(max)");

            b.Property(x => x.Holds)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => (IReadOnlyList<FinancialOperationalHold>)(System.Text.Json.JsonSerializer.Deserialize<List<FinancialOperationalHold>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<FinancialOperationalHold>()))
                .HasColumnType("nvarchar(max)")
                .Metadata.SetValueComparer(ValueComparers.ReadOnlyListComparer<FinancialOperationalHold>());

            b.Property(x => x.Receipts)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => (IReadOnlyList<ExpenseReceipt>)(System.Text.Json.JsonSerializer.Deserialize<List<ExpenseReceipt>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<ExpenseReceipt>()))
                .HasColumnType("nvarchar(max)")
                .Metadata.SetValueComparer(ValueComparers.ReadOnlyListComparer<ExpenseReceipt>());
        });

        modelBuilder.Entity<FinancialOperationEvent>(b =>
        {
            b.ToTable("Financial_OperationEvents");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => x.AggregateId);

            b.Property(x => x.CorrelationChain)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<FinancialCorrelationChain>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                .HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<FinancialFinalizationSnapshot>(b =>
        {
            b.ToTable("Financial_FinalizationSnapshots");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.PeriodId).HasConversion(id => id.Value, value => new FinancialOperationalPeriodId(value)).IsRequired();
            b.Property(x => x.FinalizedTotal).HasPrecision(18, 4).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.PeriodId }).IsUnique();
        });
    }
}
