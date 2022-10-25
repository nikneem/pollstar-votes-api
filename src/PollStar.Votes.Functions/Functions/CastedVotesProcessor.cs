using System;
using Azure;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PollStar.Charts.Api;
using PollStar.Core.ExtensionMethods;
using PollStar.Votes.Abstractions.DataTransferObjects;
using PollStar.Votes.Functions.Commands;
using PollStar.Votes.Repositories.Entities;

namespace PollStar.Votes.Functions.Functions
{
    public class CastedVotesProcessor
    {
        [FunctionName("CastedVotesProcessor")]
        public async Task Run(
            [ServiceBusTrigger(Queues.Votes, Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message,
            [ServiceBus(Queues.Charts, Connection = "ServiceBusConnection")] IAsyncCollector<ServiceBusMessage> calculationCommands,
            [Table(Tables.Votes)] TableClient client,
            ILogger log)
        {
            var payloadString = Encoding.UTF8.GetString(message.Body);
            var payload = JsonConvert.DeserializeObject<CastVoteDto>(payloadString);
            if (payload == null)
            {
                log.LogError("Could not parse the casted vote into a valid vote object");
            }
            else
            {
                Activity.Current?.AddBaggage("PollId", payload.PollId.ToString());
                Activity.Current?.AddBaggage("UserId", payload.UserId.ToString());

                log.LogInformation("Received vote for poll {pollId} for processing", payload.PollId);

                var voteEntity = new VoteTableEntity
                {
                    PartitionKey = payload.PollId.ToString(),
                    RowKey = payload.UserId.ToString(),
                    OptionId = payload.OptionId.ToString(),
                    Timestamp = DateTimeOffset.UtcNow,
                    ETag = ETag.All
                };

                await client.UpsertEntityAsync(voteEntity);

                var calcCommand = new ChartCalculationCommand
                {
                    PollId = payload.PollId,
                    SessionId = payload.SessionId
                }.ToServiceBusMessage();
                try
                {
                    await calculationCommands.AddAsync(calcCommand);
                    await calculationCommands.FlushAsync();
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Failure");
                }
            }
        }
    }
}
