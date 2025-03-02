# azure cli

# Build bicep file into ARM template:
az bicep build --file .\main.bicep

# Test/Validate the template
Test-AzTemplate -TemplatePath .\main.json

# create resource group
az group create `
  --name rg-egg-test `
  --location northcentralus `
  --subscription "Visual Studio Premium with MSDN"

# build bicep file
az bicep build --file main.bicep

# deploy bicep template
az deployment group create `
  --resource-group rg-egg-test `
  --name deployment-egg `
  --template-file main.bicep `
  --parameters main.parameters.json

# rollback deployment
az deployment group delete `
  --resource-group rg-egg-test `
  --subscription "Visual Studio Premium with MSDN" `
  --name "deployment-egg"

# list resource groups
az group list --output table

# delete resource group
az group delete `
  --name rg-egg-test `
  --subscription "Visual Studio Premium with MSDN" `
  --yes
