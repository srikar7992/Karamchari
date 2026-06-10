// -----------------------------------------------------------------------
// <copyright file="LoanEndpoints.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Api.Middleware;
using Karamchari.Payroll.Contracts;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.Loans;
using Karamchari.Payroll.Services.Loans;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Payroll;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class LoanEndpoints
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WebApplication MapLoanEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/payroll/loans").RequireAuthorization();

        group.MapPost("/", RequestLoan).WithName("Loan.Request").WithIdempotency();
        group.MapGet("/{id:guid}", GetLoan).WithName("Loan.Get");
        group.MapGet("/", ListLoans).WithName("Loan.List");
        group.MapGet("/{id:guid}/schedule", GetSchedule).WithName("Loan.Schedule");
        group.MapPost("/{id:guid}/pre-close", PreCloseLoan).WithName("Loan.PreClose").WithIdempotency();

        return app;
    }

    private static async Task<IResult> RequestLoan(
        [FromBody] CreateLoanRequest request,
        ClaimsPrincipal user,
        PayrollDbContext db,
        IPublishEndpoint bus,
        CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        if (!Enum.TryParse<LoanType>(request.LoanType, out var loanType))
            return Results.BadRequest($"Invalid loan type: {request.LoanType}");

        if (!Enum.TryParse<LoanInterestType>(request.InterestType, out var interestType))
            return Results.BadRequest($"Invalid interest type: {request.InterestType}");

        var loan = EmployeeLoan.Request(
            tenantId, request.EmployeeId, request.EmployeeName,
            loanType, interestType,
            request.PrincipalAmount, request.InterestRatePercent,
            request.TenureMonths, request.DisbursedOn);

        var schedule = LoanAmortizationEngine.GenerateSchedule(loan, request.DisbursedOn);
        loan.SetSchedule(schedule.Installments, schedule.MonthlyEmi);

        db.Set<EmployeeLoan>().Add(loan);
        await db.SaveChangesAsync(ct);

        await bus.Publish(new LoanCreatedIntegrationEvent
        {
            LoanId = loan.Id,
            TenantId = tenantId,
            EmployeeId = request.EmployeeId,
            LoanType = request.LoanType,
            PrincipalAmount = request.PrincipalAmount,
            TenureMonths = request.TenureMonths,
            OccurredOnUtc = DateTimeOffset.UtcNow
        }, ct);

        return Results.Created($"/api/v1/payroll/loans/{loan.Id}", MapToDto(loan));
    }

    private static async Task<IResult> GetLoan(
        Guid id, PayrollDbContext db, CancellationToken ct)
    {
        var loan = await db.Set<EmployeeLoan>()
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        return loan is null ? Results.NotFound() : Results.Ok(MapToDto(loan));
    }

    private static async Task<IResult> ListLoans(
        PayrollDbContext db,
        [FromQuery] Guid? employeeId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = db.Set<EmployeeLoan>().AsNoTracking();

        if (employeeId.HasValue)
            query = query.Where(l => l.EmployeeId == employeeId.Value);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<LoanStatus>(status, out var s))
            query = query.Where(l => l.Status == s);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(l => l.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Results.Ok(new { Items = items.Select(MapToDto), Total = total, Page = page });
    }

    private static async Task<IResult> GetSchedule(
        Guid id, PayrollDbContext db, CancellationToken ct)
    {
        var loan = await db.Set<EmployeeLoan>()
            .AsNoTracking()
            .Include(l => l.Installments)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        if (loan is null) return Results.NotFound();

        var schedule = loan.Installments
            .OrderBy(i => i.InstallmentNumber)
            .Select(i => new LoanInstallmentDto(
                i.InstallmentNumber, i.PeriodName,
                i.PrincipalAmount, i.InterestAmount, i.TotalAmount,
                i.OutstandingAfter, i.Status.ToString()))
            .ToList();

        return Results.Ok(schedule);
    }

    private static async Task<IResult> PreCloseLoan(
        Guid id,
        [FromBody] decimal paymentAmount,
        PayrollDbContext db,
        IPublishEndpoint bus,
        CancellationToken ct)
    {
        var loan = await db.Set<EmployeeLoan>()
            .Include(l => l.Installments)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        if (loan is null) return Results.NotFound();

        loan.PreClose(paymentAmount);
        await db.SaveChangesAsync(ct);

        await bus.Publish(new LoanClosedIntegrationEvent
        {
            LoanId = loan.Id,
            TenantId = loan.TenantId,
            EmployeeId = loan.EmployeeId,
            ClosureType = LoanStatus.PreClosed.ToString(),
            OccurredOnUtc = DateTimeOffset.UtcNow
        }, ct);

        return Results.Ok(MapToDto(loan));
    }

    private static LoanDto MapToDto(EmployeeLoan l) =>
        new(l.Id, l.EmployeeId, l.EmployeeName, l.Type.ToString(),
            l.Status.ToString(), l.PrincipalAmount, l.OutstandingBalance,
            l.MonthlyEmi, l.TenureMonths, l.DisbursedOn);
}
