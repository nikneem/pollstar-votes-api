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

const string storageAccountname = "charts";
const string storageTableName= "votes";


async Task Main()
{
    var identity = new ChainedTokenCredential(
        new ManagedIdentityCredential(),
        new VisualStudioCredential(),
        new VisualStudioCodeCredential(),
        new AzureCliCredential());

    var serviceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnection");

    var serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
    var receiver = serviceBusClient.CreateReceiver(sourceQueueName);
    var sender = serviceBusClient.CreateSender(targetQueueName);

    var receivedMessage = await receiver.ReceiveMessageAsync();

    if (receivedMessage != null)
    {
        var payloadString = Encoding.UTF8.GetString(receivedMessage.Body);
        var payload = JsonConvert.DeserializeObject<CastVoteDto>(payloadString);
        if (payload != null)
        {
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

            var client = new TableClient(new Uri(storageAccountname), storageTableName, identity);
            await client.UpsertEntityAsync(voteEntity);

            var calcCommand = new ChartCalculationCommand
            {
                PollId = payload.PollId,
                SessionId = payload.SessionId
            }.ToServiceBusMessage();

            await sender.SendMessageAsync(calcCommand);
            await receiver.CompleteMessageAsync(receivedMessage);
        }

    }

}
