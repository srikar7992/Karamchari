using Karamchari.Api.BFF.Common;
using Karamchari.Api.Validation;
using Karamchari.Core.Multitenancy;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain;
using Karamchari.Payroll.Domain.SalaryStructures;
using Karamchari.Payroll.Domain.Statutory;
using Karamchari.Payroll.Services;
using Karamchari.Payroll.Services.Declarations;
using Karamchari.Payroll.Services.Statutory;
using Karamchari.Payroll.Services.Statutory.Rules;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Payroll;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class PayrollEndpoints
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WebApplication MapPayrollEndpoints(this WebApplication app)
    {
        var payroll = app.MapGroup("/api/payroll").RequireAuthorization();

        payroll.MapPost("/runs", StartPayrollRun).WithValidation<StartPayrollRunRequest>();
        payroll.MapGet("/runs", GetPayrollRuns);
        payroll.MapGet("/runs/{id}/summary", GetPayrollRunSummary);
        payroll.MapGet("/runs/{id}/details", GetPayrollRunDetails);
        payroll.MapPut("/runs/{id}/lock", LockPayrollRun);

        payroll.MapPost("/salary-components", CreateSalaryComponent);
        payroll.MapGet("/salary-components", GetSalaryComponents);
        payroll.MapPost("/salary-templates", CreateSalaryTemplate);
        payroll.MapPost("/salary-templates/{id}/calculate", CalculateCTCBreakdown);
        payroll.MapPost("/salary-templates/{id}/calculate-statutory", CalculateStatutory);

        return app;
    }

    private static async Task<IResult> StartPayrollRun(
        StartPayrollRunRequest request,
        IPublishEndpoint publishEndpoint,
        ITenantProvider tenantProvider)
    {
        var runId = NewId.NextGuid();
        var tenant = tenantProvider.GetTenant();
        await publishEndpoint.Publish(new Karamchari.Core.Contracts.IntegrationEvents.StartPayrollRunCommand(runId, tenant.TenantId, request.PeriodName));
        return Results.Accepted($"/api/payroll/runs/{runId}", new { RunId = runId });
    }

    private static async Task<IResult> GetPayrollRuns(
        PayrollDbContext dbContext, ITenantProvider tenantProvider)
    {
        var tenantId = tenantProvider.GetTenant().TenantId;
        var runs = await dbContext.PayrollRunStates
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.StartedAt)
            .ToListAsync();
        return Results.Ok(runs);
    }

    private static async Task<IResult> GetPayrollRunSummary(
        Guid id, PayrollDbContext dbContext, ITenantProvider tenantProvider)
    {
        var tenantId = tenantProvider.GetTenant().TenantId;
        var run = await dbContext.PayrollRunStates
            .FirstOrDefaultAsync(r => r.CorrelationId == id && r.TenantId == tenantId);
        if (run == null) return Results.NotFound();

        var ledgerTax = await dbContext.PayrollLedger
            .Where(e => e.RunId == id)
            .SumAsync(e => e.TdsDeducted);

        return Results.Ok(new
        {
            run.CorrelationId,
            run.PeriodName,
            run.CurrentState,
            run.TotalEmployeesToProcess,
            run.ProcessedEmployees,
            run.TotalGross,
            run.TotalNet,
            TotalTax = ledgerTax,
            run.StartedAt,
            run.LockedAt,
            run.LockedBy
        });
    }

    private static async Task<IResult> GetPayrollRunDetails(
        Guid id, PayrollDbContext dbContext, ITenantProvider tenantProvider)
    {
        var tenantId = tenantProvider.GetTenant().TenantId;
        var run = await dbContext.PayrollRunStates
            .FirstOrDefaultAsync(r => r.CorrelationId == id && r.TenantId == tenantId);
        if (run == null) return Results.NotFound();

        var currentEntries = await dbContext.PayrollLedger
            .Where(e => e.RunId == id)
            .ToListAsync();

        var employeeIds = currentEntries.Select(e => e.EmployeeId).ToList();

        var previousEntries = await dbContext.PayrollLedger
            .Where(e => employeeIds.Contains(e.EmployeeId) && e.RunId != id)
            .GroupBy(e => e.EmployeeId)
            .Select(g => g.OrderByDescending(e => e.Year).ThenByDescending(e => e.Month).First())
            .ToListAsync();

        var previousMap = previousEntries.ToDictionary(e => e.EmployeeId, e => e);

        var details = currentEntries.Select(e =>
        {
            var prev = previousMap.GetValueOrDefault(e.EmployeeId);
            decimal variance = 0;
            if (prev != null && prev.NetPay != 0)
            {
                variance = (e.NetPay - prev.NetPay) / prev.NetPay;
            }

            return new
            {
                e.EmployeeId,
                e.MonthlyGross,
                e.NetPay,
                TDS = e.TdsDeducted,
                VariancePercentage = variance,
                IsAnomaly = Math.Abs(variance) > 0.10m
            };
        });

        return Results.Ok(details);
    }

    private static async Task<IResult> LockPayrollRun(Guid id, LockPayrollRunRequest request, IPublishEndpoint publishEndpoint)
    {
        await publishEndpoint.Publish(new Karamchari.Core.Contracts.IntegrationEvents.LockPayrollRunCommand(id, request.ApprovedBy));
        return Results.Accepted();
    }

    private static async Task<IResult> CreateSalaryComponent(CreateSalaryComponentRequest request, PayrollDbContext dbContext)
    {
        var component = SalaryComponent.Create(request.Name, request.Type, request.Rounding);
        dbContext.SalaryComponents.Add(component);
        await dbContext.SaveChangesAsync();
        return Results.Created($"/api/payroll/salary-components/{component.Id}", component);
    }

    private static async Task<IResult> GetSalaryComponents(PayrollDbContext dbContext)
    {
        var components = await dbContext.SalaryComponents.ToListAsync();
        return Results.Ok(components);
    }

    private static async Task<IResult> CreateSalaryTemplate(CreateSalaryTemplateRequest request, PayrollDbContext dbContext)
    {
        var template = SalaryTemplate.Create(request.Name);
        foreach (var comp in request.Components)
        {
            template.AddComponent(comp);
        }
        dbContext.SalaryTemplates.Add(template);
        await dbContext.SaveChangesAsync();
        return Results.Created($"/api/payroll/salary-templates/{template.Id}", template);
    }

    private static async Task<IResult> CalculateCTCBreakdown(
        Guid id,
        CalculateCTCBreakdownRequest request,
        PayrollDbContext dbContext)
    {
        var template = await dbContext.SalaryTemplates.FindAsync(id);
        if (template == null) return Results.NotFound("Template not found");

        var masterComponents = await dbContext.SalaryComponents.ToListAsync();
        var plan = CTCTemplateCompiler.Compile(template, masterComponents);
        var result = CTCBreakdownService.Calculate(request.AnnualCTC, plan, request.Overrides);

        return Results.Ok(result);
    }

    private static async Task<IResult> CalculateStatutory(
        Guid id,
        CalculateStatutoryRequest request,
        PayrollDbContext dbContext,
        IProfessionalTaxProvider ptProvider,
        IIncomeProjectionService projectionService,
        IExemptionCalculator exemptionCalculator,
        ITaxSlabProvider taxSlabProvider,
        IITDeclarationRepository declarationRepository)
    {
        var template = await dbContext.SalaryTemplates.FindAsync(id);
        if (template == null) return Results.NotFound("Template not found");

        var masterComponents = await dbContext.SalaryComponents.ToListAsync();
        var plan = CTCTemplateCompiler.Compile(template, masterComponents);
        var breakdown = CTCBreakdownService.Calculate(request.AnnualCTC, plan, request.Overrides);

        var profile = request.EmployeeId.HasValue
            ? await dbContext.PayrollProfiles.FirstOrDefaultAsync(p => p.EmployeeId == request.EmployeeId.Value)
            : PayrollProfile.CreateDraft(Guid.Empty);

        var ruleSet = new FY20262027RuleSet(
            request.EpfBaseComponentIds,
            ptProvider,
            projectionService,
            exemptionCalculator,
            taxSlabProvider,
            declarationRepository);

        var statutoryContext = new StatutoryContext(breakdown, profile!, ruleSet.Year, DateTime.UtcNow.Month);
        var result = await StatutoryPipelineEngine.ExecuteAsync(statutoryContext, ruleSet);

        return Results.Ok(new
        {
            Breakdown = breakdown,
            Statutory = result
        });
    }
}
