// infrastructure/bicep/modules/keyvault.bicep
//
// H-3 security hardening (certification finding H-3):
//   * RBAC authorization replaces legacy access policies (unified IAM model).
//   * Soft delete with 90-day retention (regulatory requirement).
//   * Purge protection enabled (prevents accidental or malicious destruction).
//   * Public network access disabled (private endpoint / VNet only).
//   * Role assignments are the caller's responsibility (see main.bicep).

param keyVaultName string
param location string
param tenantId string

@description('Enable public network access. Default disabled (private endpoint only).')
param publicNetworkAccess string = 'Disabled'

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenantId
    // Modern RBAC model — no legacy access policies.
    enableRbacAuthorization: true
    accessPolicies: []
    // Soft delete + purge protection: protects against accidental/malicious destruction.
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: true
    // No public exposure: reach the vault via private endpoint / VNet only.
    publicNetworkAccess: publicNetworkAccess
    networkAcls: publicNetworkAccess == 'Disabled' ? {
      defaultAction: 'Deny'
      bypass: 'AzureServices'
    } : null
  }
}

output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output keyVaultId string = keyVault.id
