namespace Karamchari.Api.BFF.Notifications;

// â”€â”€ Notification Inbox â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record NotificationInboxResponse(
    IReadOnlyList<NotificationDto> Items,
    int UnreadCount,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasMore);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
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

// â”€â”€ Unread Count â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record UnreadCountResponse(int UnreadCount);

// â”€â”€ Mark Read â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record MarkReadResponse(bool Success);
