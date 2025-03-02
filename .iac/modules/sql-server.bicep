// ======================================================================================
// Module: Create a SQL Server
// Author: Franz Hemmer
// Version: 1.0
// ======================================================================================
@description('The name of the SQL Server')
param sqlServerName string

@description('The location of the SQL Server')
param location string = resourceGroup().location

@description('The administrator username for the SQL Server')
param adminUsername string

@description('The administrator password for the SQL Server')
@secure()
param adminPassword string

@description('The name of the SQL Database')
param databaseName string

@description('The SKU for the SQL Database')
param skuName string = 'Basic'

@description('The compute size for the SQL Database')
param skuTier string = 'Basic'

@description('The backup storage redundancy for the SQL Database')
param backupStorageRedundancy string = 'Local'

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: adminUsername
    administratorLoginPassword: adminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2 * 1024 * 1024 * 1024  // Default max size is 250 GB
    requestedBackupStorageRedundancy: backupStorageRedundancy
  }
  sku: {
    name: skuName
    tier: skuTier
  }
}

output sqlServerName string = sqlServer.name
output sqlServerFQDN string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseName string = sqlDatabase.name
