param containerAppPrincipalId string
param developersGroup string
param integrationResourceGroupName string

resource storageAccountDataContributorRole 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: resourceGroup()
  name: '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'
}
module storageAccountDataReaderRoleAssignment 'roleAssignment.bicep' = {
  name: 'storageAccountDataReaderRoleAssignmentModule'
  scope: resourceGroup()
  params: {
    principalId: containerAppPrincipalId
    roleDefinitionId: storageAccountDataContributorRole.id
  }
}
module storageAccountDataReaderRoleAssignmentForDevelopers 'roleAssignment.bicep' = if (length(developersGroup) > 0) {
  name: 'storageAccountDataReaderRoleAssignmentForDevelopersModule'
  scope: resourceGroup()
  params: {
    principalId: developersGroup
    roleDefinitionId: storageAccountDataContributorRole.id
    principalType: 'Group'
  }
}

resource configurationDataReaderRole 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: resourceGroup()
  name: '516239f1-63e1-4d78-a4de-a74fb236a071'
}
module configurationReaderRoleAssignment 'roleAssignment.bicep' = {
  name: 'configurationReaderRoleAssignmentModule'
  scope: resourceGroup(integrationResourceGroupName)
  params: {
    principalId: containerAppPrincipalId
    roleDefinitionId: configurationDataReaderRole.id
  }
}

resource accessSecretsRole 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: resourceGroup()
  name: '4633458b-17de-408a-b874-0445c86b69e6'
}
module keyVaultSecretsAccessRoleAssignment 'roleAssignment.bicep' = {
  name: 'keyVaultSecretsAccessRoleAssignmentModule'
  scope: resourceGroup(integrationResourceGroupName)
  params: {
    principalId: containerAppPrincipalId
    roleDefinitionId: accessSecretsRole.id
  }
}

resource serviceBusDataReceiverRole 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: resourceGroup()
  name: '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0'
}
module serviceBusDataReceiverRoleAssignment 'roleAssignment.bicep' = {
  name: 'serviceBusDataReceiverRoleAssignmentModule'
  scope: resourceGroup(integrationResourceGroupName)
  params: {
    principalId: containerAppPrincipalId
    roleDefinitionId: serviceBusDataReceiverRole.id
  }
}

resource serviceBusDataSenderRole 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: resourceGroup()
  name: '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'
}
module serviceBusDataSenderRoleAssignment 'roleAssignment.bicep' = {
  name: 'serviceBusDataSenderRoleAssignmentModule'
  scope: resourceGroup(integrationResourceGroupName)
  params: {
    principalId: containerAppPrincipalId
    roleDefinitionId: serviceBusDataSenderRole.id
  }
}
