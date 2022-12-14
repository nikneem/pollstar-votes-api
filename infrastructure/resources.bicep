param defaultResourceName string
param location string
param storageAccountTables array
param containerVersion string
param environmentName string
param integrationResourceGroupName string
param containerAppEnvironmentResourceName string
param serviceBusResourceName string
param queues array
param azureAppConfigurationName string
param developersGroup string

param containerPort int = 80
param containerAppName string = 'pollstar-votes-api'
param functionAppName string = 'pollstar-votes-func'

resource containerAppEnvironments 'Microsoft.App/managedEnvironments@2022-03-01' existing = {
  name: containerAppEnvironmentResourceName
  scope: resourceGroup(integrationResourceGroupName)
}
resource appConfiguration 'Microsoft.AppConfiguration/configurationStores@2022-05-01' existing = {
  name: azureAppConfigurationName
  scope: resourceGroup(integrationResourceGroupName)
}
resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-01-01-preview' existing = {
  name: serviceBusResourceName
  scope: resourceGroup(integrationResourceGroupName)
}
var serviceBusEndpoint = '${serviceBus.id}/AuthorizationRules/RootManageSharedAccessKey'
var serviceBusConnectionString = listKeys(serviceBusEndpoint, serviceBus.apiVersion).primaryConnectionString

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-09-01' = {
  name: uniqueString(defaultResourceName)
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}
resource storageAccountTableService 'Microsoft.Storage/storageAccounts/tableServices@2021-09-01' = {
  name: 'default'
  parent: storageAccount
}
resource storageAccountTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2021-09-01' = [for table in storageAccountTables: {
  name: table
  parent: storageAccountTableService
}]

module queuesModule 'ServiceBus/namespaces/queues.bicep' = {
  name: 'serviceBusQueuesModule'
  scope: resourceGroup(integrationResourceGroupName)
  params: {
    serviceBusName: serviceBusResourceName
    queues: queues
  }
}

resource apiContainerApp 'Microsoft.App/containerApps@2022-03-01' = {
  name: '${defaultResourceName}-aca'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppEnvironments.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: containerPort
        transport: 'auto'
        allowInsecure: false
        traffic: [
          {
            weight: 100
            latestRevision: true
          }
        ]
      }
      dapr: {
        enabled: true
        appPort: containerPort
        appId: containerAppName
      } }
    template: {
      containers: [
        {
          image: 'pollstarint${environmentName}neuacr.azurecr.io/${containerAppName}:${containerVersion}'
          name: containerAppName
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'Azure__StorageAccount'
              value: storageAccount.name
            }
            {
              name: 'AzureAppConfiguration'
              value: appConfiguration.properties.endpoint
            }
          ]

        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 6
        rules: [
          {
            name: 'http-rule'
            http: {
              metadata: {
                concurrentRequests: '30'
              }
            }
          }
        ]
      }
    }
  }
}

resource funcContainerApp 'Microsoft.App/containerApps@2022-03-01' = {
  name: '${defaultResourceName}-func'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppEnvironments.id
    configuration: {
      activeRevisionsMode: 'Single'
      secrets: [
        {
          name: 'storage-account-key'
          value: storageAccount.listKeys().keys[0].value
        }
        {
          name: 'azure-web-jobs-storage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'servicebus-connection-string'
          value: serviceBusConnectionString
        }
      ]
      dapr: {
        enabled: true
        appPort: containerPort
        appId: functionAppName
      }
    }
    template: {
      containers: [
        {
          image: 'pollstarint${environmentName}neuacr.azurecr.io/${functionAppName}:${containerVersion}'
          name: functionAppName
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'Azure__StorageAccount'
              value: storageAccount.name
            }
            {
              name: 'AzureAppConfiguration'
              value: appConfiguration.properties.endpoint
            }
            {
              name: 'AzureWebJobsStorage'
              secretRef: 'azure-web-jobs-storage'
            }
            {
              name: 'ServiceBusConnection__fullyQualifiedNamespace'
              value: 'pollstar-int-prod-neu-bus.servicebus.windows.net'
            }
            {
              name: 'FUNCTIONS_WORKER_RUNTIME'
              value: 'dotnet'
            }
          ]

        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 6
        rules: [
          {
            name: 'event-driven'
            custom: {
              type: 'azure-servicebus'
              metadata: {
                queueName: 'votes'
                messageCount: '20'
              }
              auth: [
                {
                  secretRef: 'servicebus-connection-string'
                  triggerParameter: 'connection'
                }
              ]
            }
          }
        ]
      }
    }
  }
}

module roleAssignmentsModule 'all-role-assignments.bicep' = {
  name: 'roleAssignmentsModule'
  params: {
    containerAppPrincipalId: apiContainerApp.identity.principalId
    developersGroup: developersGroup
    integrationResourceGroupName: integrationResourceGroupName
  }
}
module functionAppRoleAssignments 'all-role-assignments.bicep' = {
  name: 'functionAppRoleAssignmentsModule'
  dependsOn: [
    roleAssignmentsModule
  ]
  params: {
    containerAppPrincipalId: funcContainerApp.identity.principalId
    developersGroup: ''
    integrationResourceGroupName: integrationResourceGroupName
  }
}
