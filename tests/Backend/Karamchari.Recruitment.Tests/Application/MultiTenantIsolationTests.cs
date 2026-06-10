// -----------------------------------------------------------------------
// <copyright file="MultiTenantIsolationTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Recruitment.Application.Commands;
using Karamchari.Recruitment.Domain;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Karamchari.Recruitment.Tests.Application;

#pragma warning disable CA1707 // Underscores in test names

public sealed class MultiTenantIsolationTests : RecruitmentIntegrationTestBase
{
    [Fact]
    public async Task CandidateApply_WhenCandidateBelongsToOtherTenant_ShouldThrowNotFound()
    {
        // 1. Arrange: Tenant A creates a candidate
        var candHandler = new CreateCandidateHandler(DbContext);
        var candId = await candHandler.Handle(new CreateCandidateCommand("Jane", "Doe", "jane@example.com", null), default);

        var reqHandler = new CreateRequisitionHandler(DbContext);
        var reqId = await reqHandler.Handle(new CreateRequisitionCommand("Dev", Guid.NewGuid(), Guid.NewGuid()), default);
        await new PublishRequisitionHandler(DbContext).Handle(new PublishRequisitionCommand(reqId), default);

        // 2. Act: Tenant B attempts to apply with Tenant A's candidate
        TenantProvider.GetCurrentTenantId().Returns("tenant-b");

        var applyHandler = new ApplyCandidateHandler(DbContext);
        var act = () => applyHandler.Handle(new ApplyCandidateCommand(candId, reqId), default);

        // 3. Assert: Fail because Global Query Filter excludes Tenant A's data for Tenant B
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage($"Candidate {candId.Value} not found.");
    }

    [Fact]
    public async Task GetRequisition_WhenRequisitionBelongsToOtherTenant_ShouldReturnNull()
    {
        // Arrange
        var reqHandler = new CreateRequisitionHandler(DbContext);
        var reqId = await reqHandler.Handle(new CreateRequisitionCommand("Dev", Guid.NewGuid(), Guid.NewGuid()), default);

        // Act
        TenantProvider.GetCurrentTenantId().Returns("tenant-b");
        var req = await DbContext.Requisitions.FirstOrDefaultAsync(r => r.Id == reqId);

        // Assert
        req.Should().BeNull();
    }
}
