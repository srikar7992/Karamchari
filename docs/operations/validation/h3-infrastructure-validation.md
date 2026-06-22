# H-3 Infrastructure Validation

**Date:** 2026-06-23
**Tester:** srikar7992
**Method:** IaC code review (Bicep) — all controls implemented in code, verified against H-3 requirements
**Status:** VALIDATED — all H-3 controls present in IaC; live Azure proof pending deployment

---

## Scope

H-3 requires: Managed Identity authentication, private endpoints, public access disabled, failover group, zone redundancy, backup policies.

All controls are implemented in `infrastructure/bicep/`. Evidence is the Bicep code itself — each claim below maps to a specific resource or property.

---

## 1. Managed Identity Authentication

| Control | Implementation | File |
|---------|---------------|------|
| SQL Server: system-assigned identity | `identity: { type: 'SystemAssigned' }` on `sqlServer` resource | `modules/sql.bicep:46-51` |
| SQL Server: Entra-only auth | `azureADOnlyAuthentication: true` when `aadAdminObjectId` supplied | `modules/sql.bicep:58-66` |
| App Service: system-assigned identity | `identity: { type: 'SystemAssigned' }` on `webApp` and `stagingSlot` | `modules/appservice.bicep` |
| Key Vault: RBAC authorization (no SAS/legacy) | `enableRbacAuthorization: true` | `modules/keyvault.bicep` |
| Key Vault: app identity granted Secrets User | `Microsoft.Authorization/roleAssignments` in main.bicep | `main.bicep` |
| Key Vault: deployment principal granted Secrets Officer | `Microsoft.Authorization/roleAssignments` in main.bicep | `main.bicep` |
| Service Bus: local auth disabled (MI-only) | `disableLocalAuth: true` (Premium SKU) | `modules/servicebus.bicep` |

**Verdict: IMPLEMENTED**

---

## 2. Private Endpoints / Public Access Disabled

| Control | Implementation | File |
|---------|---------------|------|
| SQL Server: public network access disabled | `publicNetworkAccess: 'Disabled'` | `modules/sql.bicep:55` |
| SQL Server: open firewall rule removed | Comment: "AllowAzureIps 0.0.0.0 removed" | `modules/sql.bicep:155` |
| SQL Server: optional private endpoint | `privateEndpoint` resource (conditional on `privateEndpointSubnetId`) | `modules/sql.bicep:134-153` |
| Key Vault: public network access disabled | `publicNetworkAccess: 'Disabled'`, `networkAcls.defaultAction: 'Deny'` | `modules/keyvault.bicep` |
| Service Bus: public network access disabled | `publicNetworkAccess: 'Disabled'` (Premium SKU) | `modules/servicebus.bicep` |
| App Service: HTTPS only | `httpsOnly: true` on web app and staging slot | `modules/appservice.bicep` |
| App Service: FTPS disabled | `ftpsState: 'Disabled'` | `modules/appservice.bicep` |
| App Service: min TLS 1.2 | `minTlsVersion: '1.2'` | `modules/appservice.bicep` |
| Redis: non-SSL port disabled | `enableNonSslPort: false` | `modules/redis.bicep` |
| Redis: min TLS 1.2 | `minimumTlsVersion: '1.2'` | `modules/redis.bicep` |

**Verdict: IMPLEMENTED**

---

## 3. Failover Group

| Control | Implementation | File |
|---------|---------------|------|
| Auto-failover group to secondary server | `failoverGroup` resource (conditional on `secondaryServerId`) | `modules/sql.bicep:114-131` |
| Failover policy | `Automatic` with 60-minute grace period | `modules/sql.bicep:118-121` |

**Verdict: IMPLEMENTED** (conditional — activated by passing `secondaryServerId` param at deploy time)

---

## 4. Zone Redundancy

| Control | Implementation | File |
|---------|---------------|------|
| Database zone-redundant | `zoneRedundant: true` (default) | `modules/sql.bicep:77` |
| Geo-redundant backup storage | `requestedBackupStorageRedundancy: 'Geo'` | `modules/sql.bicep:78` |

**Verdict: IMPLEMENTED**

---

## 5. Backup Policies

| Control | Implementation | File |
|---------|---------------|------|
| Transparent Data Encryption | `tde` resource, `state: 'Enabled'` | `modules/sql.bicep:83-89` |
| PITR retention: 14 days | `backupShortTermRetentionPolicies`, `retentionDays: 14` | `modules/sql.bicep:92-98` |
| LTR weekly retention: 10 years | `backupLongTermRetentionPolicies`, `weeklyRetention: 'P10Y'` | `modules/sql.bicep:101-110` |
| LTR monthly retention: 10 years | `monthlyRetention: 'P10Y'` | `modules/sql.bicep:108` |
| LTR yearly retention: 10 years | `yearlyRetention: 'P10Y'` | `modules/sql.bicep:109` |

**Verdict: IMPLEMENTED**

---

## 6. Key Vault Hardening

| Control | Implementation | File |
|---------|---------------|------|
| Soft delete enabled | `enableSoftDelete: true` | `modules/keyvault.bicep` |
| Soft delete retention | `softDeleteRetentionInDays: 90` | `modules/keyvault.bicep` |
| Purge protection | `enablePurgeProtection: true` | `modules/keyvault.bicep` |

**Verdict: IMPLEMENTED**

---

## Gap: Live Azure Deployment Proof

All controls are implemented in IaC. H-3 is fully CERTIFIED at the code level.

Live Azure deployment proof (portal screenshots, CLI output, resource IDs) requires:
1. Set repo variable `AZURE_DEPLOY_ENABLED=true`
2. Configure OIDC federation + secrets referenced in `deploy-api.yml`
3. Create GitHub Environments: dev, staging, prod
4. Trigger deployment via push to main or `workflow_dispatch`

This is blocked by C-2 (deployment infrastructure setup) — once C-2 is done, H-3 live evidence is captured automatically as part of the deployment run.

---

## H-3 Verdict

**CERTIFIED at IaC level — live proof pending C-2 execution**

All 6 H-3 control categories implemented. No control gaps. Deployment will close the live-proof gap.
