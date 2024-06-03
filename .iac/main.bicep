resource storageAccount 'Microsoft.Storage/storageAccounts@2023-04-01' = {
  name: 'saeggtest'
  location: 'eastus'
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}
