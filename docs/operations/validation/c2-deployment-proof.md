# C-2 Deployment Proof

**Status:** PIPELINE READY — live execution pending Azure target provisioning
**Pipeline file:** `.github/workflows/deploy-api.yml`
**Date updated:** 2026-06-23

---

## What Is Already Implemented

The deployment pipeline is **real** (not mocked). It implements:

| Step | Implementation |
|------|---------------|
| Docker image build | `docker build -f src/Backend/Hosts/Karamchari.Api/Dockerfile` |
| OIDC login to Azure (no stored credentials) | `azure/login@v2` with `id-token: write` |
| Image push to ACR | `az acr login` + `docker push` |
| Bicep infrastructure deploy | `azure/arm-deploy@v2` → `infrastructure/bicep/main.bicep` |
| Blue-green slot swap | Deploy to `staging` slot → health check → `az webapp deployment slot swap` |
| Health verification (pre-swap) | 12 retries × 10s for `/health/live` to return 200 |
| Automatic rollback | `if: failure()` → swap production back to staging |
| Post-swap health check | Second 12-retry loop after go-live |
| Environment gates | `dev` → `staging` → `prod` (prod requires manual reviewer approval) |

---

## Activation Checklist

To activate the pipeline, complete the following in GitHub:

### Repository Secrets (Settings → Secrets → Actions)

| Secret | Value |
|--------|-------|
| `AZURE_CLIENT_ID` | App registration client ID (for OIDC federated credential) |
| `AZURE_TENANT_ID` | Azure AD tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Target Azure subscription ID |

### Repository Variables (Settings → Variables → Actions)

| Variable | Value |
|----------|-------|
| `AZURE_DEPLOY_ENABLED` | `true` (gates all deploy jobs; build-only when unset) |
| `ACR_NAME` | ACR name without `.azurecr.io` suffix |
| `AZURE_RG_DEV` | Resource group for dev (default: `rg-karamchari-dev`) |
| `AZURE_APP_DEV` | App Service name for dev |
| `AZURE_RG_STAGING` | Resource group for staging |
| `AZURE_APP_STAGING` | App Service name for staging |
| `AZURE_RG_PROD` | Resource group for prod |
| `AZURE_APP_PROD` | App Service name for prod |

### GitHub Environments (Settings → Environments)

Create: `dev`, `staging`, `prod`. Add required reviewers to `prod`.

### OIDC Federated Credential

In the app registration, add a federated credential:
- **Issuer:** `https://token.actions.githubusercontent.com`
- **Subject:** `repo:srikar7992/Karamchari:ref:refs/heads/main`
- **Audience:** `api://AzureADTokenExchange`

### Infrastructure Pre-provision

Run Bicep deployment for each environment (first-time only):

```bash
az deployment group create \
  --resource-group rg-karamchari-dev \
  --template-file infrastructure/bicep/main.bicep \
  --parameters environmentName=dev tenantId=<TENANT_ID> principalId=<SP_OBJECT_ID> sqlAdministratorLoginPassword=<PASSWORD>
```

---

## Evidence Template (fill after first live run)

| Item | Expected | Actual | Result |
|------|----------|--------|--------|
| GitHub Actions run URL | — | _FILL IN_ | — |
| Build + push duration | < 5 min | _FILL_ | PASS/FAIL |
| Bicep deploy (dev) | Clean, no errors | _FILL_ | PASS/FAIL |
| Staging slot health check (dev) | 200 within 12 retries | _FILL_ | PASS/FAIL |
| Slot swap (dev) | Completes | _FILL_ | PASS/FAIL |
| Post-swap health check (dev) | 200 | _FILL_ | PASS/FAIL |
| Staging environment deploys after dev | Completes | _FILL_ | PASS/FAIL |
| Prod environment (after reviewer approval) | Completes | _FILL_ | PASS/FAIL |

### Rollback Test (run after first successful prod deploy)

Manually inject a failing health check (e.g. temporarily break `/health/live`) and re-run. Verify:
- Pipeline detects 502/000 in post-swap health check
- Automatic swap-back fires (`Rollback on failure` step)
- Production slot returns to previous working state

| Item | Result |
|------|--------|
| Rollback triggered | PASS/FAIL |
| Production slot restored | PASS/FAIL |
| RTO (time to rollback) | _FILL_ seconds |

---

## C-2 Verdict

**PIPELINE CERTIFIED at code level.** Live execution (and this doc's evidence section) completes once Azure target is provisioned and `AZURE_DEPLOY_ENABLED=true` is set.
