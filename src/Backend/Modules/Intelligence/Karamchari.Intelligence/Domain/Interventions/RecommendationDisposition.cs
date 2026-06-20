// -----------------------------------------------------------------------
// <copyright file="RecommendationDisposition.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;

namespace Karamchari.Intelligence.Domain.Interventions;

/// <summary>
/// What happened to a recommendation that did not become an intervention.
/// Lightweight record — does not need aggregate-root lifecycle.
/// </summary>
public enum RecommendationDispositionType
{
    /// <summary>Owner explicitly accepted; an InterventionInstance was created.</summary>
    Accepted,
    /// <summary>Owner explicitly declined — not applicable to this employee right now.</summary>
    Declined,
    /// <summary>Owner acknowledged but deferred; marked as low priority.</summary>
    Deferred,
    /// <summary>Recommendation closed automatically because risk score dropped below threshold.</summary>
    Expired
}

/// <summary>
/// Records the fate of a <c>WorkforceRecommendation</c> that was presented to an owner.
///
/// Without this, the platform can measure:
///   Intervention success rate (50 completions / 60 started = 83%)
///
/// but cannot measure:
///   Recommendation acceptance rate (60 started / 500 recommended = 12%)
///
/// The acceptance rate distinction is operationally critical — a technically effective
/// intervention that nobody acts on is not a viable recommendation.
///
/// One disposition per recommendation (unique on TenantId + RecommendationId).
/// Maps to Intel_RecommendationDispositions.
/// </summary>
public sealed class RecommendationDisposition : ITenantOwned
{
    public Guid Id { get; private set; }

    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    public Guid RecommendationId { get; private set; }

    public Guid EmployeeId { get; private set; }

    public RecommendationDispositionType Disposition { get; private set; }

    /// <summary>Manager, HR user, or system actor that recorded the disposition.</summary>
    public string? ActorId { get; private set; }

    public DateTime RecordedAt { get; private set; }

    /// <summary>Optional free-text reason for Declined or Deferred dispositions.</summary>
    public string? Reason { get; private set; }

    private RecommendationDisposition() { }

    public static RecommendationDisposition Record(
        string tenantId,
        Guid recommendationId,
        Guid employeeId,
        RecommendationDispositionType disposition,
        string? actorId = null,
        string? reason = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return new RecommendationDisposition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RecommendationId = recommendationId,
            EmployeeId = employeeId,
            Disposition = disposition,
            ActorId = actorId,
            RecordedAt = DateTime.UtcNow,
            Reason = reason
        };
    }
}
