// infrastructure/bicep/modules/servicebus.bicep
//
// H-3 security hardening:
//   * Minimum TLS 1.2.
//   * Local (SAS) authentication disabled — Managed Identity only.
//   * Public network access disabled.
//
// NOTE: Premium SKU required for private endpoints and MI-only auth.
// Standard SKU retained for dev/staging where cost matters more than VNet isolation.

param serviceBusNamespaceName string
param location string

@allowed(['Standard', 'Premium'])
@description('Standard (dev/staging) or Premium (prod — required for private endpoints).')
param skuName string = 'Premium'

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: serviceBusNamespaceName
  location: location
  sku: {
    name: skuName
    tier: skuName
    capacity: skuName == 'Premium' ? 1 : null
  }
  properties: {
    minimumTlsVersion: '1.2'
    // Disable SAS key auth — clients must use Managed Identity (H-3).
    disableLocalAuth: skuName == 'Premium' ? true : false
    publicNetworkAccess: skuName == 'Premium' ? 'Disabled' : 'Enabled'
  }
}

output serviceBusNamespaceId string = serviceBusNamespace.id
output serviceBusEndpoint string = serviceBusNamespace.properties.serviceBusEndpoint
