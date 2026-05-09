using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Api.BFF.Common;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain;
using Karamchari.Payroll.Domain.SalaryStructures;
using Karamchari.Payroll.Domain.Statutory;
using Karamchari.Payroll.Services;
using Karamchari.Payroll.Services.Declarations;
using Karamchari.Payroll.Services.Payslip;
using Karamchari.Payroll.Services.Statutory;
using Karamchari.Payroll.Services.Statutory.Rules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.ESS;

public static class ESSEndpoints
{
    public static WebApplication MapESSEndpoints(this WebApplication app)
    {
        var ess = app.MapGroup("/api/ess").RequireAuthorization();

        ess.MapGet("/payslips", GetPayslips);
        ess.MapGet("/payslips/{year}/{month}/download", DownloadPayslip);
        ess.MapGet("/payslips/ytd", GetYtdSummary);
        ess.MapPost("/tax-simulator/dry-run", SimulateTax).RequireRateLimiting("ess");
        ess.MapPost("/declarations/analyze", AnalyzeDeclaration).RequireRateLimiting("ai");

        return app;
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

        if (request.Section80C > 150000) return Results.BadRequest("Section 80C declaration cannot exceed ₹1,50,000.");
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
}
