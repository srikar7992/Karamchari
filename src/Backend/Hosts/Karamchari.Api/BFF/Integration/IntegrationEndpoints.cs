// -----------------------------------------------------------------------
// <copyright file="IntegrationEndpoints.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Core.Multitenancy;
using Karamchari.Integration.Domain;
using Karamchari.Integration.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Integration;

/// <summary>
/// Webhook subscription management — register outbound event delivery endpoints.
/// Payload is HMAC-SHA256 signed per delivery. Exponential backoff on failure (max 5 attempts).
/// </summary>
public static class IntegrationEndpoints
{
    public static WebApplication MapIntegrationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/integration").RequireAuthorization();

        group.MapGet("/webhooks", ListSubscriptions).WithName("Integration.Webhooks.List");
        group.MapPost("/webhooks", CreateSubscription).WithName("Integration.Webhooks.Create");
        group.MapGet("/webhooks/{id:guid}", GetSubscription).WithName("Integration.Webhooks.Get");
        group.MapPatch("/webhooks/{id:guid}/activate", ActivateSubscription).WithName("Integration.Webhooks.Activate");
        group.MapPatch("/webhooks/{id:guid}/deactivate", DeactivateSubscription).WithName("Integration.Webhooks.Deactivate");
        group.MapGet("/webhooks/{id:guid}/deliveries", ListDeliveries).WithName("Integration.Webhooks.Deliveries");
        group.MapGet("/webhooks/event-types", GetSupportedEventTypes).WithName("Integration.Webhooks.EventTypes");

        return app;
    }

    private static async Task<IResult> ListSubscriptions(
        ClaimsPrincipal user, IntegrationDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var subs = await db.WebhookSubscriptions
            .Where(s => s.TenantId == tenantId)
            .Select(s => new
            {
                s.Id,
                s.Description,
                s.TargetUrl,
                s.IsActive,
                s.EventTypeFilter,
                s.CreatedAt,
                s.LastTriggeredAt,
            })
            .ToListAsync(ct);

        return Results.Ok(subs);
    }

    private static async Task<IResult> CreateSubscription(
        [FromBody] CreateWebhookRequest req,
        ClaimsPrincipal user,
        IntegrationDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        try
        {
            var sub = WebhookSubscription.Create(tenantId, req.TargetUrl, req.Secret, req.Description, req.EventTypes);
            db.WebhookSubscriptions.Add(sub);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/integration/webhooks/{sub.Id.Value}", new { sub.Id, sub.IsActive });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetSubscription(
        Guid id, ClaimsPrincipal user, IntegrationDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var sub = await db.WebhookSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == new WebhookSubscriptionId(id), ct);
        return sub is null ? Results.NotFound() : Results.Ok(sub);
    }

    private static async Task<IResult> ActivateSubscription(
        Guid id, ClaimsPrincipal user, IntegrationDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var sub = await db.WebhookSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == new WebhookSubscriptionId(id), ct);
        if (sub is null) return Results.NotFound();

        sub.Activate();
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { sub.IsActive });
    }

    private static async Task<IResult> DeactivateSubscription(
        Guid id, ClaimsPrincipal user, IntegrationDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var sub = await db.WebhookSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == new WebhookSubscriptionId(id), ct);
        if (sub is null) return Results.NotFound();

        sub.Deactivate();
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { sub.IsActive });
    }

    private static async Task<IResult> ListDeliveries(
        Guid id,
        [FromQuery] string? status,
        ClaimsPrincipal user,
        IntegrationDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var sub = await db.WebhookSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == new WebhookSubscriptionId(id), ct);
        if (sub is null) return Results.NotFound();

        var deliveries = sub.Deliveries.AsQueryable();
        if (Enum.TryParse<WebhookDeliveryStatus>(status, ignoreCase: true, out var parsed))
            deliveries = deliveries.Where(d => d.Status == parsed);

        var result = deliveries
            .OrderByDescending(d => d.CreatedAt)
            .Take(100)
            .Select(d => new
            {
                d.Id,
                d.EventType,
                d.Status,
                d.AttemptCount,
                d.ResponseStatusCode,
                d.ErrorMessage,
                d.CreatedAt,
                d.LastAttemptAt,
                d.NextRetryAt,
            });

        return Results.Ok(result);
    }

    private static IResult GetSupportedEventTypes()
    {
        // Canonical outbound event catalogue — what tenants can subscribe to.
        var events = new[]
        {
            new { EventType = "EmployeeCreated", Description = "Fired when a new employee record is created." },
            new { EventType = "EmployeeTerminated", Description = "Fired when an employee is terminated." },
            new { EventType = "LeaveApproved", Description = "Fired when a leave request is approved." },
            new { EventType = "PayrollCompleted", Description = "Fired when a payroll run is completed." },
            new { EventType = "TicketCreated", Description = "Fired when a new helpdesk ticket is opened." },
            new { EventType = "TicketEscalated", Description = "Fired when a ticket breaches SLA." },
            new { EventType = "AssetAssigned", Description = "Fired when an asset is assigned to an employee." },
            new { EventType = "CertificationExpired", Description = "Fired when an employee certification expires." },
            new { EventType = "AttendanceAnomalyDetected", Description = "Fired when attendance anomaly is flagged." },
        };
        return Results.Ok(events);
    }

    private sealed record CreateWebhookRequest(
        string TargetUrl,
        string Secret,
        string Description,
        string[] EventTypes);
}
