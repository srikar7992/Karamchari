using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.Arrears;
using Karamchari.Payroll.Domain.Corrections;
using Karamchari.Payroll.Domain.FnF;
using Karamchari.Payroll.Domain.Loans;
using Karamchari.Payroll.Domain.Reconciliation;
using Karamchari.Payroll.Domain.Reimbursements;
using Karamchari.Payroll.Services.Reconciliation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Payroll;

/// <summary>
/// Payroll operations cockpit — unified visibility across all payroll workflows.
/// </summary>
public static class PayrollCockpitEndpoints
{
    public static WebApplication MapPayrollCockpitEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/payroll/cockpit").RequireAuthorization();

        group.MapGet("/", GetCockpit).WithName("Cockpit.Summary");
        group.MapGet("/reconciliation", ListReconciliationJobs).WithName("Cockpit.Reconciliation");
        group.MapPost("/reconciliation/run", RunReconciliation).WithName("Cockpit.RunReconciliation");
        group.MapGet("/anomalies", ListAnomalies).WithName("Cockpit.Anomalies");
        group.MapPost("/anomalies/{id:guid}/resolve", ResolveAnomaly).WithName("Cockpit.ResolveAnomaly");
        group.MapPost("/anomalies/{id:guid}/dismiss", DismissAnomaly).WithName("Cockpit.DismissAnomaly");

        return app;
    }

    private static async Task<IResult> GetCockpit(
        PayrollDbContext db,
        CancellationToken ct)
    {
        // Parallel queries for cockpit summary
        var fnfTask = db.Set<FnFSettlement>().AsNoTracking()
            .CountAsync(s => s.Status == FnFStatus.Draft
                || s.Status == FnFStatus.PendingApproval
                || s.Status == FnFStatus.Approved, ct);

        var arrearTask = db.Set<ArrearCalculation>().AsNoTracking()
            .CountAsync(a => a.Status == ArrearStatus.Pending
                || a.Status == ArrearStatus.PendingApproval, ct);

        var correctionTask = db.Set<PayrollCorrection>().AsNoTracking()
            .CountAsync(c => c.Status == CorrectionStatus.Draft
                || c.Status == CorrectionStatus.PendingApproval, ct);

        var anomalyTask = db.Set<ReconciliationJob>().AsNoTracking()
            .SelectMany(j => j.Anomalies)
            .CountAsync(a => a.ResolutionStatus == AnomalyResolutionStatus.Open, ct);

        var loanTask = db.Set<EmployeeLoan>().AsNoTracking()
            .CountAsync(l => l.Status == LoanStatus.Active, ct);

        var reimbTask = db.Set<ReimbursementClaim>().AsNoTracking()
            .CountAsync(c => c.Status == ReimbursementStatus.Submitted
                || c.Status == ReimbursementStatus.PendingApproval, ct);

        await Task.WhenAll(fnfTask, arrearTask, correctionTask, anomalyTask, loanTask, reimbTask);

        var summary = new PayrollCockpitSummaryDto(
            await fnfTask, await arrearTask, await correctionTask,
            await anomalyTask, await loanTask, await reimbTask);

        // FnF queue
        var fnfQueue = await db.Set<FnFSettlement>().AsNoTracking()
            .Where(s => s.Status == FnFStatus.PendingApproval)
            .OrderBy(s => s.CreatedAtUtc)
            .Take(20)
            .Select(s => new PayrollCockpitQueueItemDto(
                s.Id, s.EmployeeId, s.EmployeeName,
                s.ExitType.ToString(), s.Status.ToString(),
                s.NetSettlementAmount, s.CreatedAtUtc))
            .ToListAsync(ct);

        // Arrear queue
        var arrearQueue = await db.Set<ArrearCalculation>().AsNoTracking()
            .Where(a => a.Status == ArrearStatus.PendingApproval)
            .OrderBy(a => a.CreatedAtUtc)
            .Take(20)
            .Select(a => new PayrollCockpitQueueItemDto(
                a.Id, a.EmployeeId, a.EmployeeName,
                a.TriggerType.ToString(), a.Status.ToString(),
                a.TotalNetDelta, a.CreatedAtUtc))
            .ToListAsync(ct);

        // Correction queue
        var correctionQueue = await db.Set<PayrollCorrection>().AsNoTracking()
            .Where(c => c.Status == CorrectionStatus.PendingApproval)
            .OrderBy(c => c.CreatedAtUtc)
            .Take(20)
            .Select(c => new PayrollCockpitQueueItemDto(
                c.Id, c.EmployeeId, c.EmployeeName,
                c.Type.ToString(), c.Status.ToString(),
                c.DifferentialAmount ?? 0, c.CreatedAtUtc))
            .ToListAsync(ct);

        // Open anomalies
        var openAnomalies = await db.Set<ReconciliationJob>().AsNoTracking()
            .SelectMany(j => j.Anomalies)
            .Where(a => a.ResolutionStatus == AnomalyResolutionStatus.Open)
            .OrderByDescending(a => (int)a.Severity)
            .Take(20)
            .Select(a => new PayrollAnomalyDto(
                a.Id, a.Type.ToString(), a.Severity.ToString(),
                a.ResolutionStatus.ToString(),
                a.EmployeeId ?? Guid.Empty, a.EmployeeName ?? "Unknown",
                a.Description, a.AnomalyScore))
            .ToListAsync(ct);

        return Results.Ok(new PayrollCockpitDto(summary, fnfQueue, arrearQueue, correctionQueue, openAnomalies));
    }

    private static async Task<IResult> ListReconciliationJobs(
        PayrollDbContext db,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var total = await db.Set<ReconciliationJob>().CountAsync(ct);
        var jobs = await db.Set<ReconciliationJob>().AsNoTracking()
            .OrderByDescending(j => j.StartedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new ReconciliationJobDto(
                j.Id, j.PeriodName, j.Status.ToString(),
                j.EmployeesScanned, j.TotalAnomalies,
                j.CriticalCount, j.WarningCount, j.AnomalyScore,
                j.StartedAtUtc, j.CompletedAtUtc))
            .ToListAsync(ct);

        return Results.Ok(new { Items = jobs, Total = total, Page = page });
    }

    private static async Task<IResult> RunReconciliation(
        [FromBody] RunReconciliationRequest request,
        PayrollDbContext db,
        PayrollReconciliationService reconciliation,
        CancellationToken ct)
    {
        var job = await reconciliation.RunAsync(
            request.TenantId, request.PeriodName,
            request.Year, request.Month, ct);

        db.Set<ReconciliationJob>().Add(job);
        await db.SaveChangesAsync(ct);

        return Results.Ok(new ReconciliationJobDto(
            job.Id, job.PeriodName, job.Status.ToString(),
            job.EmployeesScanned, job.TotalAnomalies,
            job.CriticalCount, job.WarningCount, job.AnomalyScore,
            job.StartedAtUtc, job.CompletedAtUtc));
    }

    private static async Task<IResult> ListAnomalies(
        PayrollDbContext db,
        [FromQuery] string? severity,
        [FromQuery] bool openOnly = true,
        CancellationToken ct = default)
    {
        var query = db.Set<ReconciliationJob>().AsNoTracking()
            .SelectMany(j => j.Anomalies);

        if (openOnly)
            query = query.Where(a => a.ResolutionStatus == AnomalyResolutionStatus.Open);

        if (!string.IsNullOrEmpty(severity) && Enum.TryParse<AnomalySeverity>(severity, out var s))
            query = query.Where(a => a.Severity == s);

        var anomalies = await query
            .OrderByDescending(a => (int)a.Severity)
            .Select(a => new PayrollAnomalyDto(
                a.Id, a.Type.ToString(), a.Severity.ToString(),
                a.ResolutionStatus.ToString(),
                a.EmployeeId ?? Guid.Empty, a.EmployeeName ?? "Unknown",
                a.Description, a.AnomalyScore))
            .ToListAsync(ct);

        return Results.Ok(anomalies);
    }

    private static async Task<IResult> ResolveAnomaly(
        Guid id,
        [FromBody] AnomalyResolutionRequest request,
        ClaimsPrincipal user,
        HttpRequest httpRequest,
        PayrollDbContext db,
        CancellationToken ct)
    {
        var anomaly = await db.Set<ReconciliationJob>()
            .SelectMany(j => j.Anomalies)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (anomaly is null) return Results.NotFound();

        anomaly.Resolve(user.GetEmployeeIdString(httpRequest) ?? "system", request.Note);
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> DismissAnomaly(
        Guid id,
        [FromBody] AnomalyResolutionRequest request,
        ClaimsPrincipal user,
        HttpRequest httpRequest,
        PayrollDbContext db,
        CancellationToken ct)
    {
        var anomaly = await db.Set<ReconciliationJob>()
            .SelectMany(j => j.Anomalies)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (anomaly is null) return Results.NotFound();

        anomaly.Dismiss(user.GetEmployeeIdString(httpRequest) ?? "system", request.Note);
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }
}

public record RunReconciliationRequest(Guid TenantId, string PeriodName, int Year, int Month);
public record AnomalyResolutionRequest(string Note);
