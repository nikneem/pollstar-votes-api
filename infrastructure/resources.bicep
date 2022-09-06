param defaultResourceName string
param location string
param storageAccountTables array
param containerVersion string

param integrationResourceGroupName string
param containerAppEnvironmentResourceName string
param applicationInsightsResourceName string
param webPubSubResourceName string

param containerPort int = 80
param containerAppName string = 'pollstar-votes-api'

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
          name: 'storage-account-secret'
          value: listKeys(storageAccount.id, storageAccount.apiVersion).keys[0].value
        }
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
            latestRevision: true
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
              name: 'Azure__StorageKey'
              secretRef: 'storage-account-secret'
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
        minReplicas: 0
        maxReplicas: 6
      }
    }
  }
}
