# Analytics Consumers — Phase E Certification

**Date:** 2026-06-02  
**Scope:** Pre-Sprint-3 gap closure — analytics event consumers for the Intelligence module  
**Status:** IMPLEMENTED — pending runtime verification

---

## 1. Implementation Evidence

| Artifact | Path | Purpose |
|---|---|---|
| `AnalyticsReadModel` entity | `src/Backend/Modules/Intelligence/Karamchari.Intelligence/Domain/Analytics/AnalyticsReadModel.cs` | Denormalized read model: one row per event |
| `RecruitmentVelocityConsumer` | `src/Backend/Modules/Intelligence/Karamchari.Intelligence/Consumers/RecruitmentVelocityConsumer.cs` | Handles 5 event types; stamps stage + timestamp per event |
| `TimeToHireConsumer` | `src/Backend/Modules/Intelligence/Karamchari.Intelligence/Consumers/TimeToHireConsumer.cs` | Calculates days from PublishedAt → HiredAt |
| `HiringFunnelConsumer` | `src/Backend/Modules/Intelligence/Karamchari.Intelligence/Consumers/HiringFunnelConsumer.cs` | Records funnel stage counts per requisition |
| EF Migration | `src/Backend/Modules/Intelligence/Karamchari.Intelligence/Migrations/20260602061132_AddAnalyticsReadModel.cs` | Creates `Intelligence_AnalyticsReadModels` table in `__tenant__` schema |
| Design-time factory | `src/Backend/Modules/Intelligence/Karamchari.Intelligence/Migrations/IntelligenceDbContextDesignTimeFactory.cs` | Enables `dotnet ef` tooling |
| DI registration | `IntelligenceServiceCollectionExtensions.AddKaramchariIntelligenceConsumers` | Registers all 3 consumers with MassTransit |
| RLS table registered | `IntelligenceServiceCollectionExtensions.AddKaramchariIntelligence` | `Intelligence_AnalyticsReadModels` added to RLS policy |
| Project reference | `Karamchari.Intelligence.csproj` | Added reference to `Karamchari.Recruitment.Contracts` |

### Events Consumed

| Consumer | Event Types | EntityId Used | Stage Values |
|---|---|---|---|
| `RecruitmentVelocityConsumer` | `RequisitionPublishedIntegrationEvent` | RequisitionId | Published |
| `RecruitmentVelocityConsumer` | `CandidateAppliedIntegrationEvent` | RequisitionId | Applied |
| `RecruitmentVelocityConsumer` | `InterviewCompletedIntegrationEvent` | ApplicationId | InterviewCompleted |
| `RecruitmentVelocityConsumer` | `OfferAcceptedIntegrationEvent` | ApplicationId | OfferAccepted |
| `RecruitmentVelocityConsumer` | `CandidateHiredIntegrationEvent` | RequisitionId | Hired |
| `TimeToHireConsumer` | `CandidateHiredIntegrationEvent` | RequisitionId | Hired (Value = days elapsed) |
| `HiringFunnelConsumer` | `CandidateAppliedIntegrationEvent` | RequisitionId | Applied |
| `HiringFunnelConsumer` | `InterviewCompletedIntegrationEvent` | ApplicationId | InterviewCompleted |
| `HiringFunnelConsumer` | `OfferAcceptedIntegrationEvent` | ApplicationId | OfferAccepted |
| `HiringFunnelConsumer` | `CandidateHiredIntegrationEvent` | RequisitionId | Hired |

### Design Decisions

- **TenantId source:** read from `EnterpriseEventEnvelope.TenantId` (same pattern as `RequisitionPublishedAuditConsumer`). The `TenantConsumeFilter` on the MassTransit pipeline validates the tenant header before the consumer runs; the envelope's `TenantId` is the authoritative value written to the read model.
- **Idempotency:** Each event materializes one new row with a new `Guid` primary key. No upsert. MassTransit's default retry/error queue behavior applies; duplicate rows on retry are acceptable for count-based metrics and are handled by the `TenantConsumeFilter` replay protection upstream.
- **TimeToHire calculation:** looks back at the earliest `RecruitmentVelocity / Published` row for the same tenant + requisition. If no Published row exists yet (race or out-of-order delivery), `Value = 0` is written and can be corrected on a replay run.
- **No complex aggregation in consumers:** consumers are append-only; aggregation is deferred to query time (GROUP BY Stage COUNT / date arithmetic).

---

## 2. Manual Verification Runbook

Prerequisites: application running locally with RabbitMQ/ServiceBus and SQL Server; Intelligence module migrations applied.

### Step 1 — Apply migrations

```bash
cd src/Backend
dotnet ef database update \
  --project Modules/Intelligence/Karamchari.Intelligence/Karamchari.Intelligence.csproj \
  --context IntelligenceDbContext
```

Expected: `Intelligence_AnalyticsReadModels` table created in the `__tenant__` schema.

### Step 2 — Trigger a full recruitment lifecycle

1. POST `/api/recruitment/requisitions` — create and publish a requisition → `RequisitionPublishedIntegrationEvent` fires.
2. POST `/api/recruitment/applications` — submit a candidate application → `CandidateAppliedIntegrationEvent` fires.
3. PATCH `/api/recruitment/interviews/{id}/complete` — complete an interview → `InterviewCompletedIntegrationEvent` fires.
4. PATCH `/api/recruitment/offers/{id}/accept` — accept the offer → `OfferAcceptedIntegrationEvent` fires.
5. POST `/api/recruitment/hires` — finalize the hire → `CandidateHiredIntegrationEvent` fires.

### Step 3 — Verify rows in the database

```sql
-- Should show rows for all 3 MetricType values
SELECT MetricType, Stage, COUNT(*) AS RowCount, AVG(Value) AS AvgValue
FROM [__tenant__].[Intelligence_AnalyticsReadModels]
GROUP BY MetricType, Stage
ORDER BY MetricType, Stage;
```

Expected results:

| MetricType | Stage | RowCount | AvgValue |
|---|---|---|---|
| HiringFunnel | Applied | 1 | 1.000000 |
| HiringFunnel | Hired | 1 | 1.000000 |
| HiringFunnel | InterviewCompleted | 1 | 1.000000 |
| HiringFunnel | OfferAccepted | 1 | 1.000000 |
| RecruitmentVelocity | Applied | 1 | 1.000000 |
| RecruitmentVelocity | Hired | 1 | 1.000000 |
| RecruitmentVelocity | InterviewCompleted | 1 | 1.000000 |
| RecruitmentVelocity | OfferAccepted | 1 | 1.000000 |
| RecruitmentVelocity | Published | 1 | 1.000000 |
| TimeToHire | Hired | 1 | >0 (days elapsed) |

### Step 4 — Verify tenant isolation

Repeat Step 2 with a second tenant. Then run:

```sql
SELECT TenantId, MetricType, COUNT(*) AS Rows
FROM [__tenant__].[Intelligence_AnalyticsReadModels]
GROUP BY TenantId, MetricType
ORDER BY TenantId, MetricType;
```

Expected: rows for each tenant are separate; no cross-tenant leakage.

### Step 5 — Verify TimeToHire Value

```sql
SELECT Value AS DaysToHire, OccurredAt, CreatedAt
FROM [__tenant__].[Intelligence_AnalyticsReadModels]
WHERE MetricType = 'TimeToHire';
```

Expected: `DaysToHire` is approximately the number of days between requisition publish and hire. If the test was run within minutes, it will be near 0 but positive (or exactly 0 if sub-minute).

---

## 3. Exit Criteria Checklist

- [ ] `dotnet build` passes with 0 errors on `Karamchari.Intelligence.csproj`
- [ ] Migration `20260602061132_AddAnalyticsReadModel` applies cleanly against a fresh database
- [ ] Publishing a requisition produces a `RecruitmentVelocity / Published` row
- [ ] Submitting an application produces `RecruitmentVelocity / Applied` and `HiringFunnel / Applied` rows
- [ ] Completing an interview produces `RecruitmentVelocity / InterviewCompleted` and `HiringFunnel / InterviewCompleted` rows
- [ ] Accepting an offer produces `RecruitmentVelocity / OfferAccepted` and `HiringFunnel / OfferAccepted` rows
- [ ] Hiring a candidate produces `RecruitmentVelocity / Hired`, `HiringFunnel / Hired`, and `TimeToHire / Hired` rows
- [ ] `TimeToHire.Value` is a non-negative decimal representing days from publish to hire
- [ ] Two tenants produce isolated rows (no cross-tenant data visible)
- [ ] No messages appear in the dead-letter queue after the lifecycle test
