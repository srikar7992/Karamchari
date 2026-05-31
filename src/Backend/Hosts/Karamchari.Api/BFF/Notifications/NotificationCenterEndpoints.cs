using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Notifications.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Notifications;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public static class NotificationCenterEndpoints
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WebApplication MapNotificationCenter(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/notifications").RequireAuthorization();

        group.MapGet(string.Empty, GetInbox)
            .WithName("Notifications.Inbox");

        group.MapGet("/unread-count", GetUnreadCount)
            .WithName("Notifications.UnreadCount");

        group.MapPatch("/{id:guid}/read", MarkAsRead)
            .WithName("Notifications.MarkRead");

        return app;
    }

    private static async Task<IResult> GetInbox(
        ClaimsPrincipal user,
        HttpRequest request,
        NotificationsDbContext db,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] bool? unreadOnly,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();
        if (employeeId == Guid.Empty) return Results.Unauthorized();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize == 0 ? 20 : pageSize, 1, 100);

        var query = db.NotificationMessages
            .AsNoTracking()
            .Where(n => n.RecipientEmployeeId == employeeId
                     && n.Status == Karamchari.Notifications.Domain.NotificationStatus.Delivered
                     || n.Status == Karamchari.Notifications.Domain.NotificationStatus.DigestQueued);

        if (unreadOnly == true)
            query = db.NotificationMessages
                .AsNoTracking()
                .Where(n => n.RecipientEmployeeId == employeeId && !n.IsRead);

        query = query.OrderByDescending(n => n.CreatedOnUtc);

        var total = await query.CountAsync(ct);
        var unread = await db.NotificationMessages
            .AsNoTracking()
            .CountAsync(n => n.RecipientEmployeeId == employeeId && !n.IsRead, ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto(
                n.Id,
                n.Category.ToString(),
                n.PreferredChannel.ToString(),
                n.RenderedSubject,
                n.RenderedBodyText,
                n.Status.ToString(),
                n.IsRead,
                n.CreatedOnUtc,
                n.DeliveredOnUtc,
                n.ReadOnUtc))
            .ToListAsync(ct);

        return Results.Ok(new NotificationInboxResponse(items, unread, total, page, pageSize, total > page * pageSize));
    }

    private static async Task<IResult> GetUnreadCount(
        ClaimsPrincipal user,
        HttpRequest request,
        NotificationsDbContext db,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();
        if (employeeId == Guid.Empty) return Results.Unauthorized();

        var count = await db.NotificationMessages
            .AsNoTracking()
            .CountAsync(n => n.RecipientEmployeeId == employeeId && !n.IsRead, ct);

        return Results.Ok(new UnreadCountResponse(count));
    }

    private static async Task<IResult> MarkAsRead(
        Guid id,
        ClaimsPrincipal user,
        HttpRequest request,
        NotificationsDbContext db,
        CancellationToken ct)
    {
        var employeeId = user.GetEmployeeId();
        if (employeeId is null) return Results.Unauthorized();
        if (employeeId == Guid.Empty) return Results.Unauthorized();

        var message = await db.NotificationMessages
            .FirstOrDefaultAsync(n => n.Id == id && n.RecipientEmployeeId == employeeId, ct);

        if (message is null) return Results.NotFound();

        message.MarkRead();
        await db.SaveChangesAsync(ct);

        return Results.Ok(new MarkReadResponse(true));
    }
}
