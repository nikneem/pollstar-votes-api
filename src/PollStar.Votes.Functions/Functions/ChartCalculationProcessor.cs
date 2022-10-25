using System;
using Azure;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PollStar.Charts.Api;
using PollStar.Votes.Abstractions.DataTransferObjects;
using PollStar.Votes.Functions.Commands;
using PollStar.Votes.Repositories.Entities;
using HexMaster.RedisCache.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.WebPubSub;
using Microsoft.Azure.WebPubSub.Common;
using PollStar.Core.Events;

namespace PollStar.Votes.Functions.Functions;

public class ChartCalculationProcessor
{
    private readonly ICacheClientFactory _cacheClientFactory;

    [FunctionName("ChartCalculationProcessor")]
    public async Task Run(
        [ServiceBusTrigger(Queues.Charts, Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        [WebPubSub(Hub = "<hub>")] IAsyncCollector<WebPubSubAction> actions,
        [Table(Tables.Votes)] TableClient votesClient,
        [Table(Tables.Charts)] TableClient chartsClient,
        ILogger log)
    {
        var sw = Stopwatch.StartNew();
        //        var continuationTime = DateTimeOffset.UtcNow.AddMinutes(-1);
        var payloadString = Encoding.UTF8.GetString(message.Body);
        var payload = JsonConvert.DeserializeObject<ChartCalculationCommand>(payloadString);
        if (payload == null)
        {
            log.LogError("Could not parse the calculation command into a valid calculation command message");
        }
        else
        {
            var votes = new List<VoteOptionsDto>();
            var votesQuery = votesClient.QueryAsync<VoteTableEntity>(
                $"{nameof(VoteTableEntity.PartitionKey)} eq '{payload.PollId}'");
            await foreach (var page in votesQuery.AsPages())
            {
                votes.AddRange(page.Values.Select(v =>
                    new VoteOptionsDto
                    {
                        OptionId = Guid.Parse(v.OptionId),
                        Votes = 1
                    }));
            }

            var votesSummary = votes.GroupBy(v => v.OptionId)
                .Select((vc) => new VoteOptionsDto
                    {OptionId = vc.Key, Votes = vc.Sum(vq => vq.Votes)})
                .ToList();

            var transaction = new List<TableTransactionAction>();

            try
            {
                var cacheClient = _cacheClientFactory.CreateClient();
                var cacheKey = $"PollStar:Polls:{payload.PollId}:summary";
                var votesCachedModel = new VotesDto
                {
                    PollId = payload.PollId,
                    Votes = votesSummary
                };
                await cacheClient.SetAsAsync(cacheKey, votesCachedModel);
                    var realtimePayload = RealtimeEvent.FromDto("poll-votes", votesCachedModel.Votes);
                    await actions.AddAsync(WebPubSubAction.CreateSendToGroupAction(
                        payload.SessionId.ToString(), 
                        realtimePayload,
                        WebPubSubDataType.Json));
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Failed to store summary in cache");
            }


            foreach (var voteSum in votesSummary)
            {
                transaction.Add(new TableTransactionAction(TableTransactionActionType.UpdateReplace,
                    new ChartSumEntity
                    {
                        PartitionKey = payload.PollId.ToString(),
                        RowKey = voteSum.OptionId.ToString(),
                        Total = voteSum.Votes,
                        ETag = ETag.All,
                        Timestamp = DateTimeOffset.UtcNow
                    }));
            }

            if (transaction.Count > 0)
            {
                await chartsClient.SubmitTransactionAsync(transaction);
            }
        }
        log.LogInformation("Took {milliseconds} milliseconds to process casted votes into a chart model", sw.ElapsedMilliseconds);
    }

    public ChartCalculationProcessor(ICacheClientFactory cacheClientFactory)
    {
        _cacheClientFactory = cacheClientFactory;
    }
}