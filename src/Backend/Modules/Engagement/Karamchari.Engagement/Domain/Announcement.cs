// -----------------------------------------------------------------------
// <copyright file="Announcement.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Engagement.Domain;

public enum AnnouncementAudience { All, Department, Location, Role }

public record AnnouncementId(Guid Value)
{
    public static AnnouncementId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public record PollId(Guid Value)
{
    public static PollId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Company-wide or targeted announcement. Posted to social wall and employee notification feed.
/// </summary>
public sealed class Announcement : AggregateRoot<AnnouncementId>, ITenantOwned
{
    private Announcement(
        AnnouncementId id,
        string tenantId,
        Guid authorEmployeeId,
        string title,
        string body,
        AnnouncementAudience audience,
        Guid? audienceEntityId,
        DateTimeOffset? expiresAt)
        : base(id)
    {
        TenantId = tenantId;
        AuthorEmployeeId = authorEmployeeId;
        Title = title;
        Body = body;
        Audience = audience;
        AudienceEntityId = audienceEntityId;
        ExpiresAt = expiresAt;
        PublishedAt = DateTimeOffset.UtcNow;
        IsActive = true;
    }

    private Announcement() { TenantId = string.Empty; Title = string.Empty; Body = string.Empty; }

    public string TenantId { get; private set; }
    public Guid AuthorEmployeeId { get; private set; }
    public string Title { get; private set; }
    public string Body { get; private set; }
    public AnnouncementAudience Audience { get; private set; }
    public Guid? AudienceEntityId { get; private set; }
    public DateTimeOffset PublishedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsExpired => ExpiresAt.HasValue && DateTimeOffset.UtcNow > ExpiresAt.Value;

    public static Announcement Publish(
        string tenantId,
        Guid authorId,
        string title,
        string body,
        AnnouncementAudience audience = AnnouncementAudience.All,
        Guid? audienceEntityId = null,
        DateTimeOffset? expiresAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        return new Announcement(AnnouncementId.New(), tenantId, authorId, title, body, audience, audienceEntityId, expiresAt);
    }

    public void Retract() => IsActive = false;
}

/// <summary>
/// Quick poll for gathering team opinion. Single-choice. Auto-closes on CloseOn.
/// </summary>
public sealed class Poll : AggregateRoot<PollId>, ITenantOwned
{
    private readonly List<PollVote> _votes = [];

    private Poll(
        PollId id,
        string tenantId,
        Guid createdByEmployeeId,
        string question,
        string[] choices,
        DateOnly closeOn)
        : base(id)
    {
        TenantId = tenantId;
        CreatedByEmployeeId = createdByEmployeeId;
        Question = question;
        Choices = choices;
        CloseOn = closeOn;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private Poll() { TenantId = string.Empty; Question = string.Empty; Choices = []; }

    public string TenantId { get; private set; }
    public Guid CreatedByEmployeeId { get; private set; }
    public string Question { get; private set; }
    public string[] Choices { get; private set; }
    public DateOnly CloseOn { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public bool IsClosed => DateOnly.FromDateTime(DateTime.UtcNow) > CloseOn;
    public IReadOnlyList<PollVote> Votes => _votes.AsReadOnly();

    public static Poll Create(string tenantId, Guid createdByEmployeeId, string question, string[] choices, DateOnly closeOn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        if (choices.Length < 2) throw new ArgumentException("Poll needs at least 2 choices.");
        if (closeOn <= DateOnly.FromDateTime(DateTime.UtcNow)) throw new ArgumentException("CloseOn must be future.");
        return new Poll(PollId.New(), tenantId, createdByEmployeeId, question, choices, closeOn);
    }

    public void Vote(Guid employeeId, int choiceIndex)
    {
        if (IsClosed) throw new InvalidOperationException("Poll is closed.");
        if (choiceIndex < 0 || choiceIndex >= Choices.Length)
            throw new ArgumentOutOfRangeException(nameof(choiceIndex));

        var existing = _votes.FirstOrDefault(v => v.EmployeeId == employeeId);
        if (existing != null) _votes.Remove(existing);
        _votes.Add(new PollVote(employeeId, choiceIndex, DateTimeOffset.UtcNow));
    }

    public Dictionary<string, int> Tally() =>
        Choices
            .Select((choice, idx) => (choice, count: _votes.Count(v => v.ChoiceIndex == idx)))
            .ToDictionary(x => x.choice, x => x.count);
}

public sealed record PollVote(Guid EmployeeId, int ChoiceIndex, DateTimeOffset VotedAt);
