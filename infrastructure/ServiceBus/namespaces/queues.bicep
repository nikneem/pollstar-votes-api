param queues array
param serviceBusName string
param location string = resourceGroup().location

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-01-01-preview' = {
  name: serviceBusName
  location: location
}

resource serviceBusQueue 'Microsoft.ServiceBus/namespaces/queues@2022-01-01-preview' = [for queue in queues: {
  name: queue
  parent: serviceBus
}]
