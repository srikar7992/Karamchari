using FluentAssertions;
using Karamchari.Core.Multitenancy;
using Karamchari.Recruitment.Domain;
using Karamchari.Recruitment.Persistence;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

// Disambiguate: System.ApplicationId vs Karamchari.Recruitment.Domain.ApplicationId
using ApplicationId = Karamchari.Recruitment.Domain.ApplicationId;

namespace Karamchari.Recruitment.Tests.Application;

/// <summary>
/// Proves that long-running Recruitment state machines survive worker restarts.
///
/// In a distributed worker deployment, the worker process can stop (deployment,
/// OOM kill, host preemption) at any point during an offer lifecycle. Because all
/// state is persisted in the database via EF Core, a new worker instance must be
/// able to load and continue the workflow from exactly where it left off — no
/// in-memory state, no coordinator re-computation.
///
/// These tests simulate worker restart by:
///   1. Creating a new <see cref="RecruitmentDbContext"/> (instance A = "worker-before-restart")
///   2. Advancing state and persisting
///   3. Disposing instance A (context discarded — all in-memory change-tracker state lost)
///   4. Creating a fresh <see cref="RecruitmentDbContext"/> (instance B = "worker-after-restart")
///   5. Loading the aggregate from DB and continuing the workflow
/// </summary>
public sealed class WorkflowRecoveryTests
{
    private const string TenantId = "tenant-recovery";

    // ── Offer: Issued state survives worker restart → Accept succeeds ─────────

    [Fact]
    public async Task OfferInIssuedStateSurvivesWorkerRestartAcceptSucceeds()
    {
        var dbName = Guid.NewGuid().ToString();
        var applicationId = ApplicationId.New();
        OfferId offerId;

        // ── Phase 1: Worker-before-restart ────────────────────────────────────
        await using (var ctxA = CreateDbContext(dbName))
        {
            var offer = Offer.Create(applicationId, 95_000m, "USD");
            offer.SubmitForApproval();
            offer.Approve();
            offer.Issue(DateTimeOffset.UtcNow.AddDays(14));

            offerId = offer.Id;
            ctxA.Offers.Add(offer);
            await ctxA.SaveChangesAsync();
        }
        // ctxA disposed — change tracker destroyed. Simulates worker process exit.

        // ── Phase 2: Worker-after-restart ─────────────────────────────────────
        await using (var ctxB = CreateDbContext(dbName))
        {
            var loaded = await ctxB.Offers
                .FirstOrDefaultAsync(o => o.Id == offerId);

            // Assert: state survived restart
            loaded.Should().NotBeNull("offer must persist across worker restarts");
            loaded!.Status.Should().Be(OfferStatus.Issued,
                "Issued state must be durable — not held in memory");

            // Act: resume workflow — Accept the offer
            loaded.Accept();
            await ctxB.SaveChangesAsync();
        }

        // ── Phase 3: Verify final state persisted ─────────────────────────────
        await using var ctxC = CreateDbContext(dbName);
        var final = await ctxC.Offers.FirstAsync(o => o.Id == offerId);
        final.Status.Should().Be(OfferStatus.Accepted,
            "Accept() must persist correctly after post-restart continuation");
    }

    // ── Offer: PendingApproval survives restart → Approve → Issue ────────────

    [Fact]
    public async Task OfferInPendingApprovalStateSurvivesRestartApproveAndIssueSucceed()
    {
        var dbName = Guid.NewGuid().ToString();
        var applicationId = ApplicationId.New();
        OfferId offerId;

        // Phase 1: create offer and submit for approval
        await using (var ctxA = CreateDbContext(dbName))
        {
            var offer = Offer.Create(applicationId, 80_000m, "USD");
            offer.SubmitForApproval();
            offerId = offer.Id;
            ctxA.Offers.Add(offer);
            await ctxA.SaveChangesAsync();
        }

        // Phase 2: restart — approve
        await using (var ctxB = CreateDbContext(dbName))
        {
            var loaded = await ctxB.Offers.FirstAsync(o => o.Id == offerId);
            loaded.Status.Should().Be(OfferStatus.PendingApproval,
                "PendingApproval state must survive worker restart");

            loaded.Approve();
            await ctxB.SaveChangesAsync();
        }

        // Phase 3: restart — issue
        await using (var ctxC = CreateDbContext(dbName))
        {
            var loaded = await ctxC.Offers.FirstAsync(o => o.Id == offerId);
            loaded.Status.Should().Be(OfferStatus.Approved,
                "Approved state must survive second worker restart");

            loaded.Issue(DateTimeOffset.UtcNow.AddDays(30));
            await ctxC.SaveChangesAsync();
        }

        // Phase 4: verify
        await using var ctxD = CreateDbContext(dbName);
        var final = await ctxD.Offers.FirstAsync(o => o.Id == offerId);
        final.Status.Should().Be(OfferStatus.Issued,
            "full offer lifecycle across 3 worker restarts must complete correctly");
    }

    // ── Multiple concurrent long-running offers ───────────────────────────────

    [Fact]
    public async Task MultipleIssuedOffersAllSurviveRestartCanEachBeAccepted()
    {
        var dbName = Guid.NewGuid().ToString();
        var offerIds = new List<OfferId>();

        // Phase 1: create 5 offers, all advanced to Issued
        await using (var ctxA = CreateDbContext(dbName))
        {
            for (var i = 0; i < 5; i++)
            {
                var offer = Offer.Create(ApplicationId.New(), 70_000m + (i * 10_000m), "USD");
                offer.SubmitForApproval();
                offer.Approve();
                offer.Issue(DateTimeOffset.UtcNow.AddDays(14));
                offerIds.Add(offer.Id);
                ctxA.Offers.Add(offer);
            }
            await ctxA.SaveChangesAsync();
        }

        // Phase 2: restart — accept all 5
        await using (var ctxB = CreateDbContext(dbName))
        {
            var offers = await ctxB.Offers
                .Where(o => offerIds.Contains(o.Id))
                .ToListAsync();

            offers.Should().HaveCount(5, "all 5 offers must persist");
            offers.Should().AllSatisfy(o => o.Status.Should().Be(OfferStatus.Issued));

            foreach (var offer in offers)
            {
                offer.Accept();
            }
            await ctxB.SaveChangesAsync();
        }

        // Phase 3: verify
        await using var ctxC = CreateDbContext(dbName);
        var finalOffers = await ctxC.Offers
            .Where(o => offerIds.Contains(o.Id))
            .ToListAsync();

        finalOffers.Should().AllSatisfy(o =>
            o.Status.Should().Be(OfferStatus.Accepted,
                "all 5 offers must accept successfully after worker restart"));
    }

    // ── Decline after restart ─────────────────────────────────────────────────

    [Fact]
    public async Task OfferInIssuedStateSurvivesRestartDeclineSucceeds()
    {
        var dbName = Guid.NewGuid().ToString();
        var applicationId = ApplicationId.New();
        OfferId offerId;

        await using (var ctxA = CreateDbContext(dbName))
        {
            var offer = Offer.Create(applicationId, 90_000m, "USD");
            offer.SubmitForApproval();
            offer.Approve();
            offer.Issue(DateTimeOffset.UtcNow.AddDays(14));
            offerId = offer.Id;
            ctxA.Offers.Add(offer);
            await ctxA.SaveChangesAsync();
        }

        await using (var ctxB = CreateDbContext(dbName))
        {
            var loaded = await ctxB.Offers.FirstAsync(o => o.Id == offerId);
            loaded.Status.Should().Be(OfferStatus.Issued);
            loaded.Decline();
            await ctxB.SaveChangesAsync();
        }

        await using var ctxC = CreateDbContext(dbName);
        var final = await ctxC.Offers.FirstAsync(o => o.Id == offerId);
        final.Status.Should().Be(OfferStatus.Declined,
            "Decline after restart must persist correctly");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static RecruitmentDbContext CreateDbContext(string dbName)
    {
        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentTenantId().Returns(TenantId);
        var envelope = new TenantExecutionEnvelope(
            TenantId, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(),
            ExecutionSource.Test, TenantSource.AdminOverride);
        tenantProvider.GetTenant().Returns(envelope);
        tenantProvider.TryGetCurrentTenantId(out Arg.Any<string?>()).Returns(x =>
        {
            x[0] = TenantId;
            return true;
        });

        var options = new DbContextOptionsBuilder<RecruitmentDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(new RecruitmentTestStampingInterceptor(tenantProvider))
            .Options;

        return new RecruitmentDbContext(options, tenantProvider);
    }
}
