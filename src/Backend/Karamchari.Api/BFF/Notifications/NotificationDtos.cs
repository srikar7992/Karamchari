namespace Karamchari.Api.BFF.Notifications;

// ── Notification Inbox ────────────────────────────────────────────────────────

public sealed record NotificationInboxResponse(
    IReadOnlyList<NotificationDto> Items,
    int UnreadCount,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasMore);

public sealed record NotificationDto(
    Guid Id,
    string Category,
    string Channel,
    string Subject,
    string BodyText,
    string Status,
    bool IsRead,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DeliveredAt,
    DateTimeOffset? ReadAt);

// ── Unread Count ──────────────────────────────────────────────────────────────

public sealed record UnreadCountResponse(int UnreadCount);

// ── Mark Read ─────────────────────────────────────────────────────────────────

public sealed record MarkReadResponse(bool Success);
