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

var integrationResourceGroupName = toLower('pollstar-int-${environmentName}-${locationAbbreviation}')
var containerAppEnvironmentName = '${integrationResourceGroupName}-env'
var applicationInsightsResourceName = '${integrationResourceGroupName}-ai'
var webPubSubResourceName = '${integrationResourceGroupName}-pubsub'
var serviceBusResourceName = '${integrationResourceGroupName}-sb'

var apiResourceGroupName = toLower('${systemName}-${environmentName}-${locationAbbreviation}')

var storageAccountTables = [
  'votes'
]
var storageAccountQueues = [
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
    storageAccountQueues: storageAccountQueues
    containerVersion: containerVersion
    integrationResourceGroupName: integrationResourceGroupName
    containerAppEnvironmentResourceName: containerAppEnvironmentName
    applicationInsightsResourceName: applicationInsightsResourceName
    webPubSubResourceName: webPubSubResourceName
    serviceBusResourceName: serviceBusResourceName
    queues: queues
  }
}
