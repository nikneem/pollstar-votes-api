param defaultResourceName string
param location string
param storageAccountTables array
param storageAccountQueues array
param containerVersion string

param integrationResourceGroupName string
param containerAppEnvironmentResourceName string
param applicationInsightsResourceName string
param webPubSubResourceName string
param serviceBusResourceName string
param queues array

param containerPort int = 80
param containerAppName string = 'pollstar-votes-api'

resource queueContributorRole 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: resourceGroup()
  name: '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
}
resource tableDataContributorRole 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: resourceGroup()
  name: '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'
}

resource containerAppEnvironments 'Microsoft.App/managedEnvironments@2022-03-01' existing = {
  name: containerAppEnvironmentResourceName
  scope: resourceGroup(integrationResourceGroupName)
}
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: applicationInsightsResourceName
  scope: resourceGroup(integrationResourceGroupName)
}
resource webPubSub 'Microsoft.SignalRService/webPubSub@2021-10-01' existing = {
  name: webPubSubResourceName
  scope: resourceGroup(integrationResourceGroupName)
}

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

resource storageAccountQueueService 'Microsoft.Storage/storageAccounts/queueServices@2022-05-01' = {
  name: 'default'
  parent: storageAccount
}

resource storageAccountQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2021-09-01' = [for queue in storageAccountQueues: {
  name: queue
  parent: storageAccountQueueService
}]

module queuesModule 'ServiceBus/namespaces/queues.bicep' = {
  name: 'serviceBusQueuesModule'
  scope: resourceGroup(integrationResourceGroupName)
  params: {
    serviceBusName: serviceBusResourceName
    location: location
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
      secrets: [
        {
          name: 'application-insights-connectionstring'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'web-pubsub-connectionstring'
          value: webPubSub.listKeys().primaryConnectionString
        }
      ]
      ingress: {
        external: false
        targetPort: 80
        transport: 'auto'
        allowInsecure: false
        traffic: [
          {
            weight: 100
            label: containerVersion
            revisionName: containerVersion
          }
        ]
      }
      dapr: {
        enabled: true
        appPort: containerPort
        appId: containerAppName
      }
    }
    template: {
      containers: [
        {
          image: 'pollstarinttestneuacr.azurecr.io/${containerAppName}:${containerVersion}'
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
              name: 'Azure__WebPubSub'
              secretRef: 'web-pubsub-connectionstring'
            }
            {
              name: 'Azure__PollStarHub'
              value: 'pollstar'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              secretRef: 'application-insights-connectionstring'
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

resource allowQueueContribution 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('${apiContainerApp.name}-${queueContributorRole.id}')
  properties: {
    description: 'Configuration access for the development team'
    principalId: apiContainerApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: queueContributorRole.id
  }
}
resource allowTableStorageContribution 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('${apiContainerApp.name}-${tableDataContributorRole.id}')
  properties: {
    description: 'Configuration access for the development team'
    principalId: apiContainerApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: tableDataContributorRole.id
  }
}
