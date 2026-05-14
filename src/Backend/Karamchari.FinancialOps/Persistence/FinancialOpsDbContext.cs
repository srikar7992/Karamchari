using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.FinancialOps.Contracts.Primitives;
using Karamchari.FinancialOps.Contracts.Reimbursements;
using Karamchari.FinancialOps.Contracts.Settlements;
using Karamchari.FinancialOps.Domain.Ledger;
using Karamchari.FinancialOps.Domain.Periods;
using Karamchari.FinancialOps.Domain.Reimbursements;
using Karamchari.FinancialOps.Domain.Settlements;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.FinancialOps.Persistence;

/// <summary>
/// Database context for the Financial Operations bounded context.
/// Tracks the operational financial ledger and period governance.
/// </summary>
public sealed class FinancialOpsDbContext : KaramchariDbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FinancialOpsDbContext"/> class.
    /// </summary>
    /// <param name="options">The context options.</param>
    /// <param name="tenantProvider">The tenant provider.</param>
    public FinancialOpsDbContext(DbContextOptions<FinancialOpsDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    /// <summary>
    /// Schema where MassTransit's transactional outbox tables live.
    /// </summary>
    private const string MessagingSchema = "dbo";

    /// <summary>
    /// Gets the set of workforce financial entries (the ledger).
    /// </summary>
    public DbSet<WorkforceFinancialEntry> LedgerEntries => Set<WorkforceFinancialEntry>();

    /// <summary>
    /// Gets the set of financial operational periods.
    /// </summary>
    public DbSet<FinancialOperationalPeriod> OperationalPeriods => Set<FinancialOperationalPeriod>();

    /// <summary>
    /// Gets the set of financial operation events (internal event sourcing).
    /// </summary>
    public DbSet<FinancialOperationEvent> OperationEvents => Set<FinancialOperationEvent>();

    /// <summary>
    /// Gets the set of employee loans.
    /// </summary>
    public DbSet<EmployeeLoan> Loans => Set<EmployeeLoan>();

    /// <summary>
    /// Gets the set of employee advances.
    /// </summary>
    public DbSet<EmployeeAdvance> Advances => Set<EmployeeAdvance>();

    /// <summary>
    /// Gets the set of financial reconciliation snapshots.
    /// </summary>
    public DbSet<FinancialReconciliationSnapshot> ReconciliationSnapshots => Set<FinancialReconciliationSnapshot>();

    /// <summary>
    /// Gets the set of financial finalization snapshots.
    /// </summary>
    public DbSet<FinancialFinalizationSnapshot> FinalizationSnapshots => Set<FinancialFinalizationSnapshot>();

    /// <summary>
    /// Gets the set of financial compensation operations.
    /// </summary>
    public DbSet<FinancialCompensationOperation> Compensations => Set<FinancialCompensationOperation>();

    /// <summary>
    /// Configures the domain model for the FinancialOps context.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.Entity<FinancialCompensationOperation>(b =>
        {
            b.ToTable("FinancialCompensations");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.TargetEntryId).HasConversion(id => id.Value, value => new WorkforceFinancialEntryId(value)).IsRequired();
            b.Property(x => x.Amount).HasPrecision(18, 4).IsRequired();
            b.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();

            b.OwnsOne(x => x.CorrelationChain, cb =>
            {
                cb.Property(c => c.RootId).HasConversion(id => id.Value, value => new FinancialCorrelationId(value)).HasColumnName("CorrelationRootId");
                cb.Property(c => c.ParentId).HasConversion(id => id != null ? id.Value : (Guid?)null, value => value.HasValue ? new FinancialCorrelationId(value.Value) : null).HasColumnName("CorrelationParentId");
                cb.Property(c => c.CurrentId).HasConversion(id => id.Value, value => new FinancialCorrelationId(value)).HasColumnName("CorrelationCurrentId");
                cb.Property(c => c.Depth).HasColumnName("CorrelationDepth");
            });

            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => x.TargetEntryId);
        });

        modelBuilder.Entity<EmployeeLoan>(b =>
        {
            b.ToTable("EmployeeLoans");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value)).IsRequired();
            b.Property(x => x.PrincipalAmount).HasPrecision(18, 4).IsRequired();
            b.Property(x => x.InterestRate).HasPrecision(5, 2).IsRequired();
            b.Property(x => x.RemainingPrincipal).HasPrecision(18, 4).IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();

            b.OwnsOne(x => x.ReplayMetadata, rb =>
            {
                rb.Property(r => r.SourceSystem).HasMaxLength(100).HasColumnName("ReplaySourceSystem");
                rb.Property(r => r.OriginalTimestamp).HasColumnName("ReplayOriginalTimestamp");
                rb.Property(r => r.ReplayAttempt).HasColumnName("ReplayAttemptCount");
                rb.Property(r => r.IsSimulated).HasColumnName("IsSimulatedReplay");
            });

            b.OwnsMany(x => x.Repayments, rb =>
            {
                rb.ToTable("EmployeeLoanRepayments");
                rb.WithOwner().HasForeignKey("LoanId");
                rb.HasKey(x => x.Id);
                rb.Property(x => x.Amount).HasPrecision(18, 4).IsRequired();
                rb.Property(x => x.Explanation).HasMaxLength(500).IsRequired();

                rb.OwnsOne(x => x.CorrelationChain, cb =>
                {
                    cb.Property(c => c.RootId).HasConversion(id => id.Value, value => new FinancialCorrelationId(value)).HasColumnName("CorrelationRootId");
                    cb.Property(c => c.ParentId).HasConversion(id => id != null ? id.Value : (Guid?)null, value => value.HasValue ? new FinancialCorrelationId(value.Value) : null).HasColumnName("CorrelationParentId");
                    cb.Property(c => c.CurrentId).HasConversion(id => id.Value, value => new FinancialCorrelationId(value)).HasColumnName("CorrelationCurrentId");
                });
            });

            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => x.EmployeeId);
        });

        modelBuilder.Entity<EmployeeAdvance>(b =>
        {
            b.ToTable("EmployeeAdvances");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value)).IsRequired();
            b.Property(x => x.PeriodId).HasConversion(id => id.Value, value => new FinancialOperationalPeriodId(value)).IsRequired();
            b.Property(x => x.Amount).HasPrecision(18, 4).IsRequired();
            b.Property(x => x.IsRecovered).IsRequired();

            b.OwnsOne(x => x.ReplayMetadata, rb =>
            {
                rb.Property(r => r.SourceSystem).HasMaxLength(100).HasColumnName("ReplaySourceSystem");
                rb.Property(r => r.OriginalTimestamp).HasColumnName("ReplayOriginalTimestamp");
                rb.Property(r => r.ReplayAttempt).HasColumnName("ReplayAttemptCount");
                rb.Property(r => r.IsSimulated).HasColumnName("IsSimulatedReplay");
            });

            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => x.EmployeeId);
            b.HasIndex(x => x.PeriodId);
        });

        modelBuilder.Entity<FinancialReconciliationSnapshot>(b =>
        {
            b.ToTable("FinancialReconciliationSnapshots");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.PeriodId).HasConversion(id => id.Value, value => new FinancialOperationalPeriodId(value)).IsRequired();
            b.Property(x => x.ExpectedTotal).HasPrecision(18, 4).IsRequired();
            b.Property(x => x.ActualTotal).HasPrecision(18, 4).IsRequired();
            b.Property(x => x.DriftAmount).HasPrecision(18, 4).IsRequired();
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => x.PeriodId);
        });

        modelBuilder.Entity<FinancialFinalizationSnapshot>(b =>
        {
            b.ToTable("FinancialFinalizationSnapshots");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.PeriodId).HasConversion(id => id.Value, value => new FinancialOperationalPeriodId(value)).IsRequired();
            b.Property(x => x.FinalizedTotal).HasPrecision(18, 4).IsRequired();
            b.Property(x => x.Checksum).HasMaxLength(100).IsRequired();
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => x.PeriodId);
        });

        modelBuilder.Entity<FinancialOperationEvent>(b =>
        {
            b.ToTable("FinancialOperationEvents");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.AggregateType).HasMaxLength(100).IsRequired();
            b.Property(x => x.EventName).HasMaxLength(100).IsRequired();
            b.Property(x => x.FromState).HasMaxLength(64).IsRequired();
            b.Property(x => x.ToState).HasMaxLength(64).IsRequired();
            b.Property(x => x.TriggeredBy).HasMaxLength(100).IsRequired();

            b.OwnsOne(x => x.CorrelationChain, cb =>
            {
                cb.Property(c => c.RootId).HasConversion(id => id.Value, value => new FinancialCorrelationId(value)).HasColumnName("CorrelationRootId");
                cb.Property(c => c.ParentId).HasConversion(id => id != null ? id.Value : (Guid?)null, value => value.HasValue ? new FinancialCorrelationId(value.Value) : null).HasColumnName("CorrelationParentId");
                cb.Property(c => c.CurrentId).HasConversion(id => id.Value, value => new FinancialCorrelationId(value)).HasColumnName("CorrelationCurrentId");
                cb.Property(c => c.Depth).HasColumnName("CorrelationDepth");
            });

            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => x.AggregateId);
            b.HasIndex(x => x.TimestampUtc);
        });

        modelBuilder.Entity<WorkforceFinancialEntry>(b =>
        {
            b.ToTable("WorkforceFinancialLedger");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasConversion(id => id.Value, value => new WorkforceFinancialEntryId(value));

            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value)).IsRequired();
            b.Property(x => x.PeriodId).HasConversion(id => id.Value, value => new FinancialOperationalPeriodId(value)).IsRequired();

            b.Property(x => x.Type).HasConversion<int>().IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();
            b.Property(x => x.Amount).HasPrecision(18, 4).IsRequired();
            b.Property(x => x.Currency).HasConversion(c => c.Value, value => new CurrencyCode(value)).HasMaxLength(3).IsRequired();

            b.OwnsOne(x => x.CorrelationChain, cb =>
            {
                cb.Property(c => c.RootId).HasConversion(id => id.Value, value => new FinancialCorrelationId(value)).HasColumnName("CorrelationRootId");
                cb.Property(c => c.ParentId).HasConversion(id => id != null ? id.Value : (Guid?)null, value => value.HasValue ? new FinancialCorrelationId(value.Value) : null).HasColumnName("CorrelationParentId");
                cb.Property(c => c.CurrentId).HasConversion(id => id.Value, value => new FinancialCorrelationId(value)).HasColumnName("CorrelationCurrentId");
                cb.Property(c => c.Depth).HasColumnName("CorrelationDepth");
            });

            b.OwnsOne(x => x.ReplayMetadata, rb =>
            {
                rb.Property(r => r.SourceSystem).HasMaxLength(100).HasColumnName("ReplaySourceSystem");
                rb.Property(r => r.OriginalTimestamp).HasColumnName("ReplayOriginalTimestamp");
                rb.Property(r => r.ReplayAttempt).HasColumnName("ReplayAttemptCount");
                rb.Property(r => r.IsSimulated).HasColumnName("IsSimulatedReplay");
            });

            b.Property(x => x.IdempotencyKey).HasMaxLength(100);
            b.Property(x => x.ExecutionSequenceRef).HasMaxLength(100);

            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => x.EmployeeId);
            b.HasIndex(x => x.PeriodId);
            b.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");
        });

        modelBuilder.Entity<FinancialOperationalPeriod>(b =>
        {
            b.ToTable("FinancialOperationalPeriods");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasConversion(id => id.Value, value => new FinancialOperationalPeriodId(value));

            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();

            b.Property(x => x.StartsAt).IsRequired();
            b.Property(x => x.EndsAt).IsRequired();
            b.Property(x => x.CutoffAt).IsRequired();
            b.Property(x => x.IsLocked).IsRequired();
            b.Property(x => x.IsClosed).IsRequired();

            b.HasIndex(x => x.TenantId);
        });

        modelBuilder.Entity<ReimbursementSettlementBatch>(b =>
        {
            b.ToTable("ReimbursementSettlementBatches");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasConversion(id => id.Value, value => new SettlementBatchId(value));
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.PeriodId).HasConversion(id => id.Value, value => new FinancialOperationalPeriodId(value)).IsRequired();
            b.Property(x => x.Type).HasConversion<int>().IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();
            b.Property(x => x.ExternalReference).HasMaxLength(200);
            b.Property(x => x.LastExplanation).HasMaxLength(500);

            // Collection of Claim IDs stored as a JSON string for simple scale-out partitioning
            // within the same table.
            b.Property(x => x.ClaimIds)
                .HasConversion(
                    ids => System.Text.Json.JsonSerializer.Serialize(ids.Select(id => id.Value), (System.Text.Json.JsonSerializerOptions?)null),
                    json => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(json, (System.Text.Json.JsonSerializerOptions?)null)!
                        .Select(guid => new ExpenseClaimId(guid)).ToList())
                .HasColumnName("ClaimIdsJson");

            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => x.PeriodId);
            b.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<ExpenseClaim>(b =>
        {
            b.ToTable("ExpenseClaims");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasConversion(id => id.Value, value => new ExpenseClaimId(value));

            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value)).IsRequired();
            b.Property(x => x.CategoryId).HasConversion(id => id.Value, value => new ExpenseCategoryId(value)).IsRequired();
            b.Property(x => x.ClaimNumber).HasMaxLength(64).IsRequired();
            b.Property(x => x.ClaimedAmount).HasPrecision(18, 4).IsRequired();
            b.Property(x => x.Currency).HasConversion(c => c.Value, value => new CurrencyCode(value)).HasMaxLength(3).IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();
            b.Property(x => x.TaxTreatment).HasConversion<int>().IsRequired();
            b.Property(x => x.PreferredSettlementType).HasConversion<int>().IsRequired();

            b.OwnsOne(x => x.ReplayMetadata, rb =>
            {
                rb.Property(r => r.SourceSystem).HasMaxLength(100).HasColumnName("ReplaySourceSystem");
                rb.Property(r => r.OriginalTimestamp).HasColumnName("ReplayOriginalTimestamp");
                rb.Property(r => r.ReplayAttempt).HasColumnName("ReplayAttemptCount");
                rb.Property(r => r.IsSimulated).HasColumnName("IsSimulatedReplay");
            });

            b.OwnsMany(x => x.Holds, hb =>
            {
                hb.ToTable("ExpenseClaimHolds");
                hb.WithOwner().HasForeignKey("ExpenseClaimId");
                hb.Property<Guid>("Id");
                hb.HasKey("Id");
                hb.Property(x => x.Type).HasConversion<int>().IsRequired();
                hb.Property(x => x.Reason).HasMaxLength(500).IsRequired();
                hb.Property(x => x.PlacedBy).HasMaxLength(100).IsRequired();
                hb.Property(x => x.ReleasedBy).HasMaxLength(100);
            });

            b.OwnsMany(x => x.Receipts, rb =>
            {
                rb.ToTable("ExpenseReceipts");
                rb.WithOwner().HasForeignKey("ExpenseClaimId");
                rb.HasKey(x => x.Id);
                rb.Property(x => x.FileName).HasMaxLength(255).IsRequired();
                rb.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
                rb.Property(x => x.StoragePath).HasMaxLength(500).IsRequired();
                rb.Property(x => x.Checksum).HasMaxLength(100).IsRequired();
            });

            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => x.EmployeeId);
            b.HasIndex(x => x.ClaimNumber).IsUnique();
        });

        // MassTransit's transactional outbox entities.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<InboxState>(b => b.ToTable("InboxState", MessagingSchema));
        modelBuilder.Entity<OutboxMessage>(b => b.ToTable("OutboxMessage", MessagingSchema));
        modelBuilder.Entity<OutboxState>(b => b.ToTable("OutboxState", MessagingSchema));
    }
}
