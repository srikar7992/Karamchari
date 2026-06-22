namespace Karamchari.Approvals.Domain;

public sealed record ApprovalAction(
    Guid ActorId,
    string ActorName,
    ApprovalRequestStatus Decision,
    string? Comment,
    DateTimeOffset ActedAtUtc);
