using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Intelligence.Services;

/// <summary>
/// Records ground-truth outcome labels for ML training data collection.
/// Called by the consumer when terminal events occur (termination, transfer, etc.).
/// </summary>
public sealed class OutcomeLabelService
{
    private readonly IntelligenceDbContext _db;

    public OutcomeLabelService(IntelligenceDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Records an outcome label with the employee's current scores at the time of the event.
    /// Idempotent per (employeeId, outcome, outcomeDate).
    /// </summary>
    public async Task RecordOutcomeAsync(
        string tenantId,
        Guid employeeId,
        WorkforceOutcome outcome,
        DateOnly outcomeDate,
        string notes = "",
        CancellationToken ct = default)
    {
        var alreadyRecorded = await _db.WorkforceOutcomeLabels
            .AnyAsync(l => l.TenantId == tenantId
                        && l.EmployeeId == employeeId
                        && l.Outcome == outcome
                        && l.OutcomeDate == outcomeDate, ct);

        if (alreadyRecorded) return;

        var burnoutScore = await _db.WorkforceBurnoutScores
            .Where(s => s.TenantId == tenantId && s.EmployeeId == employeeId)
            .Select(s => s.Score)
            .FirstOrDefaultAsync(ct);

        var attritionScore = await _db.WorkforceAttritionScores
            .Where(s => s.TenantId == tenantId && s.EmployeeId == employeeId)
            .Select(s => s.Score)
            .FirstOrDefaultAsync(ct);

        _db.WorkforceOutcomeLabels.Add(WorkforceOutcomeLabel.Record(
            tenantId, employeeId, outcome, outcomeDate,
            burnoutScore, attritionScore, notes));

        await _db.SaveChangesAsync(ct);
    }
}
