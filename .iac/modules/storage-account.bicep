// ===========
// Parameters:
// ===========
@minLength(3)
@maxLength(24)
@description('Name of the storage account')
param storageAccountName string

@description('Location of the storage account')
@allowed([
  'North Central US'
  'East US'
])
param location string

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
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-04-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: sku
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: httpsTrafficOnly
  }
}

// =============
// == Outputs ==
// =============
output storageAccount object = storageAccount
