// -----------------------------------------------------------------------
// <copyright file="PulseSurvey.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Engagement.Domain;

public enum SurveyStatus { Draft, Active, Closed }
public enum QuestionType { Rating, YesNo, OpenText, MultipleChoice, NpsScale }

public record PulseSurveyId(Guid Value)
{
    public static PulseSurveyId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Pulse survey distributed to a target audience (all, department, role, etc.).
/// Collects anonymous or identified responses. eNPS is a specialised variant with NpsScale question.
/// </summary>
public sealed class PulseSurvey : AggregateRoot<PulseSurveyId>, ITenantOwned
{
    private readonly List<SurveyQuestion> _questions = [];
    private readonly List<SurveyResponse> _responses = [];

    private PulseSurvey(
        PulseSurveyId id,
        string tenantId,
        string title,
        string? description,
        bool isAnonymous,
        DateOnly openOn,
        DateOnly closeOn,
        Guid createdByEmployeeId)
        : base(id)
    {
        TenantId = tenantId;
        Title = title;
        Description = description;
        IsAnonymous = isAnonymous;
        OpenOn = openOn;
        CloseOn = closeOn;
        CreatedByEmployeeId = createdByEmployeeId;
        Status = SurveyStatus.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private PulseSurvey() { TenantId = string.Empty; Title = string.Empty; }

    public string TenantId { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public bool IsAnonymous { get; private set; }
    public DateOnly OpenOn { get; private set; }
    public DateOnly CloseOn { get; private set; }
    public Guid CreatedByEmployeeId { get; private set; }
    public SurveyStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyList<SurveyQuestion> Questions => _questions.AsReadOnly();
    public IReadOnlyList<SurveyResponse> Responses => _responses.AsReadOnly();

    public int ResponseCount => _responses.Count;

    public static PulseSurvey Create(
        string tenantId,
        string title,
        string? description,
        bool isAnonymous,
        DateOnly openOn,
        DateOnly closeOn,
        Guid createdByEmployeeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (closeOn <= openOn) throw new ArgumentException("CloseOn must be after OpenOn.");
        return new PulseSurvey(PulseSurveyId.New(), tenantId, title, description, isAnonymous, openOn, closeOn, createdByEmployeeId);
    }

    public void AddQuestion(string text, QuestionType type, int order, string[]? choices = null)
    {
        if (Status != SurveyStatus.Draft) throw new InvalidOperationException("Cannot modify active survey.");
        _questions.Add(new SurveyQuestion(Guid.NewGuid(), Id, text, type, order, choices));
    }

    public void Launch()
    {
        if (_questions.Count == 0) throw new InvalidOperationException("Survey must have at least one question.");
        Status = SurveyStatus.Active;
    }

    public void RecordResponse(Guid? employeeId, Dictionary<Guid, string> answers)
    {
        if (Status != SurveyStatus.Active) throw new InvalidOperationException("Survey is not active.");
        if (!IsAnonymous && employeeId == null) throw new ArgumentException("Identified survey requires employeeId.");
        if (IsAnonymous && !IsAnonymous) { /* allowed anonymous */ }

        // Prevent duplicate identified responses
        if (employeeId.HasValue && _responses.Any(r => r.EmployeeId == employeeId))
            throw new InvalidOperationException("Employee already responded.");

        _responses.Add(new SurveyResponse(Guid.NewGuid(), Id, employeeId, answers, DateTimeOffset.UtcNow));
    }

    public void Close() => Status = SurveyStatus.Closed;

    /// <summary>
    /// Computes eNPS from NpsScale question responses. Returns null if no NPS question present.
    /// eNPS = % Promoters (9-10) - % Detractors (0-6).
    /// </summary>
    public decimal? ComputeENps()
    {
        var npsQuestion = _questions.FirstOrDefault(q => q.Type == QuestionType.NpsScale);
        if (npsQuestion == null || _responses.Count == 0) return null;

        var scores = _responses
            .Where(r => r.Answers.ContainsKey(npsQuestion.Id))
            .Select(r => int.TryParse(r.Answers[npsQuestion.Id], out var s) ? s : -1)
            .Where(s => s >= 0 && s <= 10)
            .ToList();

        if (scores.Count == 0) return null;

        decimal total = scores.Count;
        decimal promoters = scores.Count(s => s >= 9);
        decimal detractors = scores.Count(s => s <= 6);

        return Math.Round((promoters / total * 100) - (detractors / total * 100), 1);
    }
}

public sealed class SurveyQuestion : Entity<Guid>
{
    internal SurveyQuestion(Guid id, PulseSurveyId surveyId, string text, QuestionType type, int order, string[]? choices)
        : base(id)
    {
        SurveyId = surveyId;
        Text = text;
        Type = type;
        Order = order;
        Choices = choices ?? [];
    }

    private SurveyQuestion() { SurveyId = null!; Text = string.Empty; Choices = []; }

    public PulseSurveyId SurveyId { get; private set; }
    public string Text { get; private set; }
    public QuestionType Type { get; private set; }
    public int Order { get; private set; }
    public string[] Choices { get; private set; }
}

public sealed class SurveyResponse : Entity<Guid>
{
    internal SurveyResponse(Guid id, PulseSurveyId surveyId, Guid? employeeId, Dictionary<Guid, string> answers, DateTimeOffset respondedAt)
        : base(id)
    {
        SurveyId = surveyId;
        EmployeeId = employeeId;
        Answers = answers;
        RespondedAt = respondedAt;
    }

    private SurveyResponse() { SurveyId = null!; Answers = []; }

    public PulseSurveyId SurveyId { get; private set; }
    public Guid? EmployeeId { get; private set; }
    public Dictionary<Guid, string> Answers { get; private set; }
    public DateTimeOffset RespondedAt { get; private set; }
}
