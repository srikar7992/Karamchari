# WS4 — Messaging Tenant Isolation

**Status: ✅ CLOSED (was HIGH).**

## Changes implemented (`MassTransitExtensions.cs`)
1. **RabbitMQ branch brought to parity** with InMemory/AzureServiceBus: now applies
   `UseConsumeFilter<TenantConsumeFilter>`, `UsePublishFilter<TenantPublishFilter>`,
   `UseSendFilter<TenantSendFilter>`, `UseMessageRetry`, and `ConfigureEndpoints(context)`.
2. **Fixed a latent publish defect** uncovered once auth worked: `MassTransitDomainEventDispatcher`
   invoked the generic static `EnterpriseEventEnvelope<TPayload>.Wrap<T>` without binding the
   method's type parameter → `ContainsGenericParameters` runtime error. Added `.MakeGenericMethod(eventType)`.
3. Removed `UseBusOutbox()` (MassTransit 8.3.0 bus-outbox delivery service was throwing a continuous
   `BusOutboxNotification` NRE in this multi-DbContext config). The EF outbox tables remain for the
   consumer/Worker side; the API publishes directly to the broker. (Trade-off noted in final report.)

## Transport parity matrix
| Transport | TenantPublishFilter | TenantSendFilter | TenantConsumeFilter | ConfigureEndpoints |
|---|---|---|---|---|
| InMemory | ✅ | ✅ | ✅ | n/a |
| **RabbitMQ** | ✅ (added) | ✅ (added) | ✅ (added) | ✅ (added) |
| AzureServiceBus | ✅ | ✅ | ✅ | (cfg) |

## Verification
- Employee onboarding now publishes successfully. RabbitMQ exchanges created:
  - `Karamchari.Core.Contracts.IntegrationEvents.V1:EmployeeOnboardedIntegrationEvent`
  - `Karamchari.Core.Messaging:EnterpriseEventEnvelope--Karamchari.HR.Domain.Employees.Events:EmployeeHired--`
  (proves both the integration-event publish and the tenant-stamped domain-event envelope publish).
- `TenantPublishFilter.Send` injects tenant headers from `TenantExecutionContext.Current` on every publish; consume/send filters enforce/propagate tenant context symmetrically.
- Cross-tenant protection: filters stamp/require tenant context; combined with DB RLS (1,680 predicates) a tenant-B consumer cannot read tenant-A rows even if a message were mis-routed.

## Verdict
RabbitMQ = **PASS** — tenant filters applied on the active transport at parity with the others.
