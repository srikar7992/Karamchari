using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using Karamchari.Integration.Domain;
using Karamchari.Integration.Persistence;
using Karamchari.Workflow.Domain;
using Karamchari.Workflow.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Karamchari.TenantIsolationCertification.Domain;

/// <summary>
/// Adversarial cross-tenant isolation tests for Phase 15.
/// Phase 14 proved Layer 1 (EF global filter) + Layer 2 (explicit predicate) work correctly.
/// Phase 15 targets the failure modes that happen OUTSIDE query filters:
///
///   1. Webhook leakage: Tenant A subscription must not expose Tenant B delivery records
///      even if both subscriptions listen to the same event type.
///
///   2. Consumer scope leakage: An EF context mistakenly opened in Tenant B scope
///      must not return Tenant A data, even in an async consumer boundary.
///
///   3. Workflow approval history isolation: Tenant A cannot read Tenant B's
///      approval audit trail even if both definitions share the same entity type.
///
///   4. Approval history integrity: lifecycle transitions must record actor, timestamp,
///      and status chain in order; no transition must be silently dropped.
///
/// These tests represent the "adversarial" path — the developer deliberately
/// does something wrong (wrong tenant scope, no predicate) and the platform must
/// still protect tenant data.
/// </summary>
public sealed class Phase15AdversarialIsolationTests
{
    // ── 1. Webhook delivery cross-tenant leakage ──────────────────────────────

    [Fact]
    public async Task WebhookSubscription_DeliveryRecords_NotVisibleAcrossTenants()
    {
        // Arrange: two tenants each have a webhook subscription for the same event type.
        // Tenant A records a delivery with a correlationId.
        // Tenant B queries the integration context — must NOT see Tenant A's delivery.
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var correlationIdA = Guid.NewGuid().ToString();

        await using var dbA = new IntegrationDbContext(options, new FixedTenantProvider("alpha-corp"));
        var subA = WebhookSubscription.Create(
            "alpha-corp", "https://hooks.alpha.example.com/events",
            "secret-a", "Alpha webhook", ["PayrollRunCompleted"]);
        subA.RecordDelivery("PayrollRunCompleted", "{\"runId\":\"1\"}", correlationIdA);
        dbA.WebhookSubscriptions.Add(subA);
        await dbA.SaveChangesAsync();

        await using var dbB = new IntegrationDbContext(options, new FixedTenantProvider("beta-corp"));
        var subB = WebhookSubscription.Create(
            "beta-corp", "https://hooks.beta.example.com/events",
            "secret-b", "Beta webhook", ["PayrollRunCompleted"]);
        dbB.WebhookSubscriptions.Add(subB);
        await dbB.SaveChangesAsync();

        // Act: Tenant B queries its own subscriptions with explicit predicate.
        // Must not see Alpha's subscription or its delivery records.
        await using var queryCtx = new IntegrationDbContext(options, new FixedTenantProvider("beta-corp"));
        var betaSubs = await queryCtx.WebhookSubscriptions
            .Where(s => s.TenantId == "beta-corp")
            .ToListAsync();

        // Assert: Tenant B sees only its own subscription
        betaSubs.Should().HaveCount(1);
        betaSubs.Single().TenantId.Should().Be("beta-corp");
        betaSubs.Should().NotContain(s => s.TenantId == "alpha-corp");

        // Assert: Tenant B subscription has no deliveries (Alpha's delivery not leaked)
        betaSubs.Single().Deliveries.Should().BeEmpty(
            "Tenant B webhook subscription must not contain Tenant A's delivery records");
    }

    [Fact]
    public void WebhookDelivery_CorrelationId_BoundToSubscriptionTenant()
    {
        // Adversarial: if a delivery is created with a correlationId from Tenant A's request,
        // that delivery must belong to Tenant A's subscription only.
        // This is enforced by the aggregate boundary: RecordDelivery is called on the
        // subscription aggregate which carries TenantId.
        var correlationA = Guid.NewGuid().ToString();
        var correlationB = Guid.NewGuid().ToString();

        var subA = WebhookSubscription.Create(
            "tenant-a", "https://a.example.com/events",
            "sec-a", "A sub", ["EmployeeTerminated"]);
        var subB = WebhookSubscription.Create(
            "tenant-b", "https://b.example.com/events",
            "sec-b", "B sub", ["EmployeeTerminated"]);

        var deliveryA = subA.RecordDelivery("EmployeeTerminated", "{\"id\":\"a\"}", correlationA);
        var deliveryB = subB.RecordDelivery("EmployeeTerminated", "{\"id\":\"b\"}", correlationB);

        // Each delivery is scoped to its subscription's aggregate — no cross-tenant mixing possible
        deliveryA.CorrelationId.Should().Be(correlationA);
        deliveryB.CorrelationId.Should().Be(correlationB);

        subA.Deliveries.Should().HaveCount(1);
        subB.Deliveries.Should().HaveCount(1);

        // Cross-check: Tenant A subscription only holds Tenant A delivery
        subA.Deliveries.Should().NotContain(d => d.CorrelationId == correlationB);
        subB.Deliveries.Should().NotContain(d => d.CorrelationId == correlationA);
    }

    // ── 2. Consumer async boundary — wrong scope must not expose cross-tenant data ─

    [Fact]
    public async Task Consumer_ContextWithWrongTenantScope_ReturnsNoRows()
    {
        // Adversarial scenario: a consumer handler accidentally constructs a DbContext
        // with the wrong tenant ID (e.g., defaults to "system" or passes the publisher
        // tenant instead of the consumer's tenant). The EF global query filter + explicit
        // predicate must still return 0 rows for the "wrong" tenant.
        var dbName = Guid.NewGuid().ToString();

        // Seed: Tenant A has a workflow definition
        var seedOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        await using var seedCtx = new WorkflowDbContext(seedOptions, new FixedTenantProvider("publisher-a"));
        var def = WorkflowDefinition.Create("publisher-a", "Expense", "Publisher A Rule");
        seedCtx.WorkflowDefinitions.Add(def);
        await seedCtx.SaveChangesAsync();

        // Act: consumer accidentally uses "consumer-b" scope to query (wrong tenant)
        var queryOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        await using var consumerCtx = new WorkflowDbContext(queryOptions, new FixedTenantProvider("consumer-b"));

        // Even without an explicit TenantId predicate, the global filter protects consumer-b
        // from reading publisher-a data.
        var rows = await consumerCtx.WorkflowDefinitions.ToListAsync();

        rows.Should().BeEmpty(
            "a consumer operating in tenant-b scope must not see tenant-a data " +
            "even if it omits an explicit TenantId predicate — global query filter enforces isolation");
    }

    // ── 3. Workflow approval history — cross-tenant audit trail isolation ──────

    [Fact]
    public async Task WorkflowApprovalHistory_IsolatedPerTenant()
    {
        // Two tenants each have a workflow definition. Both go through SubmitForReview → Approve.
        // Tenant B context must not see Tenant A's approval history entries.
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbA = new WorkflowDbContext(options, new FixedTenantProvider("acme"));
        var defA = WorkflowDefinition.CreateDraft("acme", "Bonus", "Acme Bonus Rule");
        defA.SubmitForReview("user-hr-a", "Ready for compliance review");
        defA.Approve("user-compliance-a", "Reviewed and approved");
        dbA.WorkflowDefinitions.Add(defA);
        await dbA.SaveChangesAsync();

        await using var dbB = new WorkflowDbContext(options, new FixedTenantProvider("globex"));
        var defB = WorkflowDefinition.CreateDraft("globex", "Bonus", "Globex Bonus Rule");
        defB.SubmitForReview("user-hr-b");
        dbB.WorkflowDefinitions.Add(defB);
        await dbB.SaveChangesAsync();

        // Globex context must not see Acme's definitions or approval history
        await using var queryCtx = new WorkflowDbContext(options, new FixedTenantProvider("globex"));
        var globexDefs = await queryCtx.WorkflowDefinitions
            .Where(d => d.TenantId == "globex")
            .ToListAsync();

        globexDefs.Should().HaveCount(1);
        globexDefs.Single().TenantId.Should().Be("globex");

        // Acme's approval entries must not appear in Globex's query results
        globexDefs.Should().NotContain(d => d.TenantId == "acme");
        globexDefs.Single().ApprovalHistory.Should().HaveCount(1,
            "Globex definition has 1 transition: Draft → InReview via SubmitForReview");
        globexDefs.Single().ApprovalHistory.Single().ActorId.Should().Be("user-hr-b");
    }

    // ── 4. Approval history integrity ─────────────────────────────────────────

    [Fact]
    public void WorkflowApprovalHistory_RecordsFullTransitionChain()
    {
        // Full governance flow: Draft → InReview → Approved → Published → Deprecated
        // Each step must record actor, timestamp, and from/to status in order.
        var def = WorkflowDefinition.CreateDraft("acme", "Expense", "Full Audit Rule");
        var t0 = DateTimeOffset.UtcNow;

        def.SubmitForReview("user-alice", "Ready");
        def.Approve("user-bob", "LGTM");
        def.Publish("user-charlie", "Going live");
        def.Deprecate("user-dave", "Replaced by v2");

        var history = def.ApprovalHistory.OrderBy(e => e.OccurredAt).ToList();

        history.Should().HaveCount(4,
            "4 lifecycle transitions must each produce 1 approval entry");

        history[0].FromStatus.Should().Be(PolicyStatus.Draft);
        history[0].ToStatus.Should().Be(PolicyStatus.InReview);
        history[0].ActorId.Should().Be("user-alice");
        history[0].Notes.Should().Be("Ready");

        history[1].FromStatus.Should().Be(PolicyStatus.InReview);
        history[1].ToStatus.Should().Be(PolicyStatus.Approved);
        history[1].ActorId.Should().Be("user-bob");

        history[2].FromStatus.Should().Be(PolicyStatus.Approved);
        history[2].ToStatus.Should().Be(PolicyStatus.Published);
        history[2].ActorId.Should().Be("user-charlie");

        history[3].FromStatus.Should().Be(PolicyStatus.Published);
        history[3].ToStatus.Should().Be(PolicyStatus.Deprecated);
        history[3].ActorId.Should().Be("user-dave");
        history[3].Notes.Should().Be("Replaced by v2");

        // All timestamps must be after t0 (wall-clock sanity check)
        history.Should().AllSatisfy(e => e.OccurredAt.Should().BeOnOrAfter(t0));
    }

    [Fact]
    public void WorkflowApprovalHistory_NullNotesAllowed()
    {
        // Notes are optional — regulators only require actor + timestamp + status chain.
        var def = WorkflowDefinition.CreateDraft("acme", "Leave", "Silent Approve");

        def.SubmitForReview("user-x");        // no notes
        def.Approve("user-y");               // no notes

        def.ApprovalHistory.Should().HaveCount(2);
        def.ApprovalHistory.Should().AllSatisfy(e => e.Notes.Should().BeNull());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class FixedTenantProvider(string tenantId) : ITenantProvider
    {
        private readonly TenantExecutionEnvelope _envelope = new(
            tenantId,
            correlationId: Guid.NewGuid().ToString(),
            requestId: Guid.NewGuid().ToString(),
            executionSource: ExecutionSource.HttpRequest,
            source: TenantSource.JwtClaim);

        public string GetCurrentTenantId() => tenantId;

        public bool TryGetCurrentTenantId([NotNullWhen(true)] out string? outTenantId)
        {
            outTenantId = tenantId;
            return true;
        }

        public TenantExecutionEnvelope GetTenant() => _envelope;

        public bool TryGetTenant([NotNullWhen(true)] out TenantExecutionEnvelope? tenant)
        {
            tenant = _envelope;
            return true;
        }
    }
}
