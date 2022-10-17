param queues array
param serviceBusName string
param location string = resourceGroup().location
param principalId string
param allowSending bool = false
param allowReading bool = false

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-01-01-preview' = {
  name: serviceBusName
  location: location
}

resource serviceBusQueue 'Microsoft.ServiceBus/namespaces/queues@2022-01-01-preview' = [for queue in queues: {
  name: queue
  parent: serviceBus
}]

resource serviceBusSenderRole 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: resourceGroup()
  name: '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'
}
resource serviceBusReceiverRole 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: resourceGroup()
  name: '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0'
}

resource serviceBusSenderRolePermission 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (allowSending) {
  name: guid('${principalId}-${serviceBusSenderRole.id}')
  properties: {
    description: 'Configuration access for the development team'
    principalId: principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: serviceBusSenderRole.id
  }
}
resource serviceBusReceiverRolePermission 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (allowReading) {
  name: guid('${principalId}-${serviceBusReceiverRole.id}')
  properties: {
    description: 'Configuration access for the development team'
    principalId: principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: serviceBusReceiverRole.id
  }
}
