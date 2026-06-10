// -----------------------------------------------------------------------
// <copyright file="IdempotencyTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Recruitment.Application.Commands;
using Karamchari.Recruitment.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Karamchari.Recruitment.Tests.Application;

#pragma warning disable CA1707 // Underscores in test names

public sealed class IdempotencyTests : RecruitmentIntegrationTestBase
{
    [Fact]
    public async Task OfferAcceptShouldBeIdempotent()
    {
        // 1. Arrange
        var app = Karamchari.Recruitment.Domain.Application.Create(CandidateId.New(), RequisitionId.New(), new CandidateSnapshot("J", "D", "j@d.com", null, 1));
        app.AdvanceToScreening();
        app.AdvanceToInterviewing();
        DbContext.Applications.Add(app);

        var offer = Offer.Create(app.Id, 1000, "USD");
        offer.SubmitForApproval();
        offer.Approve();
        offer.Issue(DateTimeOffset.UtcNow.AddDays(1));
        DbContext.Offers.Add(offer);
        await DbContext.SaveChangesAsync();

        var handler = new AcceptOfferHandler(DbContext);

        // 2. Act: First accept
        await handler.Handle(new AcceptOfferCommand(offer.Id), default);

        // 3. Act: Second accept should fail or safely ignore
        var act = () => handler.Handle(new AcceptOfferCommand(offer.Id), default);

        // 4. Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Only issued offers can be accepted.");
    }

    [Fact]
    public async Task HireCandidateShouldBeIdempotent()
    {
        // 1. Arrange
        var app = Karamchari.Recruitment.Domain.Application.Create(CandidateId.New(), RequisitionId.New(), new CandidateSnapshot("J", "D", "j@d.com", null, 1));
        app.AdvanceToScreening();
        app.AdvanceToInterviewing();
        app.MarkAsOffered();
        DbContext.Applications.Add(app);
        await DbContext.SaveChangesAsync();

        var handler = new HireCandidateHandler(DbContext);

        // 2. Act: First hire
        await handler.Handle(new HireCandidateCommand(app.Id, "user-1"), default);

        // 3. Act: Second hire
        var act = () => handler.Handle(new HireCandidateCommand(app.Id, "user-1"), default);

        // 4. Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Only applications with an accepted offer can be hired.");
    }
}
