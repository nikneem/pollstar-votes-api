using Microsoft.Extensions.Logging;
using PollStar.Core.Factories;
using PollStar.Votes.Abstractions.DataTransferObjects;
using PollStar.Votes.Abstractions.Repositories;
using PollStar.Votes.Repositories.Entities;

namespace PollStar.Votes.Repositories;

public class PollStarChartsRepositories: IPollStarChartsRepositories
{
    private readonly IStorageTableClientFactory _tableStorageClientFactory;
    private readonly ILogger<PollStarChartsRepositories> _logger;

    private const string TableName = "charts";

    public async Task<VotesDto> GetVotesSummary(Guid pollId)
    {
        var votesModel = new VotesDto
        {
            PollId = pollId,
            Votes = new List<VoteOptionsDto>()
        };
        var table = _tableStorageClientFactory.CreateClient(TableName);
        var pollsQuery = table.QueryAsync<ChartSumEntity>($"{nameof(ChartSumEntity.PartitionKey)} eq '{pollId}'");
        
        await foreach (var page in pollsQuery.AsPages())
        {
            votesModel.Votes.AddRange(page.Values.Select(v =>
                new VoteOptionsDto
                {
                    OptionId = Guid.Parse(v.RowKey),
                    Votes = v.Total
                }));
        }

        return votesModel;
    }

    public PollStarChartsRepositories(IStorageTableClientFactory tableStorageClientFactory,  ILogger<PollStarChartsRepositories> logger)
    {
        _tableStorageClientFactory = tableStorageClientFactory;
        _logger = logger;
    }
}