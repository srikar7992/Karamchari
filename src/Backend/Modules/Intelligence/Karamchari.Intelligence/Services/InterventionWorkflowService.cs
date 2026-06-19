// -----------------------------------------------------------------------
// <copyright file="InterventionWorkflowService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Intelligence.Domain.Interventions;
using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Intelligence.Services;

/// <summary>
/// Command-side service for the intervention execution workflow.
/// Handles the lifecycle of <see cref="InterventionInstance"/> records and
/// records <see cref="RecommendationDisposition"/> for recommendations that
/// are accepted, declined, deferred, or expired.
/// </summary>
public sealed class InterventionWorkflowService
{
    private readonly IntelligenceDbContext _db;
    private readonly ILogger<InterventionWorkflowService> _logger;

    public InterventionWorkflowService(IntelligenceDbContext db, ILogger<InterventionWorkflowService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new <see cref="InterventionInstance"/> for the given recommendation and
    /// records an <see cref="RecommendationDispositionType.Accepted"/> disposition.
    /// </summary>
    /// <returns>The new instance ID.</returns>
    public async Task<Guid> AcceptRecommendationAsync(
        string tenantId,
        Guid recommendationId,
        InterventionOwnerType ownerType,
        string? ownerId,
        CancellationToken ct = default)
    {
        var rec = await RequireRecommendationAsync(tenantId, recommendationId, ct);

        if (rec.TemplateId is null)
            throw new InvalidOperationException(
                $"Recommendation {recommendationId} has no associated template and cannot create an InterventionInstance.");

        var alreadyExists = await _db.InterventionInstances
            .AnyAsync(i => i.TenantId == tenantId && i.RecommendationId == recommendationId, ct);

        if (alreadyExists)
            throw new InvalidOperationException(
                $"An InterventionInstance already exists for recommendation {recommendationId}.");

        var instance = InterventionInstance.Create(
            tenantId,
            recommendationId,
            rec.TemplateId.Value,
            rec.EmployeeId,
            ownerType,
            ownerId);

        var disposition = RecommendationDisposition.Record(
            tenantId,
            recommendationId,
            rec.EmployeeId,
            RecommendationDispositionType.Accepted,
            actorId: ownerId);

        _db.InterventionInstances.Add(instance);
        _db.RecommendationDispositions.Add(disposition);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "InterventionInstance {InstanceId} created for recommendation {RecId} (employee {EmpId})",
            instance.Id, recommendationId, rec.EmployeeId);

        return instance.Id;
    }

    /// <summary>
    /// Records a <see cref="RecommendationDispositionType.Declined"/> disposition.
    /// No instance is created.
    /// </summary>
    public async Task DeclineRecommendationAsync(
        string tenantId,
        Guid recommendationId,
        string? actorId,
        string? reason,
        CancellationToken ct = default)
    {
        var rec = await RequireRecommendationAsync(tenantId, recommendationId, ct);

        await EnsureNoDispositionAsync(tenantId, recommendationId, ct);

        _db.RecommendationDispositions.Add(
            RecommendationDisposition.Record(
                tenantId, recommendationId, rec.EmployeeId,
                RecommendationDispositionType.Declined, actorId, reason));

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Recommendation {RecId} declined by {ActorId}: {Reason}",
            recommendationId, actorId, reason);
    }

    /// <summary>
    /// Records a <see cref="RecommendationDispositionType.Deferred"/> disposition.
    /// </summary>
    public async Task DeferRecommendationAsync(
        string tenantId,
        Guid recommendationId,
        string? actorId,
        string? reason,
        CancellationToken ct = default)
    {
        var rec = await RequireRecommendationAsync(tenantId, recommendationId, ct);

        await EnsureNoDispositionAsync(tenantId, recommendationId, ct);

        _db.RecommendationDispositions.Add(
            RecommendationDisposition.Record(
                tenantId, recommendationId, rec.EmployeeId,
                RecommendationDispositionType.Deferred, actorId, reason));

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Moves an instance from Pending/Accepted → InProgress.</summary>
    public async Task StartInterventionAsync(
        string tenantId, Guid instanceId, CancellationToken ct = default)
    {
        var instance = await RequireInstanceAsync(tenantId, instanceId, ct);
        instance.Start();
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Completes an instance and resolves the linked recommendation so that
    /// <c>InterventionTrackerService</c> evaluates effectiveness 30 days later.
    /// </summary>
    public async Task CompleteInterventionAsync(
        string tenantId,
        Guid instanceId,
        string? outcomeNotes,
        string resolvedBy,
        CancellationToken ct = default)
    {
        var instance = await RequireInstanceAsync(tenantId, instanceId, ct);
        instance.Complete(outcomeNotes);

        var rec = await RequireRecommendationAsync(tenantId, instance.RecommendationId, ct);
        rec.Resolve(resolvedBy);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "InterventionInstance {InstanceId} completed; recommendation {RecId} resolved for 30-day evaluation",
            instanceId, instance.RecommendationId);
    }

    /// <summary>Cancels an in-flight intervention.</summary>
    public async Task CancelInterventionAsync(
        string tenantId,
        Guid instanceId,
        string? reason,
        CancellationToken ct = default)
    {
        var instance = await RequireInstanceAsync(tenantId, instanceId, ct);
        instance.Cancel(reason);
        await _db.SaveChangesAsync(ct);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<WorkforceRecommendation> RequireRecommendationAsync(
        string tenantId, Guid recommendationId, CancellationToken ct)
    {
        var rec = await _db.WorkforceRecommendations
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Id == recommendationId, ct);

        if (rec is null)
            throw new KeyNotFoundException($"Recommendation {recommendationId} not found.");

        return rec;
    }

    private async Task<InterventionInstance> RequireInstanceAsync(
        string tenantId, Guid instanceId, CancellationToken ct)
    {
        var instance = await _db.InterventionInstances
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.Id == instanceId, ct);

        if (instance is null)
            throw new KeyNotFoundException($"InterventionInstance {instanceId} not found.");

        return instance;
    }

    private async Task EnsureNoDispositionAsync(
        string tenantId, Guid recommendationId, CancellationToken ct)
    {
        var exists = await _db.RecommendationDispositions
            .AnyAsync(d => d.TenantId == tenantId && d.RecommendationId == recommendationId, ct);

        if (exists)
            throw new InvalidOperationException(
                $"A disposition already exists for recommendation {recommendationId}.");
    }
}
