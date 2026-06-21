// infrastructure/bicep/modules/sql.bicep
//
// HA/DR + security hardening (certification findings C-3 and H-3).
// Changes vs the original single-Standard-DB module:
//   * Zone-redundant General Purpose database (survives an AZ failure).
//   * Point-in-time + long-term retention backup policies (RPO/RTO targets).
//   * Microsoft Entra (AAD) administrator + AAD-only auth when an admin is supplied.
//   * Public network access disabled; optional private endpoint.
//   * Open 0.0.0.0 "all Azure IPs" firewall rule REMOVED.
//   * Optional auto-failover group to a paired-region secondary server.
// New params have safe defaults so existing callers keep compiling.

param serverName string
param databaseName string
param location string

// SQL auth retained for backward compatibility / break-glass only. When an Entra
// admin object id is supplied, AAD-only authentication is enforced.
param administratorLogin string
@secure()
param administratorLoginPassword string

@description('Entra (AAD) admin display name. Empty = AAD admin not configured.')
param aadAdminLogin string = ''
@description('Entra (AAD) admin object id (e.g. the deployment/app principal). Empty = SQL auth remains enabled.')
param aadAdminObjectId string = ''

@description('Database SKU. Default zone-redundant-capable General Purpose.')
param skuName string = 'GP_Gen5_2'
@description('Make the database zone-redundant (requires a zone-redundant-capable SKU/region).')
param zoneRedundant bool = true

@description('PITR short-term retention in days (7-35).')
@minValue(7)
@maxValue(35)
param pitrRetentionDays int = 14

@description('Resource id of the paired-region secondary SQL server for the failover group. Empty = no geo failover group.')
param secondaryServerId string = ''

@description('Subnet resource id for a private endpoint. Empty = no private endpoint created.')
param privateEndpointSubnetId string = ''

var aadConfigured = !empty(aadAdminObjectId)

resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: serverName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    // No public access: reach the server via private endpoint / VNet only.
    publicNetworkAccess: 'Disabled'
    minimalTlsVersion: '1.2'
    administrators: aadConfigured ? {
      administratorType: 'ActiveDirectory'
      principalType: 'Application'
      login: aadAdminLogin
      sid: aadAdminObjectId
      tenantId: tenant().tenantId
      azureADOnlyAuthentication: true
    } : null
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: skuName
  }
  properties: {
    zoneRedundant: zoneRedundant
    requestedBackupStorageRedundancy: 'Geo'
  }
}

// Transparent Data Encryption (explicit, service-managed key).
resource tde 'Microsoft.Sql/servers/databases/transparentDataEncryption@2023-05-01-preview' = {
  parent: sqlDatabase
  name: 'current'
  properties: {
    state: 'Enabled'
  }
}

// Point-in-time restore retention.
resource shortTermRetention 'Microsoft.Sql/servers/databases/backupShortTermRetentionPolicies@2023-05-01-preview' = {
  parent: sqlDatabase
  name: 'default'
  properties: {
    retentionDays: pitrRetentionDays
  }
}

// Long-term retention for regulatory (Tier 1) data.
resource longTermRetention 'Microsoft.Sql/servers/databases/backupLongTermRetentionPolicies@2023-05-01-preview' = {
  parent: sqlDatabase
  name: 'default'
  properties: {
    weeklyRetention: 'P10Y'
    monthlyRetention: 'P10Y'
    yearlyRetention: 'P10Y'
    weekOfYear: 1
  }
}

// Optional auto-failover group to a paired-region secondary server (geo RTO/RPO).
// The secondary server itself is provisioned by the paired-region deployment.
resource failoverGroup 'Microsoft.Sql/servers/failoverGroups@2023-05-01-preview' = if (!empty(secondaryServerId)) {
  parent: sqlServer
  name: '${serverName}-fog'
  properties: {
    readWriteEndpoint: {
      failoverPolicy: 'Automatic'
      failoverWithDataLossGracePeriodMinutes: 60
    }
    partnerServers: [
      {
        id: secondaryServerId
      }
    ]
    databases: [
      sqlDatabase.id
    ]
  }
}

// Optional private endpoint (no public exposure).
resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-09-01' = if (!empty(privateEndpointSubnetId)) {
  name: '${serverName}-pe'
  location: location
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${serverName}-plsc'
        properties: {
          privateLinkServiceId: sqlServer.id
          groupIds: [
            'sqlServer'
          ]
        }
      }
    ]
  }
}

// NOTE: the open "AllowAzureIps" 0.0.0.0 firewall rule has been removed.
// Access is via private endpoint only (publicNetworkAccess = Disabled).

output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseName string = sqlDatabase.name
output sqlServerPrincipalId string = sqlServer.identity.principalId
