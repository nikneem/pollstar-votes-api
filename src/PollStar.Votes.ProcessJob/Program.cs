using Azure.Messaging.ServiceBus;
using Azure;
using System.Diagnostics;
using System.Text;
using Azure.Data.Tables;
using Azure.Identity;
using Newtonsoft.Json;
using PollStar.Core.ExtensionMethods;
using PollStar.Votes.Abstractions.Commands;
using PollStar.Votes.Abstractions.DataTransferObjects;
using PollStar.Votes.Repositories.Entities;

const string sourceQueueName = "votes";
const string targetQueueName = "charts";

const string storageTableName = "votes";


async static Task Main()
{

    Console.WriteLine("Starting the process job");

    // var identity = new ChainedTokenCredential(
    //     new ManagedIdentityCredential(),
    //     new VisualStudioCredential(),
    //     new VisualStudioCodeCredential(),
    //     new AzureCliCredential());

    var serviceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnection");
    var storageAccountConnection = Environment.GetEnvironmentVariable("StorageAccountConnection");

    var serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
    var receiver = serviceBusClient.CreateReceiver(sourceQueueName);
    var sender = serviceBusClient.CreateSender(targetQueueName);

    Console.WriteLine("Receiving message from service bus");
    var receivedMessage = await receiver.ReceiveMessageAsync();

    if (receivedMessage != null)
    {
        Console.WriteLine("Got a message from the service bus");
        var payloadString = Encoding.UTF8.GetString(receivedMessage.Body);
        var payload = JsonConvert.DeserializeObject<CastVoteDto>(payloadString);
        if (payload != null)
        {
            Console.WriteLine("Deserialized to a descent payload");

            Activity.Current?.AddTag("PollId", payload.PollId.ToString());
            Activity.Current?.AddTag("UserId", payload.UserId.ToString());

            var voteEntity = new VoteTableEntity
            {
                PartitionKey = payload.PollId.ToString(),
                RowKey = payload.UserId.ToString(),
                OptionId = payload.OptionId.ToString(),
                Timestamp = DateTimeOffset.UtcNow,
                ETag = ETag.All
            };

            Console.WriteLine("Created entity instance");
            var client = new TableClient(storageAccountConnection, storageTableName);
            Console.WriteLine("Saving entity in table storage");
            await client.UpsertEntityAsync(voteEntity);

            var calcCommand = new ChartCalculationCommand
            {
                PollId = payload.PollId,
                SessionId = payload.SessionId
            }.ToServiceBusMessage();
            Console.WriteLine("Constructed service bus command");


            Console.WriteLine("Sending message to chart calculation queue");
            await sender.SendMessageAsync(calcCommand);
            Console.WriteLine("Completing original message in service bus");
            await receiver.CompleteMessageAsync(receivedMessage);
            Console.WriteLine("All good, process complete");
        }
        else
        {
            Console.WriteLine("No service bus message received, terminating container");
        }
    }
}

await Main();