// ======================================================================================
// Deployment file for all the resources needed to deploy the Egg-Inc-Tracker architcture
// ======================================================================================
// This file will deploy the following resources:
// - Storage Account
// - Azure SQL Server
// - Azure SQL Database
// - Azure Key Vault
// - Azure App Service Plan
// - Azure App Service
// - Azure Application Insights
// - Azure Function App

// ===========
// Parameters:
// ===========
@minLength(3)
@maxLength(24)
@description('Name of the storage account')
param storageAccountName string

@description('Location of the storage account')
param location string = resourceGroup().location

@description('SKU of the storage account')
@allowed([
  'Standard_LRS'
  'Standard_GRS'
  'Standard_RAGRS'
  'Standard_ZRS'
])
param sku string = 'Standard_LRS'

@description('Support HTTPS traffic only')
param httpsTrafficOnly bool = true

// ==========
// Resources:
// ==========
module storageAccount 'modules/storage-account.bicep' = {
  name: storageAccountName
  params: {
    storageAccountName: 'saeggtest'
    location: location
    sku: sku
    httpsTrafficOnly: httpsTrafficOnly
  }
}

module sqlServer 'modules/sql-server.bicep' = {
  name: 'deploy-sql-server'
  params: {
    sqlServerName: 'sql-srv-egg-test'
    databaseName: 'sql-db-egg-test'
    adminUsername: 'sqladmin'
    adminPassword: 'x^^yoq1tXc&87AD34v'
    location: resourceGroup().location
  }
}

// ========
// Outputs:
// ========
output storageAccount object = storageAccount
