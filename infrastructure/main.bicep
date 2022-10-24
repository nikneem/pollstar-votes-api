targetScope = 'subscription'

param systemName string

@allowed([
  'dev'
  'test'
  'prod'
])
param environmentName string
param location string = deployment().location
param locationAbbreviation string
param containerVersion string
param queues array
param developersGroup string

var integrationResourceGroupName = toLower('pollstar-int-${environmentName}-${locationAbbreviation}')
var containerAppEnvironmentName = '${integrationResourceGroupName}-env'
var serviceBusResourceName = '${integrationResourceGroupName}-bus'
var azureAppConfigurationName = '${integrationResourceGroupName}-cfg'
var azureIntegrationKeyVaultName = '${integrationResourceGroupName}-kv'

var apiResourceGroupName = toLower('${systemName}-${environmentName}-${locationAbbreviation}')

var storageAccountTables = [
  'votes'
]

resource apiResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: apiResourceGroupName
  location: location
}

module resourcesModule 'resources.bicep' = {
  name: 'ResourceModule'
  scope: apiResourceGroup
  params: {
    defaultResourceName: apiResourceGroupName
    location: location
    storageAccountTables: storageAccountTables
    containerVersion: containerVersion
    integrationResourceGroupName: integrationResourceGroupName
    containerAppEnvironmentResourceName: containerAppEnvironmentName
    serviceBusResourceName: serviceBusResourceName
    queues: queues
    environmentName: environmentName
    azureAppConfigurationName: azureAppConfigurationName
    azureIntegrationKeyVaultName: azureIntegrationKeyVaultName
    developersGroup: developersGroup

  }
}
