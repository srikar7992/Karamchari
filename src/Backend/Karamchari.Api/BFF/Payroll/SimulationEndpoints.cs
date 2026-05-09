using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Payroll.Contracts;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.Simulation;
using Karamchari.Payroll.Services.Simulation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Payroll;

public static class SimulationEndpoints
{
    public static WebApplication MapSimulationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/payroll/simulations").RequireAuthorization();

        group.MapPost("/run", RunSimulation).WithName("Simulation.Run");
        group.MapGet("/{id:guid}", GetSimulation).WithName("Simulation.Get");
        group.MapGet("/", ListSimulations).WithName("Simulation.List");

        return app;
    }

    private static async Task<IResult> RunSimulation(
        [FromBody] SimulationParameters parameters,
        ClaimsPrincipal user,
        HttpRequest httpRequest,
        PayrollSimulationEngine engine,
        CancellationToken ct)
    {
        var tenantIdStr = user.GetTenantId(httpRequest) ?? "00000000-0000-0000-0000-000000000000";
        var requestedBy = user.GetEmployeeIdString(httpRequest) ?? "system";
        var tenantId = Guid.Parse(tenantIdStr);

        var simulation = await engine.RunAsync(tenantId, parameters, requestedBy, ct);

        return Results.Accepted($"/api/v1/payroll/simulations/{simulation.Id}", MapToDto(simulation));
    }

    private static async Task<IResult> GetSimulation(
        Guid id, PayrollDbContext db, CancellationToken ct)
    {
        var simulation = await db.Set<PayrollSimulation>()
            .AsNoTracking()
            .Include(s => s.Results)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        return simulation is null ? Results.NotFound() : Results.Ok(MapToDto(simulation));
    }

    private static async Task<IResult> ListSimulations(
        PayrollDbContext db,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = db.Set<PayrollSimulation>().AsNoTracking();

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Results.Ok(new { Items = items.Select(MapToDto), Total = total, Page = page });
    }

    private static SimulationDto MapToDto(PayrollSimulation s) =>
        new(s.Id, s.Type.ToString(), s.Status.ToString(),
            s.Results.Count, s.CreatedAtUtc, s.CompletedAtUtc);
}

public record SimulationDto(
    Guid Id, string Type, string Status, int EmployeeCount,
    DateTimeOffset StartedAt, DateTimeOffset? CompletedAt);
