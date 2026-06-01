using System.Security.Claims;
using Karamchari.Core.Security;
using Karamchari.Recruitment.Application.Commands;
using Karamchari.Recruitment.Application.Queries;
using Karamchari.Recruitment.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Karamchari.Recruitment.Api;

public static class RecruitmentEndpoints
{
    public static void MapRecruitmentEndpoints(this IEndpointRouteBuilder app)
    {
        var recruitment = app.MapGroup("/api/v1/recruitment")
            .WithTags("Recruitment")
            .RequireAuthorization();

        // Requisition
        recruitment.MapPost("/requisitions", CreateRequisition)
            .RequireAuthorization(Permissions.RecruitmentCreate);

        recruitment.MapGet("/requisitions/{id}", GetRequisition)
            .RequireAuthorization(Permissions.RecruitmentRead);

        recruitment.MapPost("/requisitions/{id}/publish", PublishRequisition)
            .RequireAuthorization(Permissions.RecruitmentUpdate);

        // Candidate
        recruitment.MapPost("/candidates", CreateCandidate)
            .RequireAuthorization(Permissions.RecruitmentCreate);

        // Application
        recruitment.MapPost("/applications", ApplyCandidate)
            .RequireAuthorization(Permissions.RecruitmentCreate);

        recruitment.MapPost("/applications/{id}/advance", AdvanceApplication)
            .RequireAuthorization(Permissions.RecruitmentUpdate);

        // Interview
        recruitment.MapPost("/interviews", ScheduleInterview)
            .RequireAuthorization(Permissions.RecruitmentInterview);

        recruitment.MapPost("/interviews/{id}/feedback", SubmitFeedback)
            .RequireAuthorization(Permissions.RecruitmentInterview);

        // Offer
        recruitment.MapPost("/offers", CreateOffer)
            .RequireAuthorization(Permissions.RecruitmentOffer);

        recruitment.MapPost("/offers/{id}/approve", ApproveOffer)
            .RequireAuthorization(Permissions.RecruitmentOffer);

        recruitment.MapPost("/offers/{id}/issue", IssueOffer)
            .RequireAuthorization(Permissions.RecruitmentOffer);

        recruitment.MapPost("/offers/{id}/accept", AcceptOffer)
            .RequireAuthorization(); // Candidate action

        // Hire
        recruitment.MapPost("/applications/{id}/hire", HireCandidate)
            .RequireAuthorization(Permissions.RecruitmentHire);
    }

    private static async Task<IResult> CreateRequisition(
        [FromBody] CreateRequisitionRequest request,
        [FromServices] IMediator mediator)
    {
        var id = await mediator.Send(new CreateRequisitionCommand(request.Title, request.DepartmentId, request.HiringManagerId));
        return Results.Created($"/api/v1/recruitment/requisitions/{id.Value}", new { id = id.Value });
    }

    private static async Task<IResult> GetRequisition(Guid id, [FromServices] IMediator mediator)
    {
        var result = await mediator.Send(new GetRequisitionQuery(id));
        return result != null ? Results.Ok(result) : Results.NotFound();
    }

    private static async Task<IResult> PublishRequisition(Guid id, [FromServices] IMediator mediator)
    {
        await mediator.Send(new PublishRequisitionCommand(new RequisitionId(id)));
        return Results.NoContent();
    }

    private static async Task<IResult> CreateCandidate(
        [FromBody] CreateCandidateRequest request,
        [FromServices] IMediator mediator)
    {
        var id = await mediator.Send(new CreateCandidateCommand(request.FirstName, request.LastName, request.Email, request.PhoneNumber));
        return Results.Created($"/api/v1/recruitment/candidates/{id.Value}", new { id = id.Value });
    }

    private static async Task<IResult> ApplyCandidate(
        [FromBody] ApplyCandidateRequest request,
        [FromServices] IMediator mediator)
    {
        try
        {
            var id = await mediator.Send(new ApplyCandidateCommand(new CandidateId(request.CandidateId), new RequisitionId(request.RequisitionId)));
            return Results.Created($"/api/v1/recruitment/applications/{id.Value}", new { id = id.Value });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private static async Task<IResult> AdvanceApplication(Guid id, [FromServices] IMediator mediator)
    {
        await mediator.Send(new AdvanceApplicationCommand(new Karamchari.Recruitment.Domain.ApplicationId(id)));
        return Results.NoContent();
    }

    private static async Task<IResult> ScheduleInterview(
        [FromBody] ScheduleInterviewRequest request,
        [FromServices] IMediator mediator)
    {
        var id = await mediator.Send(new ScheduleInterviewCommand(new Karamchari.Recruitment.Domain.ApplicationId(request.ApplicationId), request.ScheduledAt, request.DurationMinutes, request.InterviewerIds));
        return Results.Created($"/api/v1/recruitment/interviews/{id.Value}", new { id = id.Value });
    }

    private static async Task<IResult> SubmitFeedback(
        Guid id,
        [FromBody] SubmitFeedbackRequest request,
        [FromServices] IMediator mediator)
    {
        var feedbackId = await mediator.Send(new SubmitFeedbackCommand(new InterviewId(id), request.InterviewerId, request.Rating, request.Comments));
        return Results.Created($"/api/v1/recruitment/feedback/{feedbackId.Value}", new { id = feedbackId.Value });
    }

    private static async Task<IResult> CreateOffer(
        [FromBody] CreateOfferRequest request,
        [FromServices] IMediator mediator)
    {
        var id = await mediator.Send(new CreateOfferCommand(new Karamchari.Recruitment.Domain.ApplicationId(request.ApplicationId), request.BaseSalary, request.Currency));
        return Results.Created($"/api/v1/recruitment/offers/{id.Value}", new { id = id.Value });
    }

    private static async Task<IResult> ApproveOffer(Guid id, [FromServices] IMediator mediator)
    {
        await mediator.Send(new ApproveOfferCommand(new OfferId(id)));
        return Results.NoContent();
    }

    private static async Task<IResult> IssueOffer(
        Guid id,
        [FromBody] IssueOfferRequest request,
        [FromServices] IMediator mediator)
    {
        await mediator.Send(new IssueOfferCommand(new OfferId(id), request.ExpiresAt));
        return Results.NoContent();
    }

    private static async Task<IResult> AcceptOffer(Guid id, [FromServices] IMediator mediator)
    {
        await mediator.Send(new AcceptOfferCommand(new OfferId(id)));
        return Results.NoContent();
    }

    private static async Task<IResult> HireCandidate(Guid id, ClaimsPrincipal user, [FromServices] IMediator mediator)
    {
        await mediator.Send(new HireCandidateCommand(new Karamchari.Recruitment.Domain.ApplicationId(id), user.Identity?.Name ?? "system"));
        return Results.NoContent();
    }
}

public record CreateRequisitionRequest(string Title, Guid DepartmentId, Guid HiringManagerId);
public record CreateCandidateRequest(string FirstName, string LastName, string Email, string? PhoneNumber);
public record ApplyCandidateRequest(Guid CandidateId, Guid RequisitionId);
public record ScheduleInterviewRequest(Guid ApplicationId, DateTimeOffset ScheduledAt, int DurationMinutes, List<Guid> InterviewerIds);
public record SubmitFeedbackRequest(Guid InterviewerId, int Rating, string Comments);
public record CreateOfferRequest(Guid ApplicationId, decimal BaseSalary, string Currency);
public record IssueOfferRequest(DateTimeOffset ExpiresAt);
