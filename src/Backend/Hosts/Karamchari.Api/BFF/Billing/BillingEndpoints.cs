using Karamchari.Api.BFF.Common;
using Karamchari.Billing.Persistence;
using Karamchari.Billing.Services;
using Karamchari.Core.Multitenancy;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Billing;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class BillingEndpoints
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WebApplication MapBillingEndpoints(this WebApplication app)
    {
        var billing = app.MapGroup("/api/billing").RequireAuthorization();

        billing.MapPost("/contracts", CreateBillingContract);
        billing.MapPost("/contracts/{id}/rates", AddRateCard);
        billing.MapPost("/roles", MapEmployeeRole);
        billing.MapPost("/invoices/generate", GenerateBillingInvoice);
        billing.MapPost("/invoices/{id}/finalize", FinalizeBillingInvoice);
        billing.MapGet("/ar/summary", GetARSummary);
        billing.MapPost("/invoices/{id}/payment", RecordPayment);

        var collections = app.MapGroup("/api/collections").RequireAuthorization();
        collections.MapGet("/", GetCollectionCases);
        collections.MapPut("/{id}/dispute", DisputeCollectionCase);
        collections.MapPost("/{id}/remind", RemindCollectionCase);

        var forecast = app.MapGroup("/api/forecast").RequireAuthorization();
        forecast.MapGet("/summary", GetForecastSummary);

        return app;
    }

    private static async Task<IResult> CreateBillingContract(
        CreateBillingContractRequest request,
        BillingDbContext db,
        ITenantProvider tenantProvider)
    {
        var tenant = tenantProvider.GetTenant();
        var contract = Karamchari.Billing.Domain.Contracts.BillingContract.Create(
            tenant.TenantId, request.ClientId, request.ProjectId, request.BillingType);

        contract.Currency = request.Currency;
        contract.BillingCycle = request.Cycle;
        if (!string.IsNullOrEmpty(request.StartDate))
            contract.StartDate = DateOnly.Parse(request.StartDate);

        db.Contracts.Add(contract);
        await db.SaveChangesAsync();
        return Results.Created($"/api/billing/contracts/{contract.Id}", contract);
    }

    private static async Task<IResult> AddRateCard(
        Guid id,
        AddRateCardRequest request,
        BillingDbContext db)
    {
        var contract = await db.Contracts.Include(c => c.RateCards).FirstOrDefaultAsync(c => c.Id == id);
        if (contract == null) return Results.NotFound();

        contract.AddRate(
            request.RoleId,
            request.Rate,
            request.Unit,
            DateOnly.Parse(request.EffectiveFrom),
            request.EffectiveTo != null ? DateOnly.Parse(request.EffectiveTo) : null);

        await db.SaveChangesAsync();
        return Results.Ok();
    }

    private static async Task<IResult> MapEmployeeRole(
        MapEmployeeRoleRequest request,
        BillingDbContext db,
        ITenantProvider tenantProvider)
    {
        var tenant = tenantProvider.GetTenant();
        var roleMapping = new Karamchari.Billing.Domain.Contracts.EmployeeRole
        {
            TenantId = tenant.TenantId,
            EmployeeId = request.EmployeeId,
            RoleId = request.RoleId,
            ProjectId = request.ProjectId,
            EffectiveFrom = DateOnly.Parse(request.EffectiveFrom),
            EffectiveTo = request.EffectiveTo != null ? DateOnly.Parse(request.EffectiveTo) : null
        };

        db.EmployeeRoles.Add(roleMapping);
        await db.SaveChangesAsync();
        return Results.Ok();
    }

    private static async Task<IResult> GenerateBillingInvoice(
        GenerateInvoiceRunRequest request,
        Karamchari.Billing.Services.InvoiceGeneratorService generator)
    {
        var invoice = await generator.GenerateDraftAsync(
            request.ContractId,
            DateOnly.Parse(request.StartDate),
            DateOnly.Parse(request.EndDate));
        return invoice != null ? Results.Ok(invoice) : Results.NoContent();
    }

    private static async Task<IResult> FinalizeBillingInvoice(
        Guid id,
        FinalizeInvoiceRequest request,
        BillingDbContext db,
        IPublishEndpoint publishEndpoint)
    {
        var invoice = await db.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == id);
        if (invoice == null) return Results.NotFound();

        var snapshot = System.Text.Json.JsonSerializer.Serialize(new
        {
            invoice.InvoiceNumber,
            invoice.PeriodStart,
            invoice.PeriodEnd,
            Lines = invoice.Lines.Select(l => new { l.Description, l.Quantity, l.Rate, l.Amount }),
            invoice.TotalAmount,
            invoice.TaxAmount,
            invoice.GrandTotal
        });

        invoice.Finalize(request.InvoiceNumber, "Admin", snapshot);
        await db.SaveChangesAsync();

        await publishEndpoint.Publish(new Karamchari.Billing.Contracts.InvoiceIssuedIntegrationEvent(
            Guid.NewGuid(),
            invoice.Id,
            invoice.ContractId,
            invoice.TenantId,
            invoice.TotalAmount,
            invoice.TaxAmount,
            DateTimeOffset.UtcNow));

        return Results.Ok(new { invoice.InvoiceNumber, Status = "Finalized" });
    }

    private static async Task<IResult> GetARSummary(
        ARAnalyticsService arService,
        ITenantProvider tenantProvider)
    {
        var tenant = tenantProvider.GetTenant();
        var summary = await arService.GetSummaryAsync(tenant.TenantId);
        return Results.Ok(summary);
    }

    private static async Task<IResult> RecordPayment(
        Guid id,
        RecordPaymentRequest request,
        BillingDbContext db,
        IPublishEndpoint publishEndpoint)
    {
        var invoice = await db.Invoices.Include(i => i.Payments).FirstOrDefaultAsync(i => i.Id == id);
        if (invoice == null) return Results.NotFound();

        invoice.RecordPayment(request.Amount, request.PaidAt);
        await db.SaveChangesAsync();

        await publishEndpoint.Publish(new Karamchari.Billing.Contracts.PaymentReceivedIntegrationEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            invoice.Id,
            invoice.ContractId,
            invoice.TenantId,
            request.Amount,
            DateTimeOffset.UtcNow));

        return Results.Ok();
    }

    private static async Task<IResult> GetCollectionCases(
        [FromQuery] Karamchari.Billing.Domain.Collections.CollectionStatus? status,
        BillingDbContext db,
        ITenantProvider tenantProvider)
    {
        var tenant = tenantProvider.GetTenant();
        var query = db.CollectionCases
            .Where(c => c.TenantId == tenant.TenantId);

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        var cases = await query
            .OrderByDescending(c => c.DaysOutstanding)
            .ToListAsync();
        return Results.Ok(cases);
    }

    private static async Task<IResult> DisputeCollectionCase(
        Guid id,
        BillingDbContext db)
    {
        var @case = await db.CollectionCases.FindAsync(id);
        if (@case == null) return Results.NotFound();
        @case.MarkDisputed();
        await db.SaveChangesAsync();
        return Results.Ok();
    }

    private static async Task<IResult> RemindCollectionCase(
        Guid id,
        BillingDbContext db)
    {
        var @case = await db.CollectionCases.FindAsync(id);
        if (@case == null) return Results.NotFound();
        @case.RecordReminder("manual");
        await db.SaveChangesAsync();
        return Results.Ok(new { LastActionAt = @case.LastActionAt });
    }

    private static async Task<IResult> GetForecastSummary(
        Karamchari.Forecasting.Services.ForecastingEngine engine,
        ITenantProvider tenantProvider)
    {
        var tenant = tenantProvider.GetTenant();
        var summary = await engine.GetSummaryAsync(tenant.TenantId);
        return Results.Ok(summary);
    }
}
