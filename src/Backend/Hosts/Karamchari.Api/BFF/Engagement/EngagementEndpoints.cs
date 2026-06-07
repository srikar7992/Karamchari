using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Engagement.Domain;
using Karamchari.Engagement.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Engagement;

public static class EngagementEndpoints
{
    public static WebApplication MapEngagementEndpoints(this WebApplication app)
    {
        var surveys = app.MapGroup("/api/v1/engagement/surveys").RequireAuthorization();
        surveys.MapPost("/", CreateSurvey).WithName("Engagement.Surveys.Create");
        surveys.MapGet("/", ListSurveys).WithName("Engagement.Surveys.List");
        surveys.MapGet("/{surveyId:guid}", GetSurvey).WithName("Engagement.Surveys.Get");
        surveys.MapPost("/{surveyId:guid}/questions", AddQuestion).WithName("Engagement.Surveys.AddQuestion");
        surveys.MapPost("/{surveyId:guid}/launch", LaunchSurvey).WithName("Engagement.Surveys.Launch");
        surveys.MapPost("/{surveyId:guid}/respond", RespondSurvey).WithName("Engagement.Surveys.Respond");
        surveys.MapGet("/{surveyId:guid}/enps", GetENps).WithName("Engagement.Surveys.ENps");

        var recognition = app.MapGroup("/api/v1/engagement/recognition").RequireAuthorization();
        recognition.MapGet("/badges", GetBadges).WithName("Engagement.Recognition.Badges");
        recognition.MapPost("/badges", CreateBadge).WithName("Engagement.Recognition.CreateBadge");
        recognition.MapPost("/", GiveRecognition).WithName("Engagement.Recognition.Give");
        recognition.MapGet("/feed", GetRecognitionFeed).WithName("Engagement.Recognition.Feed");
        recognition.MapPost("/{recognitionId:guid}/react", ReactToRecognition).WithName("Engagement.Recognition.React");

        var announcements = app.MapGroup("/api/v1/engagement/announcements").RequireAuthorization();
        announcements.MapPost("/", PublishAnnouncement).WithName("Engagement.Announcements.Publish");
        announcements.MapGet("/", ListAnnouncements).WithName("Engagement.Announcements.List");
        announcements.MapDelete("/{announcementId:guid}", RetractAnnouncement).WithName("Engagement.Announcements.Retract");

        var polls = app.MapGroup("/api/v1/engagement/polls").RequireAuthorization();
        polls.MapPost("/", CreatePoll).WithName("Engagement.Polls.Create");
        polls.MapGet("/", ListPolls).WithName("Engagement.Polls.List");
        polls.MapPost("/{pollId:guid}/vote", VotePoll).WithName("Engagement.Polls.Vote");
        polls.MapGet("/{pollId:guid}/results", GetPollResults).WithName("Engagement.Polls.Results");

        return app;
    }

    private static async Task<IResult> CreateSurvey([FromBody] CreateSurveyRequest req, ClaimsPrincipal user, EngagementDbContext db, CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        var survey = PulseSurvey.Create(tenantId, req.Title, req.Description, req.IsAnonymous, req.OpenOn, req.CloseOn, employeeId.Value);
        db.PulseSurveys.Add(survey);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/engagement/surveys/{survey.Id.Value}", new { survey.Id });
    }

    private static async Task<IResult> ListSurveys(ClaimsPrincipal user, EngagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var surveys = await db.PulseSurveys
            .Where(s => s.TenantId == tenantId)
            .Select(s => new { s.Id, s.Title, s.Status, s.OpenOn, s.CloseOn, s.IsAnonymous })
            .OrderByDescending(s => s.OpenOn)
            .ToListAsync(ct);
        return Results.Ok(surveys);
    }

    private static async Task<IResult> GetSurvey(Guid surveyId, ClaimsPrincipal user, EngagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var survey = await db.PulseSurveys.FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == new PulseSurveyId(surveyId), ct);
        return survey is null ? Results.NotFound() : Results.Ok(survey);
    }

    private static async Task<IResult> AddQuestion(Guid surveyId, [FromBody] AddQuestionRequest req, ClaimsPrincipal user, EngagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var survey = await db.PulseSurveys.FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == new PulseSurveyId(surveyId), ct);
        if (survey is null) return Results.NotFound();

        survey.AddQuestion(req.Text, req.Type, req.Order, req.Choices);
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> LaunchSurvey(Guid surveyId, ClaimsPrincipal user, EngagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var survey = await db.PulseSurveys.FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == new PulseSurveyId(surveyId), ct);
        if (survey is null) return Results.NotFound();

        survey.Launch();
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { survey.Status });
    }

    private static async Task<IResult> RespondSurvey(Guid surveyId, [FromBody] SurveyResponseRequest req, ClaimsPrincipal user, EngagementDbContext db, CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var survey = await db.PulseSurveys.FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == new PulseSurveyId(surveyId), ct);
        if (survey is null) return Results.NotFound();

        survey.RecordResponse(survey.IsAnonymous ? null : employeeId, req.Answers);
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> GetENps(Guid surveyId, ClaimsPrincipal user, EngagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var survey = await db.PulseSurveys.FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == new PulseSurveyId(surveyId), ct);
        if (survey is null) return Results.NotFound();

        return Results.Ok(new { eNps = survey.ComputeENps(), ResponseCount = survey.ResponseCount });
    }

    private static async Task<IResult> GetBadges(ClaimsPrincipal user, EngagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var badges = await db.Badges.Where(b => b.TenantId == tenantId && b.IsActive)
            .Select(b => new { b.Id, b.Name, b.IconUrl, b.Description })
            .ToListAsync(ct);
        return Results.Ok(badges);
    }

    private static async Task<IResult> CreateBadge([FromBody] CreateBadgeRequest req, ClaimsPrincipal user, EngagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var badge = RecognitionBadge.Create(tenantId, req.Name, req.IconUrl, req.Description);
        db.Badges.Add(badge);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/engagement/recognition/badges/{badge.Id.Value}", new { badge.Id });
    }

    private static async Task<IResult> GiveRecognition([FromBody] GiveRecognitionRequest req, ClaimsPrincipal user, EngagementDbContext db, CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        var recognition = EmployeeRecognition.Give(tenantId, employeeId.Value, req.NomineeEmployeeId, req.Message,
            req.BadgeId.HasValue ? new BadgeId(req.BadgeId.Value) : null, req.IsPublic);
        db.Recognitions.Add(recognition);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/engagement/recognition/{recognition.Id.Value}", new { recognition.Id });
    }

    private static async Task<IResult> GetRecognitionFeed([FromQuery] int page, ClaimsPrincipal user, EngagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var recognitions = await db.Recognitions
            .Where(r => r.TenantId == tenantId && r.IsPublic)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * 20)
            .Take(20)
            .Select(r => new { r.Id, r.NominatorEmployeeId, r.NomineeEmployeeId, r.Message, r.BadgeId, r.CreatedAt })
            .ToListAsync(ct);
        return Results.Ok(recognitions);
    }

    private static async Task<IResult> ReactToRecognition(Guid recognitionId, [FromBody] ReactRequest req, ClaimsPrincipal user, EngagementDbContext db, CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        var recognition = await db.Recognitions.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Id == new RecognitionId(recognitionId), ct);
        if (recognition is null) return Results.NotFound();

        recognition.React(employeeId.Value, req.Emoji);
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> PublishAnnouncement([FromBody] PublishAnnouncementRequest req, ClaimsPrincipal user, EngagementDbContext db, CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        var announcement = Announcement.Publish(tenantId, employeeId.Value, req.Title, req.Body, req.Audience, req.AudienceEntityId, req.ExpiresAt);
        db.Announcements.Add(announcement);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/engagement/announcements/{announcement.Id.Value}", new { announcement.Id });
    }

    private static async Task<IResult> ListAnnouncements(ClaimsPrincipal user, EngagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var announcements = await db.Announcements
            .Where(a => a.TenantId == tenantId && a.IsActive)
            .OrderByDescending(a => a.PublishedAt)
            .Select(a => new { a.Id, a.Title, a.Body, a.Audience, a.PublishedAt, a.ExpiresAt })
            .ToListAsync(ct);
        return Results.Ok(announcements);
    }

    private static async Task<IResult> RetractAnnouncement(Guid announcementId, ClaimsPrincipal user, EngagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var announcement = await db.Announcements.FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == new AnnouncementId(announcementId), ct);
        if (announcement is null) return Results.NotFound();

        announcement.Retract();
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> CreatePoll([FromBody] CreatePollRequest req, ClaimsPrincipal user, EngagementDbContext db, CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        var poll = Poll.Create(tenantId, employeeId.Value, req.Question, req.Choices, req.CloseOn);
        db.Polls.Add(poll);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/engagement/polls/{poll.Id.Value}", new { poll.Id });
    }

    private static async Task<IResult> ListPolls(ClaimsPrincipal user, EngagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var polls = await db.Polls
            .Where(p => p.TenantId == tenantId)
            .Select(p => new { p.Id, p.Question, p.CloseOn, p.CreatedAt })
            .ToListAsync(ct);
        return Results.Ok(polls);
    }

    private static async Task<IResult> VotePoll(Guid pollId, [FromBody] VoteRequest req, ClaimsPrincipal user, EngagementDbContext db, CancellationToken ct)
    {
        var (tenantId, employeeId) = user.GetTenantAndEmployee();
        if (tenantId is null || employeeId is null) return Results.Unauthorized();

        var poll = await db.Polls.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == new PollId(pollId), ct);
        if (poll is null) return Results.NotFound();

        poll.Vote(employeeId.Value, req.ChoiceIndex);
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> GetPollResults(Guid pollId, ClaimsPrincipal user, EngagementDbContext db, CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var poll = await db.Polls.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == new PollId(pollId), ct);
        if (poll is null) return Results.NotFound();

        return Results.Ok(new { poll.Question, Results = poll.Tally(), TotalVotes = poll.Votes.Count });
    }

    private sealed record CreateSurveyRequest(string Title, string? Description, bool IsAnonymous, DateOnly OpenOn, DateOnly CloseOn);
    private sealed record AddQuestionRequest(string Text, QuestionType Type, int Order, string[]? Choices);
    private sealed record SurveyResponseRequest(Dictionary<Guid, string> Answers);
    private sealed record CreateBadgeRequest(string Name, string? IconUrl, string? Description);
    private sealed record GiveRecognitionRequest(Guid NomineeEmployeeId, string Message, Guid? BadgeId, bool IsPublic = true);
    private sealed record ReactRequest(string Emoji);
    private sealed record PublishAnnouncementRequest(string Title, string Body, AnnouncementAudience Audience, Guid? AudienceEntityId, DateTimeOffset? ExpiresAt);
    private sealed record CreatePollRequest(string Question, string[] Choices, DateOnly CloseOn);
    private sealed record VoteRequest(int ChoiceIndex);
}
