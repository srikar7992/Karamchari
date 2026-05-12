// infrastructure/bicep/main.bicep

@description('The environment name (e.g. dev, qa, staging, prod)')
param environmentName string

@description('The primary location for all resources')
param location string = resourceGroup().location

@description('The tenant ID for Key Vault access policies')
param tenantId string

@description('The object ID of the principal executing the deployment')
param principalId string

@secure()
@description('The SQL Server Administrator Password')
param sqlAdministratorLoginPassword string

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

// 1. Key Vault
module keyVault 'modules/keyvault.bicep' = {
  name: 'keyVaultDeployment'
  params: {
    keyVaultName: naming.keyVault
    location: location
    tenantId: tenantId
    principalId: principalId
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

// 5. Service Bus (Messaging)
module servicebus 'modules/servicebus.bicep' = {
  name: 'serviceBusDeployment'
  params: {
    serviceBusNamespaceName: naming.serviceBus
    location: location
  }
}

// 6. App Service
module appService 'modules/appservice.bicep' = {
  name: 'appServiceDeployment'
  params: {
    appServicePlanName: naming.appServicePlan
    webAppName: naming.webApp
    location: location
    keyVaultName: keyVault.outputs.keyVaultName
  }
}
