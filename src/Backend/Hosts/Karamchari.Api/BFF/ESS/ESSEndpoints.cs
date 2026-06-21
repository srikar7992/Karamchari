// -----------------------------------------------------------------------
// <copyright file="ESSEndpoints.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Api.BFF.Common;
using Karamchari.Approvals.Domain;
using Karamchari.Approvals.Persistence;
using Karamchari.Core.Security;
using Karamchari.HR.Services;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain;
using Karamchari.Payroll.Domain.SalaryStructures;
using Karamchari.Payroll.Domain.Statutory;
using Karamchari.Payroll.Services;
using Karamchari.Payroll.Services.Declarations;
using Karamchari.Payroll.Services.Payslip;
using Karamchari.Payroll.Services.Statutory;
using Karamchari.Payroll.Services.Statutory.Rules;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Persistence;
using Karamchari.Benefits.Domain;
using Karamchari.Benefits.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.ESS;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class ESSEndpoints
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WebApplication MapESSEndpoints(this WebApplication app)
    {
        var ess = app.MapGroup("/api/ess").RequireAuthorization();

        ess.MapGet("/profile", GetProfile);
        ess.MapGet("/payslips", GetPayslips);
        ess.MapGet("/payslips/{year}/{month}/download", DownloadPayslip);
        ess.MapGet("/payslips/ytd", GetYtdSummary);
        ess.MapPost("/tax-simulator/dry-run", SimulateTax).RequireRateLimiting("ess");
        ess.MapPost("/declarations/analyze", AnalyzeDeclaration).RequireRateLimiting("ai");

        // Leave — self-service (Sprint 1 ESS Foundation)
        var me = ess.MapGroup("/me").AddEndpointFilter<EmployeeScopeGuard>();
        me.MapGet("/leave-balances", GetMyLeaveBalances).RequireAuthorization(Permissions.EssSelfRead);
        me.MapGet("/leave-requests", GetMyLeaveRequests).RequireAuthorization(Permissions.EssSelfRead);
        me.MapPost("/leave-requests", SubmitLeaveRequest).RequireAuthorization(Permissions.EssSelfWrite);

        // Employee profile — self-service (Sprint 2)
        me.MapGet("", GetMyProfile).RequireAuthorization(Permissions.EssSelfRead);
        me.MapPatch("", PatchMyProfile).RequireAuthorization(Permissions.EssSelfWrite);

        // Delegation — self-service (Sprint 2)
        me.MapGet("/delegation", GetMyActiveDelegation).RequireAuthorization(Permissions.EssSelfRead);
        me.MapPost("/delegation", CreateMyDelegation).RequireAuthorization(Permissions.EssDelegationWrite);
        me.MapDelete("/delegation/{id:guid}", RevokeMyDelegation).RequireAuthorization(Permissions.EssDelegationWrite);
        me.MapGet("/learning", GetMyLearning).RequireAuthorization(Permissions.EssSelfRead);
        me.MapGet("/benefits", GetMyBenefits).RequireAuthorization(Permissions.EssSelfRead);

        // Manager inbox (Sprint 1 ESS Foundation)
        var manager = app.MapGroup("/api/ess/manager").RequireAuthorization();
        manager.MapGet("/inbox", GetManagerInbox).RequireAuthorization(Permissions.EssManagerInbox);
        manager.MapPost("/inbox/{requestId:guid}/approve", ActOnApproval).RequireAuthorization(Permissions.EssManagerApprove);
        manager.MapPost("/inbox/{requestId:guid}/reject", ActOnApproval).RequireAuthorization(Permissions.EssManagerApprove);

        return app;
    }

    private static async Task<IResult> GetProfile(
        ClaimsPrincipal user,
        EmployeeSummaryService summaryService,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId == null) return Results.Unauthorized();

        var summary = await summaryService.GetSummaryAsync(employeeId.Value, ct);
        return summary == null ? Results.NotFound() : Results.Ok(summary);
    }

    private static async Task<IResult> GetPayslips(ClaimsPrincipal user, PayrollDbContext dbContext)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        var payslips = await dbContext.PayrollLedger
            .Where(e => e.TenantId == tenantId && e.EmployeeId == employeeId)
            .OrderByDescending(e => e.Year)
            .ThenByDescending(e => e.Month)
            .Select(e => new
            {
                e.Id,
                e.Year,
                e.Month,
                e.PeriodName,
                e.MonthlyGross,
                e.NetPay,
                e.TdsDeducted
            })
            .ToListAsync();
        return Results.Ok(payslips);
    }

    private static async Task<IResult> DownloadPayslip(
        int year,
        int month,
        ClaimsPrincipal user,
        PayrollDbContext dbContext,
        IPayslipStorage storage)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        var ledger = await dbContext.PayrollLedger
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.EmployeeId == employeeId && e.Year == year && e.Month == month);

        if (ledger == null) return Results.NotFound("Payslip not found or access denied.");

        try
        {
            var pdfBytes = await storage.GetAsync(employeeId.ToString()!, ledger.PeriodName);
            var fileName = $"Payslip_{ledger.PeriodName.Replace(" ", "_")}.pdf";
            return Results.File(pdfBytes, "application/pdf", fileName);
        }
        catch (FileNotFoundException)
        {
            return Results.NotFound("Physical payslip file missing.");
        }
    }

    private static async Task<IResult> GetYtdSummary(ClaimsPrincipal user, PayrollDbContext dbContext)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        var now = DateTime.UtcNow;
        var fyStartYear = now.Month >= 4 ? now.Year : now.Year - 1;

        var ytd = await dbContext.PayrollLedger
            .Where(e => e.TenantId == tenantId && e.EmployeeId == employeeId && e.FinancialYearStart == fyStartYear)
            .GroupBy(e => e.EmployeeId)
            .Select(g => new
            {
                GrossYtd = g.Sum(x => x.MonthlyGross),
                NetYtd = g.Sum(x => x.NetPay),
                TaxYtd = g.Sum(x => x.TdsDeducted),
                Count = g.Count()
            })
            .FirstOrDefaultAsync();

        return Results.Ok(ytd ?? new { GrossYtd = 0m, NetYtd = 0m, TaxYtd = 0m, Count = 0 });
    }

    private static async Task<IResult> SimulateTax(
        TaxSimulationRequest request,
        ClaimsPrincipal user,
        PayrollDbContext dbContext,
        IServiceProvider sp)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        if (request.Section80C > 150000) return Results.BadRequest("Section 80C declaration cannot exceed â‚¹1,50,000.");
        if (request.MonthlyRent < 0) return Results.BadRequest("Monthly rent cannot be negative.");

        var profile = await dbContext.PayrollProfiles.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.EmployeeId == employeeId);
        if (profile == null) return Results.NotFound("Employee payroll profile not found.");

        var template = await dbContext.SalaryTemplates.FindAsync(profile.SalaryTemplateId);
        if (template == null) return Results.NotFound("Salary template not found.");

        var masterComponents = await dbContext.SalaryComponents.ToListAsync();
        var plan = CTCTemplateCompiler.Compile(template, masterComponents);
        var breakdown = CTCBreakdownService.Calculate(profile.AnnualCTC, plan);

        var fy = new FinancialYear(2026, 2027);
        var ephemeralDeclarations = new List<ITDeclaration>();
        if (request.Section80C > 0)
            ephemeralDeclarations.Add(ITDeclaration.Create(employeeId.Value, fy.StartYear, "80C", request.Section80C, "simulator", "simulator"));
        if (request.Section80D > 0)
            ephemeralDeclarations.Add(ITDeclaration.Create(employeeId.Value, fy.StartYear, "80D", request.Section80D, "simulator", "simulator"));
        if (request.MonthlyRent > 0)
            ephemeralDeclarations.Add(ITDeclaration.Create(employeeId.Value, fy.StartYear, "HRA", request.MonthlyRent * 12, "simulator", "simulator"));

        var ptProvider = sp.GetRequiredService<IProfessionalTaxProvider>();
        var projectionService = sp.GetRequiredService<IIncomeProjectionService>();
        var exemptionCalculator = sp.GetRequiredService<IExemptionCalculator>();
        var taxSlabProvider = sp.GetRequiredService<ITaxSlabProvider>();

        var staticRepo = new StaticDeclarationRepository(ephemeralDeclarations);
        var ruleSet = new FY20262027RuleSet(
            new List<Guid> { employeeId.Value },
            ptProvider, projectionService, exemptionCalculator, taxSlabProvider, staticRepo);

        var oldProfile = profile.CloneWithRegime(TaxRegime.Old);
        var oldContext = new StatutoryContext(breakdown, oldProfile, fy, DateTime.UtcNow.Month);
        var oldResult = await StatutoryPipelineEngine.ExecuteAsync(oldContext, ruleSet);

        var newProfile = profile.CloneWithRegime(TaxRegime.New);
        var newContext = new StatutoryContext(breakdown, newProfile, fy, DateTime.UtcNow.Month);
        var newResult = await StatutoryPipelineEngine.ExecuteAsync(newContext, ruleSet);

        decimal oldTax = oldResult.Deductions.GetValueOrDefault("TDS", 0);
        decimal newTax = newResult.Deductions.GetValueOrDefault("TDS", 0);

        return Results.Ok(new TaxSimulationResult(
            oldTax,
            newTax,
            Math.Abs(oldTax - newTax),
            oldTax < newTax ? "Old Regime" : "New Regime",
            oldTax < newTax ? "Based on your deductions, the Old Regime provides higher savings." : "The New Regime is more beneficial due to lower overall tax rates."
        ));
    }

    private static async Task<IResult> AnalyzeDeclaration(
        IFormFile file,
        ClaimsPrincipal user,
        IDocumentAnalyzer analyzer,
        IProofStorage storage)
    {
        if (file == null || file.Length == 0) return Results.BadRequest("No file uploaded.");
        if (file.Length > 5 * 1024 * 1024) return Results.BadRequest("File size exceeds 5MB limit.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!ProofAllowedExtensions.Contains(extension)) return Results.BadRequest("Invalid file type. Only PDF, JPG, and PNG are supported.");

        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        var financialYear = 2026;

        using var stream = file.OpenReadStream();
        var storageUri = await storage.SaveAsync(stream, file.FileName, tenantId, employeeId.Value, financialYear);

        stream.Position = 0;
        var result = await analyzer.AnalyzeAsync(stream, file.FileName);

        return Results.Ok(result with { StorageUri = storageUri });
    }

    private static async Task<IResult> GetMyProfile(
        HttpContext httpContext,
        [FromServices] EmployeeSummaryService summaryService,
        CancellationToken ct)
    {
        var employeeId = httpContext.GetScopedEmployeeId();
        var summary = await summaryService.GetSummaryAsync(employeeId, ct);
        return summary == null ? Results.NotFound() : Results.Ok(summary);
    }

    private static Task<IResult> PatchMyProfile(
        HttpContext httpContext,
        [FromBody] EssPatchProfileBody body)
    {
        // Wire to HR Employee aggregate once self-service field set confirmed.
        _ = httpContext.GetScopedEmployeeId();
        return Task.FromResult(Results.StatusCode(501));
    }

    private static async Task<IResult> GetMyActiveDelegation(
        HttpContext httpContext,
        [FromServices] ApprovalsDbContext db,
        CancellationToken ct)
    {
        var managerId = httpContext.GetScopedEmployeeId();
        var tenantId = httpContext.User.FindFirst("tenant_id")?.Value ?? string.Empty;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var delegations = await db.ManagerDelegations
            .AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.DelegatingManagerId == managerId && d.IsActive && d.EffectiveTo >= today)
            .Select(d => new EssDelegationDto(d.Id, d.DelegateManagerId, d.EffectiveFrom, d.EffectiveTo, d.CreatedAtUtc))
            .ToListAsync(ct);

        return Results.Ok(delegations);
    }

    private static async Task<IResult> CreateMyDelegation(
        HttpContext httpContext,
        [FromBody] EssCreateDelegationBody body,
        [FromServices] ApprovalsDbContext db,
        CancellationToken ct)
    {
        var managerId = httpContext.GetScopedEmployeeId();
        var tenantId = httpContext.User.FindFirst("tenant_id")?.Value ?? string.Empty;

        try
        {
            var delegation = ManagerDelegation.Create(tenantId, managerId, body.DelegateManagerId, body.EffectiveFrom, body.EffectiveTo);
            db.ManagerDelegations.Add(delegation);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/ess/me/delegation/{delegation.Id}", new { delegationId = delegation.Id });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private static async Task<IResult> RevokeMyDelegation(
        Guid id,
        HttpContext httpContext,
        [FromServices] ApprovalsDbContext db,
        CancellationToken ct)
    {
        var managerId = httpContext.GetScopedEmployeeId();
        var tenantId = httpContext.User.FindFirst("tenant_id")?.Value ?? string.Empty;

        var delegation = await db.ManagerDelegations
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId && d.DelegatingManagerId == managerId, ct);

        if (delegation == null) return Results.NotFound();

        delegation.Revoke();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

        private static async Task<IResult> GetMyLearning(
        HttpContext httpContext,
        [FromServices] Karamchari.Capability.Persistence.CapabilityDbContext capDb,
        CancellationToken ct)
    {
        var employeeId = httpContext.GetScopedEmployeeId();
        var enrollments = await capDb.LearningEnrollments
            .AsNoTracking()
            .Where(e => e.EmployeeId == employeeId)
            .OrderBy(e => e.Status)
            .Select(e => new { e.Id, e.ModuleId, Status = e.Status.ToString(), e.CompletedAtUtc })
            .ToListAsync(ct);
        return Results.Ok(enrollments);
    }

    private static async Task<IResult> GetMyBenefits(
        HttpContext httpContext,
        [FromServices] BenefitsDbContext benefitsDb,
        CancellationToken ct)
    {
        var employeeId = httpContext.GetScopedEmployeeId();
        var enrollments = await benefitsDb.Enrollments
            .AsNoTracking()
            .Where(e => e.EmployeeId == employeeId && e.Status != EnrollmentStatus.WaivedAllCoverage)
            .Select(e => new { e.Id, e.Status, e.PlanYearStart, e.PlanYearEnd, e.SubmittedAtUtc, e.ActivatedAtUtc })
            .ToListAsync(ct);
        return Results.Ok(enrollments);
    }

        // ── Leave self-service handlers ─────────────────────────────────────────

    private static async Task<IResult> GetMyLeaveBalances(
        HttpContext httpContext,
        [FromServices] TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var employeeId = httpContext.GetScopedEmployeeId();
        var balances = await db.LeaveBalanceReadModels
            .AsNoTracking()
            .Where(b => b.EmployeeId == employeeId)
            .Select(b => new EssLeaveBalanceDto(b.PolicyId, b.PolicyName, b.AvailableBalance, b.ConsumedBalance, b.LastUpdatedAtUtc))
            .ToListAsync(ct);
        return Results.Ok(balances);
    }

    private static async Task<IResult> GetMyLeaveRequests(
        HttpContext httpContext,
        [FromServices] TimeAttendanceDbContext db,
        CancellationToken ct)
    {
        var employeeId = httpContext.GetScopedEmployeeId();
        var requests = await db.LeaveRequests
            .AsNoTracking()
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.RequestedOnUtc)
            .Take(50)
            .Select(r => new EssLeaveRequestDto(r.Id, r.PolicyId, r.StartDate, r.EndDate, r.ActualDays, r.Status.ToString(), r.Reason, r.RequestedOnUtc))
            .ToListAsync(ct);
        return Results.Ok(requests);
    }

    private static async Task<IResult> SubmitLeaveRequest(
        HttpContext httpContext,
        [FromBody] EssSubmitLeaveBody body,
        [FromServices] TimeAttendanceDbContext db,
        [FromServices] ApprovalsDbContext approvalsDb,
        CancellationToken ct)
    {
        var employeeId = httpContext.GetScopedEmployeeId();
        var tenantId = httpContext.User.FindFirst("tenant_id")?.Value ?? string.Empty;

        var holidays = await db.HolidayCalendars
            .AsNoTracking()
            .SelectMany(c => c.Holidays)
            .Select(h => h.Date)
            .ToListAsync(ct);

        var actualDays = LeaveRequestService.CalculateActualLeaveDays(
            body.StartDate, body.EndDate, holidays, allowHalfDays: false);

        if (actualDays <= 0)
            return Results.BadRequest("Selected date range contains no working days.");

        var leaveRequest = LeaveRequest.Create(employeeId, body.PolicyId, body.StartDate, body.EndDate, actualDays, body.Reason);
        db.LeaveRequests.Add(leaveRequest);

        var approvalRequest = ApprovalRequest.Create(
            tenantId,
            submittedBy: employeeId,
            approverId: body.ApproverId ?? Guid.Empty,
            requestType: ApprovalRequestType.LeaveRequest,
            subjectEntityId: leaveRequest.Id,
            subjectDescription: $"Leave {body.StartDate:d} – {body.EndDate:d} ({actualDays} days)");
        approvalsDb.ApprovalRequests.Add(approvalRequest);

        await db.SaveChangesAsync(ct);
        await approvalsDb.SaveChangesAsync(ct);

        return Results.Created($"/api/ess/me/leave-requests/{leaveRequest.Id}",
            new { leaveRequestId = leaveRequest.Id, approvalRequestId = approvalRequest.Id });
    }

    // ── Manager inbox handlers ──────────────────────────────────────────────

    private static async Task<IResult> GetManagerInbox(
        ClaimsPrincipal user,
        [FromServices] ApprovalsDbContext approvalsDb,
        CancellationToken ct)
    {
        var (_, managerId) = user.GetTenantAndEmployee();
        if (managerId is null) return Results.Forbid();

        var pending = await approvalsDb.ApprovalRequests
            .AsNoTracking()
            .Where(r => r.ApproverId == managerId && r.Status == ApprovalRequestStatus.Pending)
            .OrderBy(r => r.SubmittedAtUtc)
            .Select(r => new EssApprovalInboxItemDto(r.Id, r.RequestType.ToString(), r.SubjectEntityId, r.SubjectDescription, r.SubmittedBy, r.SubmittedAtUtc))
            .ToListAsync(ct);

        return Results.Ok(pending);
    }

    private static async Task<IResult> ActOnApproval(
        Guid requestId,
        ClaimsPrincipal user,
        [FromBody] EssApprovalActionBody body,
        [FromServices] ApprovalsDbContext approvalsDb,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var (_, actorId) = user.GetTenantAndEmployee();
        if (actorId is null) return Results.Forbid();

        var actorName = user.FindFirst(ClaimTypes.Name)?.Value ?? actorId.ToString()!;

        var request = await approvalsDb.ApprovalRequests.FindAsync([requestId], ct);
        if (request == null) return Results.NotFound();

        var path = httpContext.Request.Path.Value ?? string.Empty;
        try
        {
            if (path.EndsWith("/approve", StringComparison.OrdinalIgnoreCase))
                request.Approve(actorId.Value, actorName, body.Comment);
            else
                request.Reject(actorId.Value, actorName, body.Comment);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }

        await approvalsDb.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}

// ── ESS Leave / Approval DTOs ────────────────────────────────────────────────

public record EssSubmitLeaveBody(
    Guid PolicyId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Reason,
    Guid? ApproverId);

public record EssApprovalActionBody(string? Comment);

public record EssLeaveBalanceDto(
    Guid PolicyId,
    string PolicyName,
    decimal AvailableBalance,
    decimal ConsumedBalance,
    DateTimeOffset LastUpdatedAt);

public record EssLeaveRequestDto(
    Guid Id,
    Guid PolicyId,
    DateOnly StartDate,
    DateOnly EndDate,
    double ActualDays,
    string Status,
    string? Reason,
    DateTime RequestedOnUtc);

public record EssApprovalInboxItemDto(
    Guid RequestId,
    string RequestType,
    Guid SubjectEntityId,
    string? SubjectDescription,
    Guid SubmittedBy,
    DateTimeOffset SubmittedAtUtc);

public record EssPatchProfileBody(
    string? PreferredName,
    string? PersonalEmail,
    string? EmergencyContactName,
    string? EmergencyContactPhone);

public record EssCreateDelegationBody(
    Guid DelegateManagerId,
    DateOnly EffectiveFrom,
    DateOnly EffectiveTo);

public record EssDelegationDto(
    Guid Id,
    Guid DelegateManagerId,
    DateOnly EffectiveFrom,
    DateOnly EffectiveTo,
    DateTimeOffset CreatedAtUtc);
