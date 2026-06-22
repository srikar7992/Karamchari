// infrastructure/bicep/main.bicep
//
// H-3: All modules hardened (see individual module headers).
// Key Vault RBAC role assignments added here (requires enableRbacAuthorization=true in keyvault.bicep).

@description('The environment name (e.g. dev, qa, staging, prod)')
param environmentName string

@description('The primary location for all resources')
param location string = resourceGroup().location

@description('The tenant ID for Key Vault')
param tenantId string

@description('The object ID of the principal executing the deployment (granted Key Vault Secrets Officer).')
param principalId string

@secure()
@description('The SQL Server Administrator Password (break-glass fallback; primary auth is Entra).')
param sqlAdministratorLoginPassword string

@description('ACR name (without .azurecr.io suffix).')
param acrName string = 'karamchari'

@description('Container image tag to deploy. Defaults to environmentName so each env carries its own image.')
param imageTag string = environmentName

// Service Bus SKU: Standard for non-prod (cheaper); Premium for prod (private endpoints, MI-only auth).
var serviceBusSku = environmentName == 'prod' ? 'Premium' : 'Standard'

var suffix = '${environmentName}-${uniqueString(resourceGroup().id)}'
var naming = {
  appServicePlan: 'asp-karamchari-${suffix}'
  webApp: 'app-karamchari-${suffix}'
  sqlServer: 'sql-karamchari-${suffix}'
  sqlDatabase: 'sqldb-karamchari'
  redis: 'redis-karamchari-${suffix}'
  keyVault: 'kv-karamchari-${substring(uniqueString(resourceGroup().id), 0, 6)}'
  storageAccount: 'stkaramchari${substring(uniqueString(resourceGroup().id), 0, 6)}'
  serviceBus: 'sb-karamchari-${suffix}'
}

// ── Built-in role definition IDs ───────────────────────────────────────────
// These are stable, subscription-scope constants.
var keyVaultSecretsUser = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '4633458b-17de-408a-b874-0445c86b69e4')

var keyVaultSecretsOfficer = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  'b86a8fe4-44ce-4948-aee5-eccb2c155cd7')

// 1. Key Vault
module keyVault 'modules/keyvault.bicep' = {
  name: 'keyVaultDeployment'
  params: {
    keyVaultName: naming.keyVault
    location: location
    tenantId: tenantId
  }
}

// 2. Storage Account
module storage 'modules/storage.bicep' = {
  name: 'storageDeployment'
  params: {
    storageAccountName: naming.storageAccount
    location: location
  }
}

// 3. SQL Server & Database
module sql 'modules/sql.bicep' = {
  name: 'sqlDeployment'
  params: {
    serverName: naming.sqlServer
    databaseName: naming.sqlDatabase
    location: location
    administratorLogin: 'karamchariadmin'
    administratorLoginPassword: sqlAdministratorLoginPassword
    // Entra-only auth: deployment principal becomes the SQL AAD admin (H-3).
    aadAdminLogin: 'karamchari-deploy-principal'
    aadAdminObjectId: principalId
  }
}

// 4. Redis Cache
module redis 'modules/redis.bicep' = {
  name: 'redisDeployment'
  params: {
    redisCacheName: naming.redis
    location: location
  }
}

// 5. Service Bus
module servicebus 'modules/servicebus.bicep' = {
  name: 'serviceBusDeployment'
  params: {
    serviceBusNamespaceName: naming.serviceBus
    location: location
    skuName: serviceBusSku
  }
}

// 6. App Service + staging slot
module appService 'modules/appservice.bicep' = {
  name: 'appServiceDeployment'
  params: {
    appServicePlanName: naming.appServicePlan
    webAppName: naming.webApp
    location: location
    keyVaultName: keyVault.outputs.keyVaultName
    acrName: acrName
    imageTag: imageTag
  }
}

// ── Key Vault RBAC role assignments ───────────────────────────────────────
// Deployment principal: Key Vault Secrets Officer (can set/read/delete secrets during deployment).
resource deployPrincipalKvRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.outputs.keyVaultId, principalId, keyVaultSecretsOfficer)
  scope: resourceGroup()
  properties: {
    roleDefinitionId: keyVaultSecretsOfficer
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}

// Web app (production slot) managed identity: Key Vault Secrets User (read-only at runtime).
resource webAppKvRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.outputs.keyVaultId, appService.outputs.webAppPrincipalId, keyVaultSecretsUser)
  scope: resourceGroup()
  properties: {
    roleDefinitionId: keyVaultSecretsUser
    principalId: appService.outputs.webAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Staging slot managed identity: also needs Secrets User so health checks pass before slot swap.
resource stagingSlotKvRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.outputs.keyVaultId, appService.outputs.stagingSlotPrincipalId, keyVaultSecretsUser)
  scope: resourceGroup()
  properties: {
    roleDefinitionId: keyVaultSecretsUser
    principalId: appService.outputs.stagingSlotPrincipalId
    principalType: 'ServicePrincipal'
  }
}
