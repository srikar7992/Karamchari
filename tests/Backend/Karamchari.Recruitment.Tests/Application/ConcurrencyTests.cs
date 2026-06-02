using FluentAssertions;
using Karamchari.Recruitment.Application.Commands;
using Karamchari.Recruitment.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Karamchari.Recruitment.Tests.Application;

#pragma warning disable CA1707 // Underscores in test names

public sealed class ConcurrencyTests : RecruitmentIntegrationTestBase
{
    [Fact]
    public async Task CandidateApplyConcurrentRequestsShouldOnlyCreateOneApplication()
    {
        // 1. Arrange
        var reqId = await new CreateRequisitionHandler(DbContext).Handle(new CreateRequisitionCommand("Dev", Guid.NewGuid(), Guid.NewGuid()), default);
        await new PublishRequisitionHandler(DbContext).Handle(new PublishRequisitionCommand(reqId), default);

        var candId = await new CreateCandidateHandler(DbContext).Handle(new CreateCandidateCommand("Jane", "Doe", "jane@example.com", null), default);

        var applyHandler = new ApplyCandidateHandler(DbContext);
        var command = new ApplyCandidateCommand(candId, reqId);

        // 2. Act: Execute 10 concurrent requests
        var tasks = Enumerable.Range(0, 10).Select(_ => applyHandler.Handle(command, default));

        // Use Task.WhenAll and catch exceptions (expecting some to fail with "already applied")
        var results = await Task.WhenAll(tasks.Select(async t =>
        {
            try { return await t; }
            catch { return default; }
        }));

        // 3. Assert: Only one success (non-default ApplicationId)
        var successCount = results.Count(r => r != default);
        successCount.Should().Be(1, "Concurrent apply requests should be idempotent.");

        var appsCount = await DbContext.Applications.CountAsync(a => a.CandidateId == candId && a.RequisitionId == reqId);
        appsCount.Should().Be(1);
    }
}
