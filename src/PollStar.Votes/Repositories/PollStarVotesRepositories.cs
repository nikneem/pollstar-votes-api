using Azure;
using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PollStar.Core;
using PollStar.Core.Configuration;
using PollStar.Votes.Abstractions.DataTransferObjects;
using PollStar.Votes.Abstractions.Repositories;
using PollStar.Votes.Repositories.Entities;

namespace PollStar.Votes.Repositories;

public class PollStarVotesRepositories : IPollStarVotesRepositories
{
    private readonly ILogger<PollStarVotesRepositories> _logger;

    private TableClient _tableClient;
    private QueueClient _queueClient;
    private const string TableName = "votes";
    private const string QueueName = "votes";

    public async Task<VotesDto> GetPollVostesAsync(Guid pollId)
    {
        var redisCacheKey = $"polls:votes:{pollId}";
        return await GetPollVotesByPollIdAsync(pollId);
    }

    public async Task<VotesDto> CastVoteAsync(CastVoteDto dto)
    {
        var overview = await GetPollVostesAsync(dto.PollId);

        try
        {
            var previouslyCastedVote =
                await _tableClient.GetEntityAsync<VoteTableEntity>(dto.PollId.ToString(), dto.UserId.ToString());
            if (previouslyCastedVote != null)
            {
                var previousCastOption = Guid.Parse(previouslyCastedVote.Value.OptionId);
                var vote = overview.Votes.FirstOrDefault(v => v.OptionId == previousCastOption);
                if (vote != null && vote.Votes > 0)
                {
                    vote.Votes -= 1;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting previous vote from user");
        }

        var newVoteEntity = new VoteTableEntity
        {
            PartitionKey = dto.PollId.ToString(),
            RowKey = dto.UserId.ToString(),
            OptionId = dto.OptionId.ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            ETag = ETag.All
        };

        var result = await _tableClient.UpsertEntityAsync(newVoteEntity, TableUpdateMode.Replace);
        if (!result.IsError)
        {
            var cumulative = overview.Votes.FirstOrDefault(x => x.OptionId == dto.OptionId) ??
                             new VoteOptionsDto {OptionId = dto.OptionId, Votes = 0};
            cumulative.Votes += 1;
            if (overview.Votes.All(x => x.OptionId != dto.OptionId))
            {
                overview.Votes.Add(cumulative);
            }
        }

//        await _cacheClient.SetAsAsync($"polls:votes:{dto.PollId}", overview);
        return overview;
    }

    private async Task<VotesDto> GetPollVotesByPollIdAsync(Guid pollId)
    {
        var polls = await GetRawVotesFromRepositoryAsync(pollId);
        return new VotesDto
        {
            PollId = pollId,
            Votes = polls.GroupBy(v => v.OptionId)
                .Select((vc) => new VoteOptionsDto
                    { OptionId = vc.Key, Votes = vc.Sum(vq => vq.Votes) })
                .ToList()
        };
    }
    private async Task<List<VoteOptionsDto>> GetRawVotesFromRepositoryAsync(Guid pollId)
    {
        var polls = new List<VoteOptionsDto>();
        var pollsQuery = _tableClient.QueryAsync<VoteTableEntity>($"{nameof(VoteTableEntity.PartitionKey)} eq '{pollId}'");
        await foreach (var page in pollsQuery.AsPages())
        {
            polls.AddRange(page.Values.Select(v =>
                new VoteOptionsDto
                {
                    OptionId = Guid.Parse(v.OptionId),
                    Votes = 1
                }));
        }

        return polls;
    }

    public PollStarVotesRepositories(IOptions<AzureConfiguration> options, ILogger<PollStarVotesRepositories> logger)
    {
        _logger = logger;

        var accountName = options.Value.StorageAccount;

        var managedIdentity = new DefaultAzureCredential();
        var tableStorageUri = new Uri($"https://{accountName}.table.core.windows.net");
        var queueStorageUri = new Uri($"https://{accountName}.queue.core.windows.net/{QueueName}");

        _queueClient = new QueueClient(queueStorageUri,  managedIdentity);
        _tableClient = new TableClient(tableStorageUri, TableName, managedIdentity);
    }

}