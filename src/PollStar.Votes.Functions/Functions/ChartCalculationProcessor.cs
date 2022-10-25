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

namespace PollStar.Votes.Functions.Functions;

public class ChartCalculationProcessor
{
    [FunctionName("ChartCalculationProcessor")]
    public async Task Run(
        [ServiceBusTrigger(Queues.Charts, Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
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
            //            var voteEntities = new List<CastedVoteEntity>();
            var votesQuery = votesClient.QueryAsync<VoteTableEntity>(
                $"{nameof(VoteTableEntity.PartitionKey)} eq '{payload.PollId}'");
            await foreach (var page in votesQuery.AsPages())
            {
                //                voteEntities.AddRange(page.Values);
                votes.AddRange(page.Values.Select(v =>
                    new VoteOptionsDto
                    {
                        OptionId = Guid.Parse(v.OptionId),
                        Votes = 1
                    }));
            }

            //            var lastVote = voteEntities.Max(ve => ve.Timestamp);
            //            var continueCumulation = lastVote.HasValue && lastVote.Value.CompareTo(continuationTime) > 0;

            var votesSummary = votes.GroupBy(v => v.OptionId)
                .Select((vc) => new VoteOptionsDto
                    {OptionId = vc.Key, Votes = vc.Sum(vq => vq.Votes)})
                .ToList();

            var transaction = new List<TableTransactionAction>();
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
}