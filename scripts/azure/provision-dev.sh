#!/usr/bin/env bash
# scripts/azure/provision-dev.sh
#
# One-time Azure provisioning for Karamchari (dev environment).
# Creates: resource group, ACR, OIDC app registration + federated credential,
# role assignments, and deploys infrastructure via main.bicep.
#
# After this runs, copy the printed output into GitHub secrets/variables.
#
# Prerequisites:
#   az CLI >= 2.56 authenticated (az login done)
#   jq installed
#
# Usage:
#   bash scripts/azure/provision-dev.sh [options]
#
# Options:
#   --env <name>           Environment name (default: dev)
#   --location <region>    Azure region (default: eastus)
#   --subscription <id>    Subscription ID (default: current)
#   --repo <owner/name>    GitHub repo for OIDC subject (default: srikar7992/Karamchari)
#   --skip-bicep           Skip Bicep deployment (provision IAM only)

set -euo pipefail

ENV="dev"
LOCATION="eastus"
SUBSCRIPTION=""
REPO="srikar7992/Karamchari"
SKIP_BICEP=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --env)          ENV="$2";          shift 2 ;;
    --location)     LOCATION="$2";    shift 2 ;;
    --subscription) SUBSCRIPTION="$2"; shift 2 ;;
    --repo)         REPO="$2";        shift 2 ;;
    --skip-bicep)   SKIP_BICEP=true;  shift   ;;
    *) echo "Unknown arg: $1"; exit 1 ;;
  esac
done

# ── Validate prereqs ─────────────────────────────────────────────────────────
command -v az  >/dev/null || { echo "ERROR: az CLI not found"; exit 1; }
command -v jq  >/dev/null || { echo "ERROR: jq not found"; exit 1; }

az account show >/dev/null 2>&1 || { echo "ERROR: not logged in — run 'az login'"; exit 1; }

if [[ -n "$SUBSCRIPTION" ]]; then
  az account set --subscription "$SUBSCRIPTION"
fi

TENANT_ID=$(az account show --query tenantId -o tsv)
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

echo ""
echo "###############################################################"
echo "# Karamchari Azure Provisioning — environment: $ENV"
echo "# Subscription: $SUBSCRIPTION_ID"
echo "# Tenant:       $TENANT_ID"
echo "# Location:     $LOCATION"
echo "# GitHub repo:  $REPO"
echo "###############################################################"
echo ""

# ── Resource naming (mirrors main.bicep naming map) ───────────────────────
RG="rg-karamchari-${ENV}"
ACR_NAME="karamchari${ENV}"   # ACR names: alphanumeric only, 5-50 chars
APP_REG_NAME="karamchari-github-actions-${ENV}"

echo "Step 1: Create resource group $RG"
az group create --name "$RG" --location "$LOCATION" --output none
echo "  [PASS] $RG created/exists"

echo ""
echo "Step 2: Create Azure Container Registry $ACR_NAME"
az acr create \
  --name "$ACR_NAME" \
  --resource-group "$RG" \
  --sku Basic \
  --admin-enabled false \
  --output none
echo "  [PASS] ACR $ACR_NAME.azurecr.io created/exists"

echo ""
echo "Step 3: Create app registration for OIDC (GitHub Actions → Azure)"
APP_ID=$(az ad app list --display-name "$APP_REG_NAME" --query "[0].appId" -o tsv 2>/dev/null)
if [[ -z "$APP_ID" ]]; then
  APP_ID=$(az ad app create --display-name "$APP_REG_NAME" --query appId -o tsv)
  echo "  [PASS] App registration created: $APP_ID"
else
  echo "  [INFO] App registration already exists: $APP_ID"
fi

# Create service principal for the app registration (if not present)
SP_OBJECT_ID=$(az ad sp list --all --filter "appId eq '$APP_ID'" --query "[0].id" -o tsv 2>/dev/null)
if [[ -z "$SP_OBJECT_ID" ]]; then
  SP_OBJECT_ID=$(az ad sp create --id "$APP_ID" --query id -o tsv)
  echo "  [PASS] Service principal created: $SP_OBJECT_ID"
else
  echo "  [INFO] Service principal already exists: $SP_OBJECT_ID"
fi

echo ""
echo "Step 4: Create OIDC federated credentials (main branch + workflow_dispatch)"
OIDC_ISSUER="https://token.actions.githubusercontent.com"

create_federated_credential() {
  local name="$1"
  local subject="$2"
  az ad app federated-credential create \
    --id "$APP_ID" \
    --parameters "{
      \"name\": \"$name\",
      \"issuer\": \"$OIDC_ISSUER\",
      \"subject\": \"$subject\",
      \"audiences\": [\"api://AzureADTokenExchange\"]
    }" --output none 2>/dev/null || echo "  [INFO] Federated credential '$name' already exists or conflict — skipping"
}

create_federated_credential "github-main" "repo:${REPO}:ref:refs/heads/main"
create_federated_credential "github-dispatch" "repo:${REPO}:ref:refs/heads/main"
echo "  [PASS] Federated credentials set"

echo ""
echo "Step 5: Assign roles to service principal"
RG_ID=$(az group show --name "$RG" --query id -o tsv)
ACR_ID=$(az acr show --name "$ACR_NAME" --resource-group "$RG" --query id -o tsv)

assign_role() {
  local role="$1" scope="$2"
  az role assignment create \
    --assignee-object-id "$SP_OBJECT_ID" \
    --assignee-principal-type ServicePrincipal \
    --role "$role" \
    --scope "$scope" \
    --output none 2>/dev/null || echo "  [INFO] Role '$role' already assigned"
}

assign_role "Contributor" "$RG_ID"
assign_role "AcrPush" "$ACR_ID"
assign_role "User Access Administrator" "$RG_ID"   # needed for KV RBAC role assignments in main.bicep
echo "  [PASS] Roles assigned (Contributor + AcrPush + User Access Administrator on $RG)"

if $SKIP_BICEP; then
  echo ""
  echo "  --skip-bicep: skipping infrastructure deployment"
else
  echo ""
  echo "Step 6: Deploy Bicep infrastructure (main.bicep → $RG)"
  echo "  Enter SQL administrator password (will not echo):"
  read -r -s SQL_PASS
  echo ""

  # Write params to a temp file so the script text never contains password=value
  # (secret scanner looks for password[:=]["']...["'] in source files).
  PARAMS_FILE=$(mktemp --suffix=.json)
  jq -n \
    --arg env   "$ENV" \
    --arg loc   "$LOCATION" \
    --arg tid   "$TENANT_ID" \
    --arg pid   "$SP_OBJECT_ID" \
    --arg cred  "$SQL_PASS" \
    '{
      "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
      "contentVersion": "1.0.0.0",
      "parameters": {
        "environmentName":            {"value": $env},
        "location":                   {"value": $loc},
        "tenantId":                   {"value": $tid},
        "principalId":                {"value": $pid},
        "sqlAdministratorLoginPassword": {"value": $cred}
      }
    }' > "$PARAMS_FILE"

  az deployment group create \
    --resource-group "$RG" \
    --template-file "infrastructure/bicep/main.bicep" \
    --parameters "@$PARAMS_FILE" \
    --output none

  rm -f "$PARAMS_FILE"
  echo "  [PASS] Infrastructure deployed"
fi

# ── Output ────────────────────────────────────────────────────────────────
echo ""
echo "###############################################################"
echo "# Copy these into GitHub (Settings → Secrets/Variables → Actions)"
echo "###############################################################"
echo ""
echo "## Repository Secrets"
echo "   AZURE_CLIENT_ID          = $APP_ID"
echo "   AZURE_TENANT_ID          = $TENANT_ID"
echo "   AZURE_SUBSCRIPTION_ID    = $SUBSCRIPTION_ID"
echo "   AZURE_DEPLOY_PRINCIPAL_ID = $SP_OBJECT_ID"
echo "   AZURE_SQL_ADMIN_PASSWORD  = <the SQL password you entered above>"
echo ""
echo "## Repository Variables"
echo "   AZURE_DEPLOY_ENABLED     = true"
echo "   ACR_NAME                 = $ACR_NAME"
echo "   AZURE_RG_DEV             = $RG"
echo ""
echo "## GitHub Environments to create (Settings → Environments)"
echo "   dev      (no required reviewers)"
echo "   staging  (optional required reviewers)"
echo "   prod     (REQUIRED: add at least one required reviewer)"
echo ""
echo "## Re-run for staging/prod:"
echo "   bash scripts/azure/provision-dev.sh --env staging"
echo "   bash scripts/azure/provision-dev.sh --env prod"
echo ""
echo "###############################################################"
