param queues array
param serviceBusName string

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-01-01-preview' existing = {
  name: serviceBusName
}

resource serviceBusQueue 'Microsoft.ServiceBus/namespaces/queues@2022-01-01-preview' = [for queue in queues: {
  name: queue
  parent: serviceBus
}]

output serviceBusNamespace string = serviceBus.properties.serviceBusEndpoint
