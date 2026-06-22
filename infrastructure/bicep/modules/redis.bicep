// infrastructure/bicep/modules/redis.bicep
//
// H-3 security hardening:
//   * Minimum TLS 1.2.
//   * Non-SSL port 6379 disabled (TLS-only on 6380).

param redisCacheName string
param location string

resource redisCache 'Microsoft.Cache/redis@2023-08-01' = {
  name: redisCacheName
  location: location
  properties: {
    sku: {
      name: 'Standard'
      family: 'C'
      capacity: 1
    }
    minimumTlsVersion: '1.2'
    enableNonSslPort: false
  }
}

output redisHostName string = redisCache.properties.hostName
output redisPrimaryKey string = redisCache.listKeys().primaryKey
