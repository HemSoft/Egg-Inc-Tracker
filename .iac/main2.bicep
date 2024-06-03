resource sa_egg 'Microsoft.Storage/storageAccounts@2023-04-01' = {
  name: 'sa-egg-test'
  location: 'east-us'
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}
