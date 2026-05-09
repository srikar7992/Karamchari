using System.Security.Claims;
using Karamchari.Api.BFF;
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

        group.MapPost("/", RunSimulation).WithName("Simulation.Run");
        group.MapGet("/{id:guid}", GetSimulation).WithName("Simulation.Get");
        group.MapDelete("/{id:guid}", DiscardSimulation).WithName("Simulation.Discard");

        return app;
    }

    private static async Task<IResult> RunSimulation(
        [FromBody] RunSimulationRequest request,
        ClaimsPrincipal user,
        HttpRequest httpRequest,
        PayrollSimulationEngine engine,
        PayrollDbContext db,
        CancellationToken ct)
    {
        var tenantId = user.GetTenantId(httpRequest);
        var requestedBy = user.GetEmployeeIdString(httpRequest);

        if (!Enum.TryParse<SimulationType>(request.SimulationType, out var simType))
            return Results.BadRequest($"Invalid simulation type: {request.SimulationType}");

        var parameters = new SimulationParameters(
            simType,
            request.EmployeeIds,
            request.GlobalCTCAdjustmentPercent,
            request.PerEmployeeCTC,
            request.ForPeriodName);

        var simulation = await engine.RunAsync(tenantId, parameters, requestedBy, ct);

        db.Set<PayrollSimulation>().Add(simulation);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/payroll/simulations/{simulation.Id}", MapToDto(simulation));
    }

    private static async Task<IResult> GetSimulation(
        Guid id, PayrollDbContext db, CancellationToken ct)
    {
        var simulation = await db.Set<PayrollSimulation>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        return simulation is null ? Results.NotFound() : Results.Ok(MapToDto(simulation));
    }

    private static async Task<IResult> DiscardSimulation(
        Guid id, PayrollDbContext db, CancellationToken ct)
    {
        var simulation = await db.Set<PayrollSimulation>()
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (simulation is null) return Results.NotFound();

        simulation.Discard();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static SimulationResultDto MapToDto(PayrollSimulation s) =>
        new(s.Id, s.Type.ToString(), s.Status.ToString(),
            s.TotalProjectedGross, s.TotalProjectedNet, s.TotalProjectedDelta,
            s.Results.Select(r => new SimulationEmployeeResultDto(
                r.EmployeeId, r.EmployeeName,
                r.CurrentGross, r.ProjectedGross,
                r.CurrentNet, r.ProjectedNet,
                r.NetDelta, r.ComponentBreakdown)).ToList(),
            s.CreatedAtUtc);
}
