# D1 Runtime Evidence: End-to-End Isolation Proof

## Evidence Package A: Outbox (Capture Proof)
Successfully verified that metadata is captured and signed synchronously before EF Outbox persistence.
**Source**: `EmployeeApiTests.OnboardEmployee_ShouldCreateEmployee_WhenPayloadIsValid`
```text
OutboxMessage Id: 04b2...
Headers: { 
  "MT-Tenant-Id": "acme", 
  "MT-Correlation-Id": "0HNLU...", 
  "MT-Context-Signature": "v9A/..." 
}
```

## Evidence Package B & C: RabbitMQ / Consumer (Wire Proof)
Captured raw headers at the consumer entry point, proving they survive RabbitMQ transport.
**Source**: `TenantPropagationRegressionTests.ConsumedMessage_ShouldRestoreContext_WhenSignatureIsValid`
```text
[D1 FORENSICS] [WIRE PROOF] Raw Message Headers: 
MT-Tenant-Id=globex, 
MT-Correlation-Id=8e06..., 
MT-Conversation-Id=cf64..., 
MT-Source-Service=testhost, 
MT-Context-Signature=h5min8UVIO1jVOgsR4Qcf2REn15WffFgOuj67Fm2PdM=
```

## Evidence Package D: Database (Isolation Proof)
Executed high-concurrency flow with a real SQL Server container to verify physical schema isolation.
**Source**: `EndToEndIsolationCertificationTests.MultiTenantAsyncFlow_MustMaintainStrictDatabaseIsolation`
**Scenario**: 3 tenants, 10 concurrent messages each (30 total).

| Schema | Row Count (PayrollProfiles) | Status |
| :--- | :--- | :--- |
| `tenant_acme` | 10 | ✅ PASS |
| `tenant_dev` | 10 | ✅ PASS |
| `tenant_contoso` | 10 | ✅ PASS |
| `dbo` | 0 | ✅ CERTIFIED ISOLATED |

## Evidence Package E: Tampering (Integrity Proof)
Verified that any modification to metadata triggers immediate rejection.
**Source**: `TenantPropagationRegressionTests.ConsumedMessage_ShouldReject_WhenHeadersAreTampered`

| Mutation | Expected Result | Actual Result |
| :--- | :--- | :--- |
| `TenantId` changed | REJECT / Fault | ✅ PASS |
| `CorrelationId` changed | REJECT / Fault | ✅ PASS |
| `ConversationId` changed | REJECT / Fault | ✅ PASS |
| `SourceService` changed | REJECT / Fault | ✅ PASS |

## Evidence Package F: Replay (Protection Proof)
Verified that the system prevents duplicate processing of the same message.
**Source**: `TenantPropagationRegressionTests.ReplayedMessage_ShouldBeRejected_OnSecondAttempt`
1. Attempt 1 (Message A): ✅ Succeeded (Consumer ran).
2. Attempt 2 (Message A): ❌ Rejected (Filter threw `MalformedTenantMessageException`).

## Evidence Package G: CI (Gating Proof)
Verified that the regression suite acts as a hard deployment blocker.
1. Introduced bug in `ExecutionContextSigner`: ❌ **Build/Test FAILED**.
2. Reverted bug: ✅ **Build/Test PASSED**.
This confirms that future regressions will be blocked by CI.
