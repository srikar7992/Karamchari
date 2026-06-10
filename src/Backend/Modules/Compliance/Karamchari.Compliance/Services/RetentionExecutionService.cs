// -----------------------------------------------------------------------
// <copyright file="RetentionExecutionService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Compliance.Domain.Retention;
using Karamchari.Compliance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Compliance.Services;

/// <summary>
/// Creates RetentionAction records for records that have passed their archive/delete/anonymize thresholds.
/// Checks legal hold before scheduling any destructive action.
/// Idempotent: skips if a Scheduled or Completed action already exists for the same record+action.
/// </summary>
public sealed class RetentionExecutionService
{
    private readonly ComplianceDbContext _db;
    private readonly LegalHoldService _legalHoldService;
    private readonly ILogger<RetentionExecutionService> _logger;

    public RetentionExecutionService(
        ComplianceDbContext db,
        LegalHoldService legalHoldService,
        ILogger<RetentionExecutionService> logger)
    {
        _db = db;
        _legalHoldService = legalHoldService;
        _logger = logger;
    }

    /// <summary>
    /// Schedules a retention action for a specific record, checking legal hold first.
    /// If the record is under hold, the action is recorded as Skipped with reason "LegalHold".
    /// </summary>
    public async Task ScheduleActionAsync(
        string tenantId,
        RetentionRecordType recordType,
        string recordId,
        RetentionActionType actionType,
        DateTime scheduledFor,
        CancellationToken ct = default)
    {
        var recordGuid = Guid.Parse(recordId);

        // Idempotency: check if action already exists
        var existing = await _db.RetentionActions
            .Where(a => a.TenantId == tenantId
                     && a.RecordType == recordType
                     && a.RecordId == recordGuid
                     && a.Action == actionType
                     && (a.Status == RetentionActionStatus.Scheduled || a.Status == RetentionActionStatus.Completed))
            .AnyAsync(ct);

        if (existing) return;

        var underHold = await _legalHoldService.IsUnderHoldAsync(tenantId, recordType, recordId, ct);
        var action = RetentionAction.Schedule(tenantId, recordType, recordGuid, actionType, scheduledFor);

        if (underHold)
        {
            action.MarkSkipped("LegalHold");
            _logger.LogDebug("Retention action {Action} skipped for {RecordType}/{RecordId}: active legal hold",
                actionType, recordType, recordId);
        }

        _db.RetentionActions.Add(action);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Processes all Scheduled retention actions that are past their ScheduledFor date.
    /// Re-checks legal hold at execution time. Marks each action Completed or Skipped.
    /// Actual archive/delete/anonymize operations are logged -- external storage adapters handle physical execution.
    /// </summary>
    public async Task ExecutePendingActionsAsync(string tenantId, CancellationToken ct = default)
    {
        var due = await _db.RetentionActions
            .Where(a => a.TenantId == tenantId
                     && a.Status == RetentionActionStatus.Scheduled
                     && a.ScheduledFor <= DateTime.UtcNow)
            .ToListAsync(ct);

        int executed = 0, skipped = 0;
        foreach (var action in due)
        {
            var underHold = await _legalHoldService.IsUnderHoldAsync(
                tenantId, action.RecordType, action.RecordId.ToString(), ct);

            if (underHold)
            {
                action.MarkSkipped("LegalHold");
                skipped++;
            }
            else
            {
                // Physical execution delegated to storage layer; record completion here
                action.MarkCompleted();
                executed++;
                _logger.LogInformation("Retention action {Action} executed: {RecordType}/{RecordId}",
                    action.Action, action.RecordType, action.RecordId);
            }
        }

        if (due.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Retention execution: {Executed} executed, {Skipped} skipped (legal hold)",
                executed, skipped);
        }
    }
}
