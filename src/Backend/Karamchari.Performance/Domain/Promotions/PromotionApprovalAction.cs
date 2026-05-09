using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Promotions;

/// <summary>
/// Immutable audit record for each approval or rejection in the promotion workflow.
/// Append-only â€” never updated after creation.
/// </summary>
public sealed class PromotionApprovalAction : Entity<Guid>
{
    private PromotionApprovalAction() { /* EF materialization */ }

    internal PromotionApprovalAction(
        Guid id,
        Guid recommendationId,
        PromotionApprovalStage stage,
        Guid actorId,
        bool approved,
        string? comments) : base(id)
    {
        RecommendationId = recommendationId;
        Stage = stage;
        ActorId = actorId;
        Approved = approved;
        Comments = comments;
        ActedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid RecommendationId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public PromotionApprovalStage Stage { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ActorId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool Approved { get; private set; }
    public string? Comments { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset ActedOnUtc { get; private set; }
}
