// -----------------------------------------------------------------------
// <copyright file="BenefitsEndpoints.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Api.BFF.Benefits;

using System.Security.Claims;
using Karamchari.Benefits.Domain;
using Karamchari.Benefits.Services;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// BFF endpoints for the Benefits Administration module.
///
/// /api/ess/benefits/...    Employee self-service — employee sees only their own enrollment.
/// /api/admin/benefits/...  HR administrator — plan catalog and enrollment management.
///
/// Error handling: InvalidOperationException propagates to the global exception handler
/// which returns 400 Bad Request with the message; NotFound() is returned explicitly
/// when an entity is not found for the authenticated user.
/// </summary>
public static class BenefitsEndpoints
{
    public static WebApplication MapBenefitsEndpoints(this WebApplication app)
    {
        // ── Employee self-service ──────────────────────────────────────────────
        var ess = app.MapGroup("/api/ess/benefits").RequireAuthorization();

        ess.MapGet("/enrollment", GetMyEnrollment);
        ess.MapGet("/plans", GetAvailablePlans);
        ess.MapGet("/plans/{planId:guid}", GetPlanById);

        ess.MapPost("/enrollment/elect", ElectBenefit);
        ess.MapPost("/enrollment/waive", WaiveCategory);
        ess.MapPost("/enrollment/dependents", AddDependent);
        ess.MapDelete("/enrollment/dependents/{dependentId:guid}", RemoveDependent);
        ess.MapPost("/enrollment/submit", SubmitEnrollment);

        ess.MapPost("/life-events", SubmitLifeEvent);
        ess.MapPost("/life-events/{lifeEventId:guid}/elections", ApplyLifeEventElection);

        // ── HR admin ───────────────────────────────────────────────────────────
        var admin = app.MapGroup("/api/admin/benefits").RequireAuthorization();

        admin.MapGet("/plans", AdminGetPlans);
        admin.MapPost("/plans", AdminCreatePlan);
        admin.MapPut("/plans/{planId:guid}/tiers/{tier}", AdminSetTierCost);
        admin.MapPost("/plans/{planId:guid}/publish", AdminPublishPlan);
        admin.MapPost("/plans/{planId:guid}/deactivate", AdminDeactivatePlan);

        admin.MapGet("/enrollments", AdminGetEnrollments);
        admin.MapPost("/enrollments/{enrollmentId:guid}/activate", AdminActivateEnrollment);
        admin.MapPost("/enrollments/{enrollmentId:guid}/life-events/{lifeEventId:guid}/verify",
            AdminVerifyLifeEvent);
        admin.MapPost("/enrollments/{enrollmentId:guid}/life-events/{lifeEventId:guid}/reject",
            AdminRejectLifeEvent);

        return app;
    }

    // ── Employee self-service handlers ─────────────────────────────────────────

    private static async Task<IResult> GetMyEnrollment(
        ClaimsPrincipal user, EnrollmentQueryService svc, CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();
        var dto = await svc.GetCurrentEnrollmentAsync(employeeId.Value, ct);
        return dto is null ? Results.NotFound() : Results.Ok(dto);
    }

    private static async Task<IResult> GetAvailablePlans(
        [FromQuery] string? category,
        BenefitPlanQueryService svc, CancellationToken ct)
    {
        BenefitCategory? cat = Enum.TryParse<BenefitCategory>(category, out var parsed) ? parsed : null;
        return Results.Ok(await svc.GetAvailablePlansAsync(cat, ct));
    }

    private static async Task<IResult> GetPlanById(
        Guid planId, BenefitPlanQueryService svc, CancellationToken ct)
    {
        var dto = await svc.GetPlanByIdAsync(planId, ct);
        return dto is null ? Results.NotFound() : Results.Ok(dto);
    }

    private static async Task<IResult> ElectBenefit(
        ElectBenefitRequest request, ClaimsPrincipal user,
        EnrollmentCommandService svc, CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();
        if (!Enum.TryParse<CoverageTier>(request.CoverageTier, out var tier))
            return Results.BadRequest("Invalid coverage tier.");
        var id = await svc.ElectBenefitAsync(employeeId.Value, request.PlanId, tier, request.EffectiveDate, ct);
        return Results.Ok(new { ElectionId = id });
    }

    private static async Task<IResult> WaiveCategory(
        WaiveCategoryRequest request, ClaimsPrincipal user,
        EnrollmentCommandService svc, CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();
        if (!Enum.TryParse<BenefitCategory>(request.Category, out var cat))
            return Results.BadRequest("Invalid benefit category.");
        await svc.WaiveCategoryAsync(employeeId.Value, cat, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> AddDependent(
        AddDependentRequest request, ClaimsPrincipal user,
        EnrollmentCommandService svc, CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();
        if (!Enum.TryParse<DependentRelationship>(request.Relationship, out var rel))
            return Results.BadRequest("Invalid dependent relationship.");
        var id = await svc.AddDependentAsync(
            employeeId.Value,
            request.FirstName, request.LastName, request.DateOfBirth,
            rel, request.EnrollMedical, request.EnrollDental, request.EnrollVision, ct);
        return Results.Ok(new { DependentId = id });
    }

    private static async Task<IResult> RemoveDependent(
        Guid dependentId, ClaimsPrincipal user,
        EnrollmentCommandService svc, CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();
        await svc.RemoveDependentAsync(employeeId.Value, dependentId, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> SubmitEnrollment(
        ClaimsPrincipal user, EnrollmentCommandService svc, CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();
        await svc.SubmitEnrollmentAsync(employeeId.Value, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> SubmitLifeEvent(
        SubmitLifeEventRequest request, ClaimsPrincipal user,
        LifeEventCommandService svc, CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();
        if (!Enum.TryParse<LifeEventType>(request.EventType, out var eventType))
            return Results.BadRequest("Invalid life event type.");
        var id = await svc.SubmitLifeEventAsync(
            employeeId.Value, eventType, request.EventDate, request.DocumentStorageReference, ct);
        return Results.Ok(new { LifeEventId = id });
    }

    private static async Task<IResult> ApplyLifeEventElection(
        Guid lifeEventId, LifeEventElectionRequest request, ClaimsPrincipal user,
        LifeEventCommandService svc, CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();
        if (!Enum.TryParse<BenefitCategory>(request.Category, out var cat))
            return Results.BadRequest("Invalid benefit category.");
        if (!Enum.TryParse<CoverageTier>(request.CoverageTier, out var tier))
            return Results.BadRequest("Invalid coverage tier.");
        var id = await svc.ApplyElectionChangeAsync(
            employeeId.Value, lifeEventId, request.PlanId, cat, tier, request.EffectiveDate, ct);
        return Results.Ok(new { ElectionId = id });
    }

    // ── Admin handlers ─────────────────────────────────────────────────────────

    private static async Task<IResult> AdminGetPlans(
        [FromQuery] string? category,
        BenefitPlanQueryService svc, CancellationToken ct)
    {
        BenefitCategory? cat = Enum.TryParse<BenefitCategory>(category, out var parsed) ? parsed : null;
        return Results.Ok(await svc.GetAllPlansAsync(cat, ct));
    }

    private static async Task<IResult> AdminCreatePlan(
        CreatePlanRequest request, BenefitAdminService svc, CancellationToken ct)
    {
        if (!Enum.TryParse<BenefitCategory>(request.Category, out var cat))
            return Results.BadRequest("Invalid benefit category.");
        var id = await svc.CreatePlanAsync(
            request.PlanName, request.Description, cat,
            request.ProviderName, request.ExternalPlanCode,
            request.PlanYearStart, request.PlanYearEnd,
            request.WaitingPeriodDays, ct);
        return Results.Created($"/api/admin/benefits/plans/{id}", new { PlanId = id });
    }

    private static async Task<IResult> AdminSetTierCost(
        Guid planId, string tier, SetTierCostRequest request,
        BenefitAdminService svc, CancellationToken ct)
    {
        if (!Enum.TryParse<CoverageTier>(tier, out var covTier))
            return Results.BadRequest("Invalid coverage tier.");
        await svc.SetTierCostAsync(planId, covTier, request.EmployeePremium, request.EmployerPremium, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> AdminPublishPlan(
        Guid planId, BenefitAdminService svc, CancellationToken ct)
    {
        await svc.PublishPlanAsync(planId, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> AdminDeactivatePlan(
        Guid planId, BenefitAdminService svc, CancellationToken ct)
    {
        await svc.DeactivatePlanAsync(planId, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> AdminGetEnrollments(
        EnrollmentQueryService svc, CancellationToken ct) =>
        Results.Ok(await svc.GetActiveEnrollmentsAsync(ct));

    private static async Task<IResult> AdminActivateEnrollment(
        Guid enrollmentId, BenefitAdminService svc, CancellationToken ct)
    {
        await svc.ActivateEnrollmentAsync(enrollmentId, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> AdminVerifyLifeEvent(
        Guid enrollmentId, Guid lifeEventId, ClaimsPrincipal user,
        BenefitAdminService svc, CancellationToken ct)
    {
        var reviewerId = user.GetEmployeeIdString() ?? "system";
        await svc.VerifyLifeEventAsync(enrollmentId, lifeEventId, reviewerId, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> AdminRejectLifeEvent(
        Guid enrollmentId, Guid lifeEventId, AdminRejectRequest request,
        ClaimsPrincipal user, BenefitAdminService svc, CancellationToken ct)
    {
        var reviewerId = user.GetEmployeeIdString() ?? "system";
        await svc.RejectLifeEventAsync(enrollmentId, lifeEventId, reviewerId, request.Reason, ct);
        return Results.NoContent();
    }

    // ── Request records ────────────────────────────────────────────────────────

    private record ElectBenefitRequest(Guid PlanId, string CoverageTier, DateOnly EffectiveDate);
    private record WaiveCategoryRequest(string Category);
    private record AddDependentRequest(
        string FirstName, string LastName, DateOnly DateOfBirth, string Relationship,
        bool EnrollMedical, bool EnrollDental, bool EnrollVision);
    private record SubmitLifeEventRequest(
        string EventType, DateOnly EventDate, string? DocumentStorageReference);
    private record LifeEventElectionRequest(
        Guid PlanId, string Category, string CoverageTier, DateOnly EffectiveDate);
    private record CreatePlanRequest(
        string PlanName, string? Description, string Category,
        string ProviderName, string? ExternalPlanCode,
        DateOnly PlanYearStart, DateOnly PlanYearEnd, int WaitingPeriodDays = 0);
    private record SetTierCostRequest(decimal EmployeePremium, decimal EmployerPremium);
    private record AdminRejectRequest(string Reason);
}
