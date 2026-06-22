// infrastructure/bicep/modules/appservice.bicep
//
// H-3 security hardening (certification finding H-3):
//   * HTTPS only enforced (no plaintext HTTP).
//   * Minimum TLS 1.2.
//   * FTPS disabled (no legacy plaintext FTP).
//   * Staging deployment slot created (required for blue-green slot swap in deploy-api.yml).
//   * ACR image parameterized (was hardcoded placeholder).
//   * System-assigned managed identity output for Key Vault RBAC grant in caller.
//   * Always-on enabled (PremiumV3 SKU supports it; prevents cold-start).

param appServicePlanName string
param webAppName string
param location string
param keyVaultName string

@description('ACR name (without .azurecr.io suffix). E.g. karamchari')
param acrName string = 'karamchari'

@description('Container image tag to deploy.')
param imageTag string = 'latest'

var imageName = '${acrName}.azurecr.io/karamchari-api:${imageTag}'

resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'P1v3'
    tier: 'PremiumV3'
  }
  properties: {
    reserved: true // Linux
  }
}

resource webApp 'Microsoft.Web/sites@2022-09-01' = {
  name: webAppName
  location: location
  // Enforce HTTPS at the platform level before any request reaches the app.
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOCKER|${imageName}'
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      alwaysOn: true
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'KeyVaultName'
          value: keyVaultName
        }
        // Connection strings and other secrets are loaded via Key Vault references at runtime.
        // The managed identity is granted Key Vault Secrets User in main.bicep.
      ]
    }
  }
  identity: {
    type: 'SystemAssigned'
  }
}

// Staging slot for blue-green deployments (deploy-api.yml slot swap).
resource stagingSlot 'Microsoft.Web/sites/slots@2022-09-01' = {
  parent: webApp
  name: 'staging'
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOCKER|${imageName}'
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      alwaysOn: true
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'KeyVaultName'
          value: keyVaultName
        }
      ]
    }
  }
  identity: {
    type: 'SystemAssigned'
  }
}

output webAppName string = webApp.name
output webAppPrincipalId string = webApp.identity.principalId
output stagingSlotPrincipalId string = stagingSlot.identity.principalId
output webAppDefaultHostName string = webApp.properties.defaultHostName
