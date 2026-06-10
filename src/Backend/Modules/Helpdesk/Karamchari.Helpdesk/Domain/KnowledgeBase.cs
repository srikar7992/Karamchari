// -----------------------------------------------------------------------
// <copyright file="KnowledgeBase.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Helpdesk.Domain;

public record KbArticleId(Guid Value);
public record EscalationRuleId(Guid Value);

public enum ArticleStatus { Draft, Published, Archived }
public enum EscalationTarget { AssignedAgent, TeamLead, HrManager, HrDirector }

// ── KnowledgeBase Article ─────────────────────────────────────────────────────
/// <summary>
/// Self-service knowledge article. Employees search before raising tickets.
/// Tracks helpfulness votes to surface quality content.
/// </summary>
public sealed class KbArticle : AggregateRoot<KbArticleId>, ITenantOwned
{
    private KbArticle() { TenantId = string.Empty; Title = string.Empty; Body = string.Empty; Tags = []; }

    private KbArticle(KbArticleId id, string tenantId, string title, string body,
        TicketCategoryId? categoryId, Guid authorEmployeeId, string[] tags) : base(id)
    {
        TenantId = tenantId;
        Title = title;
        Body = body;
        CategoryId = categoryId;
        AuthorEmployeeId = authorEmployeeId;
        Tags = tags;
        Status = ArticleStatus.Draft;
        ViewCount = 0;
        HelpfulVotes = 0;
        NotHelpfulVotes = 0;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public string TenantId { get; private set; }
    public string Title { get; private set; }
    public string Body { get; private set; }
    public TicketCategoryId? CategoryId { get; private set; }
    public Guid AuthorEmployeeId { get; private set; }
    public string[] Tags { get; private set; }
    public ArticleStatus Status { get; private set; }
    public int ViewCount { get; private set; }
    public int HelpfulVotes { get; private set; }
    public int NotHelpfulVotes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static KbArticle Create(string tenantId, string title, string body,
        TicketCategoryId? categoryId, Guid authorEmployeeId, string[] tags)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        return new KbArticle(new KbArticleId(Guid.NewGuid()), tenantId, title, body,
            categoryId, authorEmployeeId, tags);
    }

    public void Publish()
    {
        if (Status == ArticleStatus.Archived)
            throw new InvalidOperationException("Cannot publish archived article.");
        Status = ArticleStatus.Published;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Archive()
    {
        Status = ArticleStatus.Archived;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string title, string body, string[] tags)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title;
        Body = body;
        Tags = tags;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordView() => ViewCount++;

    public void Vote(bool helpful)
    {
        if (helpful) HelpfulVotes++;
        else NotHelpfulVotes++;
    }

    public double HelpfulnessRatio =>
        (HelpfulVotes + NotHelpfulVotes) == 0 ? 0 :
        (double)HelpfulVotes / (HelpfulVotes + NotHelpfulVotes);
}

// ── EscalationRule ────────────────────────────────────────────────────────────
/// <summary>
/// Defines when and how a ticket escalates.
/// Engine evaluates rules on a scheduled basis; breached tickets auto-escalate.
/// </summary>
public sealed class EscalationRule : AggregateRoot<EscalationRuleId>, ITenantOwned
{
    private EscalationRule() { TenantId = string.Empty; RuleName = string.Empty; }

    private EscalationRule(EscalationRuleId id, string tenantId, string ruleName,
        TicketPriority? appliesTo, int breachAfterMinutes,
        EscalationTarget target, int level) : base(id)
    {
        TenantId = tenantId;
        RuleName = ruleName;
        AppliesToPriority = appliesTo;
        BreachAfterMinutes = breachAfterMinutes;
        Target = target;
        Level = level;   // 1 = first escalation, 2 = second, etc.
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string TenantId { get; private set; }
    public string RuleName { get; private set; }
    public TicketPriority? AppliesToPriority { get; private set; }
    public int BreachAfterMinutes { get; private set; }
    public EscalationTarget Target { get; private set; }
    public int Level { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static EscalationRule Create(string tenantId, string ruleName,
        TicketPriority? appliesTo, int breachAfterMinutes, EscalationTarget target, int level)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(breachAfterMinutes);
        return new EscalationRule(new EscalationRuleId(Guid.NewGuid()), tenantId, ruleName,
            appliesTo, breachAfterMinutes, target, level);
    }

    public void Deactivate() => IsActive = false;
}

// ── TicketEscalation record ───────────────────────────────────────────────────
/// <summary>Immutable record of a single escalation event on a ticket.</summary>
public sealed class TicketEscalation
{
    public TicketEscalation() { }

    public TicketEscalation(SupportTicketId ticketId, int level,
        EscalationTarget target, Guid? notifiedUserId, string reason)
    {
        TicketId = ticketId;
        Level = level;
        Target = target;
        NotifiedUserId = notifiedUserId;
        Reason = reason;
        EscalatedAt = DateTimeOffset.UtcNow;
    }

    public SupportTicketId TicketId { get; private set; } = null!;
    public int Level { get; private set; }
    public EscalationTarget Target { get; private set; }
    public Guid? NotifiedUserId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset EscalatedAt { get; private set; }
}
