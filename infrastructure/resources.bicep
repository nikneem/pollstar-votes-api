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

resource configurationDataReaderRole 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: resourceGroup()
  name: '516239f1-63e1-4d78-a4de-a74fb236a071'
}
resource storageAccountDataContributorRole 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: resourceGroup()
  name: '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'
}

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
    principalId: apiContainerApp.identity.principalId
    allowSending: true
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

module configurationReaderRoleAssignment 'roleAssignment.bicep' = {
  name: 'configurationReaderRoleAssignmentModule'
  scope: resourceGroup(integrationResourceGroupName)
  params: {
    principalId: apiContainerApp.identity.principalId
    roleDefinitionId: configurationDataReaderRole.id
  }
}
module storageAccountDataReaderRoleAssignment 'roleAssignment.bicep' = {
  name: 'storageAccountDataReaderRoleAssignmentModule'
  scope: resourceGroup()
  params: {
    principalId: apiContainerApp.identity.principalId
    roleDefinitionId: storageAccountDataContributorRole.id
  }
}
module storageAccountDataReaderRoleAssignmentForDevelopers 'roleAssignment.bicep' = {
  name: 'storageAccountDataReaderRoleAssignmentForDevelopersModule'
  scope: resourceGroup()
  params: {
    principalId: developersGroup
    roleDefinitionId: storageAccountDataContributorRole.id
    principalType: 'Group'
  }
}

resource accessSecretsRole 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: resourceGroup()
  name: '4633458b-17de-408a-b874-0445c86b69e6'
}
module keyVaultSecretsAccessRoleAssignment 'roleAssignment.bicep' = {
  name: 'keyVaultSecretsAccessRoleAssignmentModule'
  scope: resourceGroup()
  params: {
    principalId: apiContainerApp.identity.principalId
    roleDefinitionId: accessSecretsRole.id
  }
}
