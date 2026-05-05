namespace Karamchari.Payroll.Persistence;

using Karamchari.Core.Persistence.Provisioning;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Post-provisioning task that creates critical indexes on the <c>PayrollLedger</c> table
/// in a newly provisioned tenant schema.
/// </summary>
/// <remarks>
/// <para>
/// <c>SELECT * INTO</c> (used by <see cref="TenantProvisioningService"/>) copies column
/// structure but not indexes. This task applies the indexes that the EF model defines for
/// <c>PayrollLedgerEntry</c> so that tenant schemas match the <c>dbo</c> template.
/// </para>
/// <para>
/// All SQL is idempotent: wrapped in <c>IF NOT EXISTS</c> guards so the task is safe to
/// run repeatedly (e.g., on provisioning retry after a partial failure).
/// </para>
/// </remarks>
public sealed class PayrollLedgerIndexProvisioningTask : ITenantPostProvisioningTask
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(string schemaName, DbContext dbContext, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);
        ArgumentNullException.ThrowIfNull(dbContext);

        // Index 1: Unique composite index on (RunId, EmployeeId).
        // Prevents duplicate ledger entries for the same employee in the same payroll run.
        // This is the database-level idempotency guard that closes the race window
        // between the application-level check in PayrollBatchConsumer and BulkInsertAsync.
        // EF model: HasIndex(x => new { x.RunId, x.EmployeeId }).IsUnique()
        //           → conventional name: IX_PayrollLedger_RunId_EmployeeId
#pragma warning disable EF1002 // schemaName is validated by TenantProvisioningService before this task runs
        await dbContext.Database.ExecuteSqlRawAsync($"""
            IF NOT EXISTS (
                SELECT 1
                FROM   sys.indexes      i
                JOIN   sys.tables       t ON i.object_id = t.object_id
                JOIN   sys.schemas      s ON t.schema_id = s.schema_id
                WHERE  s.name = '{schemaName}'
                AND    t.name = 'PayrollLedger'
                AND    i.name = 'IX_PayrollLedger_RunId_EmployeeId'
            )
            CREATE UNIQUE INDEX [IX_PayrollLedger_RunId_EmployeeId]
                ON [{schemaName}].[PayrollLedger] ([RunId], [EmployeeId]);
            """);

        // Index 2: Non-unique index on (EmployeeId, FinancialYearStart).
        // The primary YTD read path — also defined in the EF model.
        // EF model: HasIndex(x => new { x.EmployeeId, x.FinancialYearStart })
        //           → conventional name: IX_PayrollLedger_EmployeeId_FinancialYearStart
        await dbContext.Database.ExecuteSqlRawAsync($"""
            IF NOT EXISTS (
                SELECT 1
                FROM   sys.indexes      i
                JOIN   sys.tables       t ON i.object_id = t.object_id
                JOIN   sys.schemas      s ON t.schema_id = s.schema_id
                WHERE  s.name = '{schemaName}'
                AND    t.name = 'PayrollLedger'
                AND    i.name = 'IX_PayrollLedger_EmployeeId_FinancialYearStart'
            )
            CREATE INDEX [IX_PayrollLedger_EmployeeId_FinancialYearStart]
                ON [{schemaName}].[PayrollLedger] ([EmployeeId], [FinancialYearStart]);
            """);
#pragma warning restore EF1002
    }
}
