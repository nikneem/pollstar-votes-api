using System.Net.WebSockets;
using Azure.Core;
using Azure.Messaging.WebPubSub;
using HexMaster.RedisCache.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PollStar.Core.Configuration;
using PollStar.Core.Events;
using PollStar.Core.ExtensionMethods;
using PollStar.Core.Factories;
using PollStar.Votes.Abstractions.DataTransferObjects;
using PollStar.Votes.Abstractions.Repositories;
using PollStar.Votes.Abstractions.Services;

namespace PollStar.Votes.Services;

public class PollStarVotesService: IPollStarVotesService
{
    private readonly ILogger<PollStarVotesService> _logger;
    private readonly IPollStarVotesRepositories _repository;
    private readonly IPollStarChartsRepositories _chartsRepository;
    private readonly IServiceBusClientFactory _queueFactory;
    private readonly ICacheClientFactory _cacheClientFactory;
    private const string QueueName = "votes";


    public  Task<VotesDto> GetVotesAsync(Guid pollId)
    {
        var cacheClient = _cacheClientFactory.CreateClient();
        var cacheKey = $"PollStar:Polls:{pollId}:summary";
        return cacheClient.GetOrInitializeAsync(() => GetVotesSummaryFromRepository(pollId), cacheKey);
    }

    public async Task CastVoteAsync(CastVoteDto dto)
    {
        try
        {
            var queueClient = _queueFactory.CreateSender(QueueName);
            await queueClient.SendMessageAsync(dto.ToServiceBusMessage());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue vote message on service bus queue");
        }
    }

    private  Task<VotesDto> GetVotesSummaryFromRepository(Guid pollId)
    {
        return  _chartsRepository.GetVotesSummary(pollId);
    }

    //private async Task BroadcastPollVotes(Guid sessionId, VotesDto dto)
    //{
    //    var pubsubClient = new WebPubSubServiceClient(_cloudConfiguration.Value.WebPubSub, _cloudConfiguration.Value.PollStarHub);
    //    var payload = RealtimeEvent.FromDto("poll-votes", dto.Votes);
    //    await pubsubClient.SendToGroupAsync(sessionId.ToString(), payload, ContentType.ApplicationJson);
    //}

    public PollStarVotesService(
        ILogger<PollStarVotesService> logger,
        IPollStarVotesRepositories repository,
        IPollStarChartsRepositories chartsRepository,
        IServiceBusClientFactory queueFactory,
        ICacheClientFactory cacheClientFactory
        )
    {
        _logger = logger;
        _repository = repository;
        _chartsRepository = chartsRepository;
        _queueFactory = queueFactory;
        _cacheClientFactory = cacheClientFactory;
    }
}