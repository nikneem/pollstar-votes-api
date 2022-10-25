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
    location: location
    queues: queues
  }
}

resource userAssigned 'Microsoft.ManagedIdentity/userAssignedIdentities@2022-01-31-preview' = {
  name: '${defaultResourceName}-uaid'
  location: location
}

resource apiContainerApp 'Microsoft.App/containerApps@2022-03-01' = {
  name: '${defaultResourceName}-aca'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: userAssigned
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
    type: 'UserAssigned'
    userAssignedIdentities: userAssigned
  }
  properties: {
    managedEnvironmentId: containerAppEnvironments.id
    configuration: {
      activeRevisionsMode: 'Single'
      dapr: {
        enabled: true
        appPort: containerPort
        appId: containerAppName
      }
    }
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

module roleAssignmentsModule 'all-role-assignments.bicep' = {
  name: 'roleAssignmentsModule'
  params: {
    containerAppPrincipalId: userAssigned.properties.principalId
    developersGroup: developersGroup
    integrationResourceGroupName: integrationResourceGroupName
  }
}
