using Karamchari.Api.BFF.Common;
using Karamchari.PSA.Domain;
using Karamchari.PSA.Persistence;
using Karamchari.PSA.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.PSA;

public static class PSAEndpoints
{
    public static WebApplication MapPSAEndpoints(this WebApplication app)
    {
        var psa = app.MapGroup("/api/psa").RequireAuthorization();

        psa.MapPost("/clients", CreateClient);
        psa.MapGet("/clients", GetClients);
        psa.MapPost("/projects", CreateProject);
        psa.MapGet("/projects", GetProjectsByClient);
        psa.MapPost("/projects/{projectId}/resources", AssignResource);
        psa.MapGet("/employees/{employeeId}/projects", GetEmployeeProjects);
        psa.MapPost("/invoices/generate", GenerateInvoice);
        psa.MapGet("/invoices/{id}/download", DownloadInvoice);

        var analytics = app.MapGroup("/api/analytics").RequireAuthorization();
        analytics.MapGet("/projects", GetProjectProfitability);
        analytics.MapGet("/projects/{projectId}/trend", GetProjectTrend);
        analytics.MapPost("/simulate", SimulateProfitability);
        analytics.MapGet("/pricing/{projectId}", GetPricingRecommendation);
        analytics.MapGet("/anomalies", GetAnomalies);
        analytics.MapGet("/cashflow/aging", GetAgingReport);
        analytics.MapGet("/cashflow/forecast", GetCashFlowForecast);
        analytics.MapGet("/clients", GetClientProfitability);

        return app;
    }

    private static async Task<IResult> CreateClient([FromBody] CreateClientRequest req, PSADbContext db)
    {
        var client = Client.Create(req.Name, req.Gstin, req.BillingAddress, req.Currency);
        db.Clients.Add(client);
        await db.SaveChangesAsync();
        return Results.Created($"/api/psa/clients/{client.Id}", new { client.Id });
    }

    private static async Task<IResult> GetClients(PSADbContext db)
    {
        var clients = await db.Clients
            .Where(c => c.IsActive)
            .Select(c => new { c.Id, c.Name, c.Gstin, c.Currency })
            .ToListAsync();
        return Results.Ok(clients);
    }

    private static async Task<IResult> CreateProject([FromBody] CreateProjectRequest req, PSADbContext db)
    {
        var project = ClientProject.Create(
            req.ClientId, req.Name,
            Enum.Parse<BillingType>(req.BillingType),
            DateOnly.Parse(req.StartDate),
            req.EndDate != null ? DateOnly.Parse(req.EndDate) : null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return Results.Created($"/api/psa/projects/{project.Id}", new { project.Id });
    }

    private static async Task<IResult> GetProjectsByClient(Guid clientId, PSADbContext db)
    {
        var projects = await db.Projects
            .Where(p => p.ClientId == clientId && p.IsActive)
            .Select(p => new { p.Id, p.Name, p.BillingType, p.StartDate, p.EndDate })
            .ToListAsync();
        return Results.Ok(projects);
    }

    private static async Task<IResult> AssignResource(
        Guid projectId,
        [FromBody] AssignResourceRequest req,
        PSADbContext db)
    {
        var resource = ProjectResource.Assign(
            req.EmployeeId, projectId,
            req.BillableRate, req.Currency,
            DateOnly.Parse(req.EffectiveFrom),
            req.IsBillable);
        db.ProjectResources.Add(resource);
        await db.SaveChangesAsync();
        return Results.Created($"/api/psa/resources/{resource.Id}", new { resource.Id });
    }

    private static async Task<IResult> GetEmployeeProjects(
        Guid employeeId,
        PSADbContext db,
        ProjectResourceRepository repo)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var projects = await repo.GetAssignedProjectsAsync(employeeId, today);
        return Results.Ok(projects.Select(p => new { p.Id, p.Name, p.BillingType }));
    }

    private static async Task<IResult> GenerateInvoice(
        [FromBody] GenerateInvoiceRequest req,
        InvoiceGeneratorService svc,
        PSADbContext db)
    {
        var cutoff = DateOnly.Parse(req.CutoffDate);
        var invoice = await svc.GenerateAsync(
            req.ClientId, cutoff, req.InvoiceNumberSeed, req.IsInterState);

        if (invoice == null) return Results.NoContent();

        var client = await db.Clients.FindAsync(req.ClientId);
        if (client != null)
        {
            var pdfDoc = new InvoicePdfDocument(invoice, client);
            var pdfBytes = pdfDoc.ToPdfBytes();
            var pdfPath = Path.Combine("artifacts", "invoices", $"{invoice.InvoiceNumber}.pdf");
            Directory.CreateDirectory(Path.GetDirectoryName(pdfPath)!);
            await File.WriteAllBytesAsync(pdfPath, pdfBytes);
            invoice.AttachPdf(pdfPath);
            await db.SaveChangesAsync();
        }

        return Results.Ok(new
        {
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.TotalAmount,
            invoice.Currency,
            LineCount = invoice.Lines.Count
        });
    }

    private static async Task<IResult> DownloadInvoice(Guid id, PSADbContext db)
    {
        var invoice = await db.Invoices.FindAsync(id);
        if (invoice == null) return Results.NotFound();
        if (string.IsNullOrEmpty(invoice.PdfPath) || !File.Exists(invoice.PdfPath))
            return Results.Problem("Invoice PDF not yet generated.");

        var pdfBytes = await File.ReadAllBytesAsync(invoice.PdfPath);
        return Results.File(pdfBytes, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
    }

    // --- Analytics ---

    private static async Task<IResult> GetProjectProfitability(int? year, PSADbContext db)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var metrics = await db.MonthlyMetrics
            .Where(m => m.Year == targetYear)
            .GroupBy(m => m.ProjectId)
            .Select(g => new
            {
                ProjectId = g.Key,
                Revenue = g.Sum(m => m.TotalRevenue),
                Cost = g.Sum(m => m.TotalCost),
                BillableHours = g.Sum(m => m.BillableHours),
                TotalHours = g.Sum(m => m.TotalHours)
            })
            .ToListAsync();

        var result = metrics.Select(m =>
        {
            var profit = m.Revenue - m.Cost;
            return new
            {
                m.ProjectId,
                m.Revenue,
                m.Cost,
                Profit = profit,
                Margin = m.Revenue == 0 ? 0 : Math.Round(profit / m.Revenue * 100, 2),
                m.BillableHours,
                Utilization = m.TotalHours == 0 ? 0 : Math.Round(m.BillableHours / m.TotalHours * 100, 2)
            };
        });
        return Results.Ok(result);
    }

    private static async Task<IResult> GetProjectTrend(Guid projectId, PSADbContext db)
    {
        var trend = await db.MonthlyMetrics
            .Where(m => m.ProjectId == projectId)
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .Select(m => new
            {
                m.Year,
                m.Month,
                m.TotalRevenue,
                m.TotalCost,
                Profit = m.TotalRevenue - m.TotalCost,
                m.BillableHours
            })
            .ToListAsync();
        return Results.Ok(trend);
    }

    private static IResult SimulateProfitability([FromBody] SimulateRequest req)
    {
        var result = SimulationService.Simulate(req.CurrentRevenue, req.CurrentCost, req.RateChangePercent, req.CostChangePercent);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetPricingRecommendation(
        Guid projectId,
        PSADbContext db,
        PricingEngine engine,
        EmployeeCostService costSvc)
    {
        var avgRate = await db.ProjectResources
            .Where(r => r.ProjectId == projectId && r.IsBillable)
            .AverageAsync(r => (decimal?)r.BillableRate) ?? 0;

        var employeeIds = await db.ProjectResources
            .Where(r => r.ProjectId == projectId)
            .Select(r => r.EmployeeId)
            .Distinct()
            .ToListAsync();

        var now = DateTime.UtcNow;
        decimal totalCost = 0;
        int costCount = 0;
        foreach (var empId in employeeIds)
        {
            var cph = await costSvc.GetCostPerHourAsync(empId, now.Year, now.Month);
            if (cph.HasValue) { totalCost += cph.Value; costCount++; }
        }
        decimal avgCost = costCount > 0 ? totalCost / costCount : 0;

        var recommendation = PricingEngine.Recommend(avgCost, avgRate);
        return Results.Ok(recommendation);
    }

    private static async Task<IResult> GetAnomalies(PSADbContext db)
    {
        var now = DateTime.UtcNow;
        var currentMonth = now.Month;
        var currentYear = now.Year;
        var prevMonth = currentMonth == 1 ? 12 : currentMonth - 1;
        var prevYear = currentMonth == 1 ? currentYear - 1 : currentYear;

        var current = await db.MonthlyMetrics
            .Where(m => m.Year == currentYear && m.Month == currentMonth)
            .ToListAsync();

        var previous = await db.MonthlyMetrics
            .Where(m => m.Year == prevYear && m.Month == prevMonth)
            .ToListAsync();

        var comparisons = current.Select(c =>
        {
            var p = previous.FirstOrDefault(x => x.ProjectId == c.ProjectId);
            return new MonthComparison(
                c.ProjectId,
                c.TotalRevenue, c.TotalCost, c.TotalRevenue - c.TotalCost,
                p?.TotalRevenue ?? 0, p?.TotalCost ?? 0,
                p != null ? p.TotalRevenue - p.TotalCost : 0);
        });

        var anomalies = AnomalyDetectionService.Detect(comparisons);
        return Results.Ok(anomalies);
    }

    private static async Task<IResult> GetAgingReport(CashFlowService svc)
    {
        var aging = await svc.GetAgingAsync();
        return Results.Ok(aging);
    }

    private static async Task<IResult> GetCashFlowForecast(CashFlowService svc)
    {
        var forecast = await svc.ForecastAsync();
        return Results.Ok(forecast);
    }

    private static async Task<IResult> GetClientProfitability(int? year, PSADbContext db)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var projectClients = await db.Projects
            .Select(p => new { p.Id, p.ClientId, p.Name })
            .ToListAsync();

        var metrics = await db.MonthlyMetrics
            .Where(m => m.Year == targetYear)
            .ToListAsync();

        var clientMetrics = metrics
            .Join(projectClients, m => m.ProjectId, p => p.Id, (m, p) => new { p.ClientId, m.TotalRevenue, m.TotalCost })
            .GroupBy(x => x.ClientId)
            .Select(g => new
            {
                ClientId = g.Key,
                Revenue = g.Sum(x => x.TotalRevenue),
                Cost = g.Sum(x => x.TotalCost),
                Profit = g.Sum(x => x.TotalRevenue - x.TotalCost)
            })
            .ToList();
        return Results.Ok(clientMetrics);
    }
}
