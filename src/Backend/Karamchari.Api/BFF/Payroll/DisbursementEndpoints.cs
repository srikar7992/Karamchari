using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Payroll.Contracts;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.Disbursement;
using Karamchari.Payroll.Services.Disbursement;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Payroll;

public static class DisbursementEndpoints
{
    public static WebApplication MapDisbursementEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/payroll/disbursements").RequireAuthorization();

        group.MapPost("/", InitiateDisbursement).WithName("Disbursement.Initiate");
        group.MapGet("/{id:guid}", GetBatch).WithName("Disbursement.Get");
        group.MapGet("/", ListBatches).WithName("Disbursement.List");
        group.MapPost("/{id:guid}/retry", RetryBatch).WithName("Disbursement.Retry");

        return app;
    }

    private static async Task<IResult> InitiateDisbursement(
        [FromBody] InitiateDisbursementRequest request,
        ClaimsPrincipal user,
        HttpRequest httpRequest,
        IPublishEndpoint bus,
        CancellationToken ct)
    {
        var tenantId = user.GetTenantId(httpRequest);
        var initiatedBy = user.GetEmployeeIdString(httpRequest);

        await bus.Publish(new InitiateDisbursementCommand
        {
            TenantId = tenantId,
            RunId = request.RunId,
            PeriodName = request.PeriodName,
            BankProvider = request.BankProvider,
            InitiatedBy = initiatedBy
        }, ct);

        return Results.Accepted();
    }

    private static async Task<IResult> GetBatch(
        Guid id, PayrollDbContext db, CancellationToken ct)
    {
        var batch = await db.Set<DisbursementBatch>()
            .AsNoTracking()
            .Include(b => b.Entries)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        return batch is null ? Results.NotFound() : Results.Ok(MapToDto(batch));
    }

    private static async Task<IResult> ListBatches(
        PayrollDbContext db,
        [FromQuery] string? status,
        [FromQuery] string? periodName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = db.Set<DisbursementBatch>().AsNoTracking();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<DisbursementBatchStatus>(status, out var s))
            query = query.Where(b => b.Status == s);

        if (!string.IsNullOrEmpty(periodName))
            query = query.Where(b => b.PeriodName == periodName);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(b => b.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Results.Ok(new { Items = items.Select(MapToDto), Total = total, Page = page });
    }

    private static async Task<IResult> RetryBatch(
        Guid id,
        IPublishEndpoint bus,
        CancellationToken ct)
    {
        await bus.Publish(new RetryDisbursementCommand { BatchId = id, TenantId = Guid.Empty }, ct);
        return Results.Accepted();
    }

    private static DisbursementBatchDto MapToDto(DisbursementBatch b) =>
        new(b.Id, b.RunId, b.PeriodName, b.BankProvider.ToString(),
            b.Status.ToString(), b.TotalAmount, b.TotalEntries,
            b.SuccessCount, b.FailedCount, b.RetryCount, b.CreatedAtUtc);
}
