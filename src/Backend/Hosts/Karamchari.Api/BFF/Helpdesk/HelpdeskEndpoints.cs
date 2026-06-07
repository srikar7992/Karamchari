using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Helpdesk.Domain;
using Karamchari.Helpdesk.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Helpdesk;

public static class HelpdeskEndpoints
{
    public static WebApplication MapHelpdeskEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/helpdesk").RequireAuthorization();

        group.MapGet("/categories", GetCategories).WithName("Helpdesk.Categories.List");
        group.MapPost("/categories", CreateCategory).WithName("Helpdesk.Categories.Create");
        group.MapPost("/tickets", RaiseTicket).WithName("Helpdesk.Tickets.Raise");
        group.MapGet("/tickets", ListTickets).WithName("Helpdesk.Tickets.List");
        group.MapGet("/tickets/{ticketId:guid}", GetTicket).WithName("Helpdesk.Tickets.Get");
        group.MapPost("/tickets/{ticketId:guid}/assign", AssignTicket).WithName("Helpdesk.Tickets.Assign");
        group.MapPost("/tickets/{ticketId:guid}/comments", AddComment).WithName("Helpdesk.Tickets.Comment");
        group.MapPost("/tickets/{ticketId:guid}/resolve", ResolveTicket).WithName("Helpdesk.Tickets.Resolve");
        group.MapPost("/tickets/{ticketId:guid}/close", CloseTicket).WithName("Helpdesk.Tickets.Close");
        group.MapPost("/tickets/{ticketId:guid}/reopen", ReopenTicket).WithName("Helpdesk.Tickets.Reopen");

        // Knowledge Base
        group.MapGet("/kb", SearchKb).WithName("Helpdesk.KB.Search");
        group.MapPost("/kb", CreateArticle).WithName("Helpdesk.KB.Create");
        group.MapGet("/kb/{articleId:guid}", GetArticle).WithName("Helpdesk.KB.Get");
        group.MapPut("/kb/{articleId:guid}", UpdateArticle).WithName("Helpdesk.KB.Update");
        group.MapPost("/kb/{articleId:guid}/publish", PublishArticle).WithName("Helpdesk.KB.Publish");
        group.MapPost("/kb/{articleId:guid}/archive", ArchiveArticle).WithName("Helpdesk.KB.Archive");
        group.MapPost("/kb/{articleId:guid}/vote", VoteArticle).WithName("Helpdesk.KB.Vote");

        // Escalation Rules
        group.MapGet("/escalation-rules", ListEscalationRules).WithName("Helpdesk.Escalation.List");
        group.MapPost("/escalation-rules", CreateEscalationRule).WithName("Helpdesk.Escalation.Create");
        group.MapDelete("/escalation-rules/{ruleId:guid}", DeactivateEscalationRule).WithName("Helpdesk.Escalation.Deactivate");
        group.MapGet("/tickets/breached", ListBreachedTickets).WithName("Helpdesk.Tickets.Breached");

        return app;
    }

    private static async Task<IResult> GetCategories(ClaimsPrincipal user, HelpdeskDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var categories = await db.TicketCategories
            .Where(c => c.TenantId == tenantId && c.IsActive)
            .Select(c => new { c.Id, c.Name, c.DefaultAssigneeTeam })
            .ToListAsync(ct);

        return Results.Ok(categories);
    }

    private static async Task<IResult> CreateCategory(
        [FromBody] CreateCategoryRequest req,
        ClaimsPrincipal user,
        HelpdeskDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var category = TicketCategory.Create(tenantId, req.Name, req.DefaultAssigneeTeam);
        db.TicketCategories.Add(category);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/helpdesk/categories/{category.Id.Value}", new { category.Id });
    }

    private static async Task<IResult> RaiseTicket(
        [FromBody] RaiseTicketRequest req,
        ClaimsPrincipal user,
        HelpdeskDbContext db,
        CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        var ticket = SupportTicket.Raise(tenantId, employeeId.Value, new TicketCategoryId(req.CategoryId), req.Subject, req.Description, req.Priority);
        db.SupportTickets.Add(ticket);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/helpdesk/tickets/{ticket.Id.Value}", new { ticket.Id, ticket.Status, ticket.SlaDeadlineAt });
    }

    private static async Task<IResult> ListTickets(
        [FromQuery] string? status,
        [FromQuery] bool myTickets,
        ClaimsPrincipal user,
        HelpdeskDbContext db,
        CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var query = db.SupportTickets.Where(t => t.TenantId == tenantId);
        if (myTickets && employeeId.HasValue) query = query.Where(t => t.EmployeeId == employeeId.Value);
        if (Enum.TryParse<TicketStatus>(status, ignoreCase: true, out var ts)) query = query.Where(t => t.Status == ts);

        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new { t.Id, t.Subject, t.Priority, t.Status, t.EmployeeId, t.AssignedToHrId, t.CreatedAt, t.SlaDeadlineAt, t.ResolvedAt })
            .Take(100)
            .ToListAsync(ct);

        return Results.Ok(tickets);
    }

    private static async Task<IResult> GetTicket(Guid ticketId, ClaimsPrincipal user, HelpdeskDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Id == new SupportTicketId(ticketId), ct);
        return ticket is null ? Results.NotFound() : Results.Ok(ticket);
    }

    private static async Task<IResult> AssignTicket(Guid ticketId, [FromBody] AssignTicketRequest req, ClaimsPrincipal user, HelpdeskDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Id == new SupportTicketId(ticketId), ct);
        if (ticket is null) return Results.NotFound();

        ticket.Assign(req.HrUserId);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { ticket.Status });
    }

    private static async Task<IResult> AddComment(Guid ticketId, [FromBody] AddCommentRequest req, ClaimsPrincipal user, HelpdeskDbContext db, CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Id == new SupportTicketId(ticketId), ct);
        if (ticket is null) return Results.NotFound();

        ticket.AddComment(employeeId.Value, req.Body, req.IsInternal);
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> ResolveTicket(Guid ticketId, [FromBody] ResolveTicketRequest req, ClaimsPrincipal user, HelpdeskDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Id == new SupportTicketId(ticketId), ct);
        if (ticket is null) return Results.NotFound();

        ticket.Resolve(req.Resolution);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { ticket.Status, ticket.ResolvedAt });
    }

    private static async Task<IResult> CloseTicket(Guid ticketId, ClaimsPrincipal user, HelpdeskDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Id == new SupportTicketId(ticketId), ct);
        if (ticket is null) return Results.NotFound();

        ticket.Close();
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> ReopenTicket(Guid ticketId, ClaimsPrincipal user, HelpdeskDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Id == new SupportTicketId(ticketId), ct);
        if (ticket is null) return Results.NotFound();

        ticket.Reopen();
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { ticket.Status });
    }

    private static async Task<IResult> SearchKb([FromQuery] string? q, [FromQuery] Guid? categoryId, ClaimsPrincipal user, HelpdeskDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var query = db.KbArticles.Where(a => a.TenantId == tenantId && a.Status == ArticleStatus.Published);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(a => a.Title.Contains(q) || a.Body.Contains(q));
        if (categoryId.HasValue)
            query = query.Where(a => a.CategoryId == new TicketCategoryId(categoryId.Value));

        var results = await query
            .OrderByDescending(a => a.HelpfulVotes)
            .Select(a => new { a.Id, a.Title, a.Tags, a.ViewCount, a.HelpfulVotes, a.NotHelpfulVotes, a.UpdatedAt })
            .Take(50)
            .ToListAsync(ct);
        return Results.Ok(results);
    }

    private static async Task<IResult> CreateArticle([FromBody] CreateArticleRequest req, ClaimsPrincipal user, HelpdeskDbContext db, CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        var article = KbArticle.Create(tenantId, req.Title, req.Body,
            req.CategoryId.HasValue ? new TicketCategoryId(req.CategoryId.Value) : null,
            employeeId.Value, req.Tags ?? []);
        db.KbArticles.Add(article);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/helpdesk/kb/{article.Id.Value}", new { article.Id });
    }

    private static async Task<IResult> GetArticle(Guid articleId, ClaimsPrincipal user, HelpdeskDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var article = await db.KbArticles.FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == new KbArticleId(articleId), ct);
        if (article is null) return Results.NotFound();

        article.RecordView();
        await db.SaveChangesAsync(ct);
        return Results.Ok(article);
    }

    private static async Task<IResult> UpdateArticle(Guid articleId, [FromBody] UpdateArticleRequest req, ClaimsPrincipal user, HelpdeskDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var article = await db.KbArticles.FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == new KbArticleId(articleId), ct);
        if (article is null) return Results.NotFound();

        article.Update(req.Title, req.Body, req.Tags ?? []);
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> PublishArticle(Guid articleId, ClaimsPrincipal user, HelpdeskDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var article = await db.KbArticles.FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == new KbArticleId(articleId), ct);
        if (article is null) return Results.NotFound();

        article.Publish();
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { article.Status });
    }

    private static async Task<IResult> ArchiveArticle(Guid articleId, ClaimsPrincipal user, HelpdeskDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var article = await db.KbArticles.FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == new KbArticleId(articleId), ct);
        if (article is null) return Results.NotFound();

        article.Archive();
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { article.Status });
    }

    private static async Task<IResult> VoteArticle(Guid articleId, [FromBody] VoteArticleRequest req, ClaimsPrincipal user, HelpdeskDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var article = await db.KbArticles.FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == new KbArticleId(articleId), ct);
        if (article is null) return Results.NotFound();

        article.Vote(req.Helpful);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { article.HelpfulVotes, article.NotHelpfulVotes });
    }

    private static async Task<IResult> ListEscalationRules(ClaimsPrincipal user, HelpdeskDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var rules = await db.EscalationRules
            .Where(r => r.TenantId == tenantId && r.IsActive)
            .Select(r => new { r.Id, r.RuleName, r.AppliesToPriority, r.BreachAfterMinutes, r.Target, r.Level })
            .OrderBy(r => r.Level)
            .ToListAsync(ct);
        return Results.Ok(rules);
    }

    private static async Task<IResult> CreateEscalationRule([FromBody] CreateEscalationRuleRequest req, ClaimsPrincipal user, HelpdeskDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var rule = EscalationRule.Create(tenantId, req.RuleName, req.AppliesToPriority,
            req.BreachAfterMinutes, req.Target, req.Level);
        db.EscalationRules.Add(rule);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/helpdesk/escalation-rules/{rule.Id.Value}", new { rule.Id });
    }

    private static async Task<IResult> DeactivateEscalationRule(Guid ruleId, ClaimsPrincipal user, HelpdeskDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var rule = await db.EscalationRules.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Id == new EscalationRuleId(ruleId), ct);
        if (rule is null) return Results.NotFound();

        rule.Deactivate();
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> ListBreachedTickets(ClaimsPrincipal user, HelpdeskDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var now = DateTimeOffset.UtcNow;
        var tickets = await db.SupportTickets
            .Where(t => t.TenantId == tenantId
                && t.Status != TicketStatus.Resolved
                && t.Status != TicketStatus.Closed
                && t.SlaDeadlineAt < now)
            .OrderBy(t => t.SlaDeadlineAt)
            .Select(t => new { t.Id, t.Subject, t.Priority, t.Status, t.SlaDeadlineAt, t.AssignedToHrId, BreachedByMinutes = (int)(now - t.SlaDeadlineAt).TotalMinutes })
            .ToListAsync(ct);
        return Results.Ok(tickets);
    }

    private sealed record CreateCategoryRequest(string Name, string? DefaultAssigneeTeam);
    private sealed record RaiseTicketRequest(Guid CategoryId, string Subject, string Description, TicketPriority Priority);
    private sealed record AssignTicketRequest(Guid HrUserId);
    private sealed record AddCommentRequest(string Body, bool IsInternal = false);
    private sealed record ResolveTicketRequest(string Resolution);
    private sealed record CreateArticleRequest(string Title, string Body, Guid? CategoryId, string[]? Tags);
    private sealed record UpdateArticleRequest(string Title, string Body, string[]? Tags);
    private sealed record VoteArticleRequest(bool Helpful);
    private sealed record CreateEscalationRuleRequest(string RuleName, TicketPriority? AppliesToPriority, int BreachAfterMinutes, EscalationTarget Target, int Level);
}
