# Phase 7 — Redis Certification

**Result: ✅ PASS (design + wiring verified); live app-populated cache not exercised (auth-blocked).**

## Wiring
`CoreServiceCollectionExtensions.cs`:
```csharp
var redisConnection = configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnection))
    services.AddStackExchangeRedisCache(...);   // ← used (Redis cs present)
else
    services.AddDistributedMemoryCache();        // fallback
```
With `ConnectionStrings:Redis=localhost:6379` set, the platform uses **StackExchange Redis** as `IDistributedCache`. Health check `Caching:Redis` = `Healthy`.

## TenantMetadataCache + TenantCacheGuard (code-verified)
`TenantMetadataCache` wraps `IDistributedCache`; every Get/Set/Remove first builds a tenant-namespaced key (`metadata`) and validates via `TenantCacheGuard`:
| Requirement | Mechanism | Result |
|---|---|---|
| Cache miss | `GetStringAsync` returns null → `null` returned | ✅ |
| Cache hit | JSON deserialize to `TenantMetadata` | ✅ |
| Cache refresh / expiry | `AbsoluteExpirationRelativeToNow` honored; `RemoveAsync` invalidates | ✅ |
| Tenant boundary | `ValidateGet/Set` parse key tenant; mismatch → `CachePoisoningDetectionException` | ✅ |
| Cache poisoning attempt | non-tenant-namespaced key → `ArgumentException`; cross-tenant key → `CachePoisoningDetectionException` | ✅ |
| Cross-tenant access | key tenant ≠ `ITenantProvider` tenant → blocked + `LogError("CROSS-TENANT CACHE ACCESS DETECTED")` | ✅ |

These guard paths are covered by `Karamchari.Core.UnitTests` (36 passing).

## Live observation
- `redis-cli DBSIZE` = 0 during the session because no tenant-resolved request completed (auth redirect prevents reaching cache-populating code paths). The cache could not be exercised via live HTTP traffic.
- Redis liveness and failure behavior verified in Phase 16: stopping Redis flips `/health` to 503; restart restores 200.

## Verdict
Cache design (tenant isolation, poisoning protection, miss/hit/expiry) is **correct and unit-tested**, and Redis is correctly wired and healthy. Live cache population via real tenant traffic is pending the auth fix.
