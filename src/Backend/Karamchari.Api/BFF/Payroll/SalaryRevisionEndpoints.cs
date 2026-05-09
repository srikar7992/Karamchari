using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Payroll.Contracts;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.SalaryRevisions;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Payroll;

public static class SalaryRevisionEndpoints
{
    public static WebApplication MapSalaryRevisionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/payroll/revisions").RequireAuthorization();

        group.MapPost("/", CreateRevision).WithName("Revision.Create");
        group.MapGet("/{id:guid}", GetRevision).WithName("Revision.Get");
        group.MapGet("/", ListRevisions).WithName("Revision.List");
        group.MapPost("/{id:guid}/approve", ApproveRevision).WithName("Revision.Approve");

        return app;
    }

    private static async Task<IResult> CreateRevision(
        [FromBody] CreateSalaryRevisionRequest request,
        ClaimsPrincipal user,
        HttpRequest httpRequest,
        PayrollDbContext db,
        CancellationToken ct)
    {
        var tenantIdStr = user.GetTenantId(httpRequest) ?? "00000000-0000-0000-0000-000000000000";
        var submittedBy = user.GetEmployeeIdString(httpRequest) ?? "system";
        var tenantId = Guid.Parse(tenantIdStr);

        var revision = SalaryRevision.Create(
            tenantId, request.EmployeeId, request.EmployeeName,
            RevisionType.AnnualIncrement,
            request.PreviousCTC, request.NewCTC, request.EffectiveFrom,
            request.RequiresArrears, submittedBy, request.Reason);

        db.Set<SalaryRevision>().Add(revision);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/payroll/revisions/{revision.Id}", MapToDto(revision));
    }

    private static async Task<IResult> GetRevision(
        Guid id, PayrollDbContext db, CancellationToken ct)
    {
        var revision = await db.Set<SalaryRevision>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        return revision is null ? Results.NotFound() : Results.Ok(MapToDto(revision));
    }

    private static async Task<IResult> ListRevisions(
        PayrollDbContext db,
        [FromQuery] string? status,
        [FromQuery] Guid? employeeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = db.Set<SalaryRevision>().AsNoTracking();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<RevisionStatus>(status, out var s))
            query = query.Where(r => r.Status == s);

        if (employeeId.HasValue)
            query = query.Where(r => r.EmployeeId == employeeId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Results.Ok(new { Items = items.Select(MapToDto), Total = total, Page = page });
    }

    private static async Task<IResult> ApproveRevision(
        Guid id,
        ClaimsPrincipal user,
        HttpRequest httpRequest,
        PayrollDbContext db,
        IPublishEndpoint bus,
        CancellationToken ct)
    {
        var revision = await db.Set<SalaryRevision>().FirstOrDefaultAsync(r => r.Id == id, ct);
        if (revision is null) return Results.NotFound();

        var approvedBy = user.GetEmployeeIdString(httpRequest) ?? "system";
        revision.Approve(approvedBy);

        await db.SaveChangesAsync(ct);

        await bus.Publish(new SalaryRevisionApprovedIntegrationEvent
        {
            RevisionId = revision.Id,
            TenantId = revision.TenantId,
            EmployeeId = revision.EmployeeId,
            PreviousCTC = revision.PreviousCTC,
            NewCTC = revision.NewCTC,
            EffectiveFrom = revision.EffectiveFrom,
            RequiresArrears = revision.RequiresArrears,
            OccurredOnUtc = DateTimeOffset.UtcNow
        }, ct);

        return Results.Ok(MapToDto(revision));
    }

    private static SalaryRevisionDto MapToDto(SalaryRevision r) =>
        new(r.Id, r.EmployeeId, r.EmployeeName, r.PreviousCTC, r.NewCTC,
            r.EffectiveFrom, r.Status.ToString(), r.RequiresArrears, r.CreatedAtUtc);
}

public record SalaryRevisionDto(
    Guid Id, Guid EmployeeId, string EmployeeName, decimal PreviousCTC,
    decimal NewCTC, DateOnly EffectiveFrom, string Status, bool RequiresArrears,
    DateTimeOffset CreatedAt);

public record CreateSalaryRevisionRequest(
    Guid EmployeeId, string EmployeeName, decimal PreviousCTC,
    decimal NewCTC, DateOnly EffectiveFrom, string Reason, bool RequiresArrears);
