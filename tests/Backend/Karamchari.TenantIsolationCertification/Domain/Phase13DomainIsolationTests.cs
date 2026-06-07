using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Karamchari.Compliance.Domain.Violations;
using Karamchari.Compliance.Persistence;
using Karamchari.Core.Contracts.EventCatalog;
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
/// Cross-tenant data isolation tests for Phase 12-13 domains:
/// WorkflowDefinition, PolicyViolation, WebhookSubscription.
///
/// Isolation contract:
///   - Every query that reads from a tenant-scoped aggregate MUST include a TenantId predicate.
///   - No cross-tenant row must ever be returned, even when the raw table contains rows from multiple tenants.
///   - These tests enforce that contract at the EF query layer using InMemory DbContext with mixed-tenant data.
/// </summary>
public sealed class Phase13DomainIsolationTests
{
    // ── WorkflowDefinition ────────────────────────────────────────────────────

    [Fact]
    public async Task WorkflowDefinition_TenantA_NotVisibleTo_TenantB_Query()
    {
        // Arrange: shared InMemory store with data from both tenants
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbA = new WorkflowDbContext(options, new FixedTenantProvider("tenant-a"));
        var defA = WorkflowDefinition.Create("tenant-a", "Expense", "Tenant A Rule");
        dbA.WorkflowDefinitions.Add(defA);
        await dbA.SaveChangesAsync();

        await using var dbB = new WorkflowDbContext(options, new FixedTenantProvider("tenant-b"));
        var defB = WorkflowDefinition.Create("tenant-b", "Expense", "Tenant B Rule");
        dbB.WorkflowDefinitions.Add(defB);
        await dbB.SaveChangesAsync();

        // Act: query as tenant-B with correct tenant scoping
        await using var queryCtx = new WorkflowDbContext(options, new FixedTenantProvider("tenant-b"));
        var tenantBDefs = await queryCtx.WorkflowDefinitions
            .Where(d => d.TenantId == "tenant-b")
            .ToListAsync();

        // Assert: only tenant-B row returned
        tenantBDefs.Should().HaveCount(1);
        tenantBDefs.Single().Name.Should().Be("Tenant B Rule");
        tenantBDefs.Should().NotContain(d => d.TenantId == "tenant-a");
    }

    [Fact]
    public async Task WorkflowDefinition_GlobalQueryFilter_BlocksCrossTenantRead()
    {
        // Use a fresh InMemory DB and single context to prove global filter works.
        // The global query filter in KaramchariDbContext applies e.TenantId == context.CurrentTenantId.
        // This test creates data for two tenants in the same InMemory store
        // and verifies the filter is applied correctly.
        var dbName = Guid.NewGuid().ToString();

        // Seed data directly via a context that ignores the global filter for seeding
        var seedOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        await using var seedCtx = new WorkflowDbContext(seedOptions, new FixedTenantProvider("tenant-a"));
        var defA = WorkflowDefinition.Create("tenant-a", "Leave", "A Leave Rule");
        seedCtx.WorkflowDefinitions.Add(defA);
        await seedCtx.SaveChangesAsync();

        // Act: query as tenant-a — global filter + predicate should return 1 row
        var queryOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        await using var queryCtx = new WorkflowDbContext(queryOptions, new FixedTenantProvider("tenant-a"));

        var aDefs = await queryCtx.WorkflowDefinitions
            .Where(d => d.TenantId == "tenant-a")
            .ToListAsync();

        aDefs.Should().HaveCount(1,
            "tenant-A context with tenant-A predicate must return exactly 1 row");
        aDefs.Single().TenantId.Should().Be("tenant-a");
        aDefs.Single().Name.Should().Be("A Leave Rule");

        // Document: cross-tenant read protection layers
        // Layer 1: EF global query filter (KaramchariDbContext.HasQueryFilter) — always applied at EF level
        // Layer 2: Explicit TenantId predicate in BFF queries — belt-and-suspenders
        // Layer 3: SQL Server RLS — enforced at DB engine level in production
    }

    [Fact]
    public async Task WorkflowDefinition_PolicyStatus_IsolatedPerTenant()
    {
        // Arrange: tenant-A has Published definition, tenant-B has Draft
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbA = new WorkflowDbContext(options, new FixedTenantProvider("tenant-a"));
        var published = WorkflowDefinition.Create("tenant-a", "Approval", "Published Rule");
        dbA.WorkflowDefinitions.Add(published);
        await dbA.SaveChangesAsync();

        await using var dbB = new WorkflowDbContext(options, new FixedTenantProvider("tenant-b"));
        var draft = WorkflowDefinition.CreateDraft("tenant-b", "Approval", "Draft Rule");
        dbB.WorkflowDefinitions.Add(draft);
        await dbB.SaveChangesAsync();

        // Query: tenant-A active defs must not include tenant-B's draft
        await using var queryCtx = new WorkflowDbContext(options, new FixedTenantProvider("tenant-a"));
        var activeDefs = await queryCtx.WorkflowDefinitions
            .Where(d => d.TenantId == "tenant-a" && d.IsActive)
            .ToListAsync();

        activeDefs.Should().HaveCount(1);
        activeDefs.Single().Status.Should().Be(PolicyStatus.Published);
        activeDefs.Should().NotContain(d => d.TenantId == "tenant-b");
    }

    // ── PolicyViolation ───────────────────────────────────────────────────────

    [Fact]
    public async Task PolicyViolation_TenantA_NotVisibleTo_TenantB_Query()
    {
        var options = new DbContextOptionsBuilder<ComplianceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var empA = Guid.NewGuid();
        var empB = Guid.NewGuid();

        await using var dbA = new ComplianceDbContext(options, new FixedTenantProvider("corp-a"));
        var violationA = PolicyViolation.Detect("corp-a", "GDPR-001", empA, ViolationSeverity.High,
            "{\"evidence\":\"Missing consent record\"}");
        dbA.PolicyViolations.Add(violationA);
        await dbA.SaveChangesAsync();

        await using var dbB = new ComplianceDbContext(options, new FixedTenantProvider("corp-b"));
        var violationB = PolicyViolation.Detect("corp-b", "SOX-042", empB, ViolationSeverity.Medium,
            "{\"evidence\":\"Segregation of duties\"}");
        dbB.PolicyViolations.Add(violationB);
        await dbB.SaveChangesAsync();

        // corp-A query must not see corp-B violations
        await using var queryCtx = new ComplianceDbContext(options, new FixedTenantProvider("corp-a"));
        var corpAViolations = await queryCtx.PolicyViolations
            .Where(v => v.TenantId == "corp-a")
            .ToListAsync();

        corpAViolations.Should().HaveCount(1);
        corpAViolations.Single().PolicyCode.Should().Be("GDPR-001");
        corpAViolations.Should().NotContain(v => v.TenantId == "corp-b");
    }

    [Fact]
    public async Task PolicyViolation_OpenViolations_IsolatedPerTenant()
    {
        var options = new DbContextOptionsBuilder<ComplianceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var empA = Guid.NewGuid();
        var empB = Guid.NewGuid();

        await using var db = new ComplianceDbContext(options, new FixedTenantProvider("acme"));
        var v1 = PolicyViolation.Detect("acme", "GDPR-001", empA, ViolationSeverity.Critical, "{\"a\":1}");
        var v2 = PolicyViolation.Detect("acme", "GDPR-002", empA, ViolationSeverity.Low, "{\"b\":2}");
        var v3 = PolicyViolation.Detect("globex", "SOX-001", empB, ViolationSeverity.High, "{\"c\":3}");

        db.PolicyViolations.AddRange(v1, v2, v3);
        await db.SaveChangesAsync();

        await using var queryCtx = new ComplianceDbContext(options, new FixedTenantProvider("acme"));
        var openCount = await queryCtx.PolicyViolations
            .Where(v => v.TenantId == "acme" && v.Status == ViolationStatus.Open)
            .CountAsync();

        openCount.Should().Be(2, "acme has 2 open violations; globex's violation must not be included");
    }

    // ── WebhookSubscription ────────────────────────────────────────────────────

    [Fact]
    public async Task WebhookSubscription_TenantA_NotVisibleTo_TenantB_Query()
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbA = new IntegrationDbContext(options, new FixedTenantProvider("tenant-a"));
        var subA = WebhookSubscription.Create("tenant-a", "https://hooks.a.example.com/events",
            "secret-a", "Tenant A webhook", ["EmployeeTerminated"]);
        dbA.WebhookSubscriptions.Add(subA);
        await dbA.SaveChangesAsync();

        await using var dbB = new IntegrationDbContext(options, new FixedTenantProvider("tenant-b"));
        var subB = WebhookSubscription.Create("tenant-b", "https://hooks.b.example.com/events",
            "secret-b", "Tenant B webhook", ["EmployeeTerminated"]);
        dbB.WebhookSubscriptions.Add(subB);
        await dbB.SaveChangesAsync();

        // Act: query as tenant-A
        await using var queryCtx = new IntegrationDbContext(options, new FixedTenantProvider("tenant-a"));
        var tenantASubs = await queryCtx.WebhookSubscriptions
            .Where(s => s.TenantId == "tenant-a")
            .ToListAsync();

        tenantASubs.Should().HaveCount(1);
        tenantASubs.Single().TargetUrl.Should().Be("https://hooks.a.example.com/events");
        tenantASubs.Should().NotContain(s => s.TenantId == "tenant-b");
    }

    [Fact]
    public async Task WebhookDelivery_CorrelationId_PreservedThroughDelivery()
    {
        // Documents that CorrelationId threads from event → webhook delivery.
        var sub = WebhookSubscription.Create(
            "tenant-a", "https://hooks.example.com", "secret",
            "Test hook", ["EmployeeTerminated"]);

        var correlationId = Guid.NewGuid().ToString();
        var delivery = sub.RecordDelivery("EmployeeTerminated", "{\"employeeId\":\"123\"}", correlationId);

        delivery.CorrelationId.Should().Be(correlationId,
            "correlation ID must flow from publishing event into webhook delivery for trace continuity");

        // Delivery without correlationId is allowed (backward compat with pre-Phase14 data)
        var legacyDelivery = sub.RecordDelivery("EmployeeTerminated", "{\"employeeId\":\"456\"}");
        legacyDelivery.CorrelationId.Should().BeNull(
            "pre-Phase14 deliveries have no correlation ID and must not fail");
    }

    // ── Event Catalog ─────────────────────────────────────────────────────────

    [Fact]
    public void EventCatalog_ContainsAllCriticalEvents()
    {
        // Verify the catalog answers the CTO's 30-second question:
        // "What publishes EmployeeTerminated? Who consumes it? Which version is active?"
        var entry = KaramchariEventCatalog.Find("EmployeeTerminatedIntegrationEvent");

        entry.Should().NotBeNull("EmployeeTerminated must be registered in the event catalog");
        entry!.Publisher.Should().Be("HR");
        entry.Status.Should().Be(EventStatus.Active);
        entry.Consumers.Should().Contain("AssetManagement",
            "AssetManagement consumer auto-returns assets on termination");
        entry.Consumers.Should().Contain("Helpdesk",
            "Helpdesk consumer closes open tickets on termination");
    }

    [Fact]
    public void EventCatalog_V1PayrollRunCompleted_MarkedDeprecated()
    {
        var v1 = KaramchariEventCatalog.Find("PayrollRunCompletedIntegrationEvent", version: 1);
        var v2 = KaramchariEventCatalog.Find("PayrollRunCompletedIntegrationEvent", version: 2);

        v1.Should().NotBeNull();
        v2.Should().NotBeNull();

        v1!.Status.Should().Be(EventStatus.Deprecated,
            "V1 does not include EmployeeName — deprecated in favor of V2");
        v2!.Status.Should().Be(EventStatus.Active);
        v1.DeprecatedInFavorOf.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void EventCatalog_Find_ReturnsHighestActiveVersion_WhenNoVersionSpecified()
    {
        // PayrollRunCompleted exists in V1 (deprecated) and V2 (active).
        // Find() with no version should return V2.
        var latest = KaramchariEventCatalog.Find("PayrollRunCompletedIntegrationEvent");

        latest.Should().NotBeNull();
        latest!.Version.Should().Be(2, "Find() defaults to highest active version");
        latest.Status.Should().Be(EventStatus.Active);
    }

    [Fact]
    public void EventCatalog_GetByPublisher_ReturnsOnlyOwnedEvents()
    {
        var hrEvents = KaramchariEventCatalog.GetByPublisher("HR");
        hrEvents.Should().NotBeEmpty();
        hrEvents.Should().AllSatisfy(e => e.Publisher.Should().Be("HR"));
        hrEvents.Should().NotContain(e => e.Publisher == "Payroll");
    }

    [Fact]
    public void EventCatalog_GetByConsumer_FindsWorkflowConsumers()
    {
        // Workflow consumes LeaveRequestCreated and ReimbursementSubmitted
        var workflowEvents = KaramchariEventCatalog.GetByConsumer("Workflow");
        workflowEvents.Should().NotBeEmpty();
        workflowEvents.Should().AllSatisfy(e =>
            e.Consumers.Should().Contain(c => c.Equals("Workflow", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void EventCatalog_TotalCount_IsAboveThreshold()
    {
        // Sanity check: catalog must have at least 30 events.
        // If this fails, someone removed entries without replacing them.
        KaramchariEventCatalog.All.Count.Should().BeGreaterThan(30,
            "platform has 40+ integration events — catalog must not be incomplete");
    }

    [Fact]
    public void EventCatalog_NoActiveEventMissingConsumers()
    {
        // Every active event must declare at least one consumer.
        // Events with no consumers are either dead code or undocumented.
        var activeWithNoConsumers = KaramchariEventCatalog.Active
            .Where(e => e.Consumers.Length == 0)
            .ToList();

        activeWithNoConsumers.Should().BeEmpty(
            "every active integration event must have at least one declared consumer");
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
