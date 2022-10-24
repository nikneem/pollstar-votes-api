using Azure.Core;
using Azure.Messaging.WebPubSub;
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
    private readonly IServiceBusClientFactory _queueFactory;
    private const string QueueName = "votes";


    public Task<VotesDto> GetVotesAsync(Guid pollId)
    {
        return _repository.GetPollVostesAsync(pollId);
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

    //private async Task BroadcastPollVotes(Guid sessionId, VotesDto dto)
    //{
    //    var pubsubClient = new WebPubSubServiceClient(_cloudConfiguration.Value.WebPubSub, _cloudConfiguration.Value.PollStarHub);
    //    var payload = RealtimeEvent.FromDto("poll-votes", dto.Votes);
    //    await pubsubClient.SendToGroupAsync(sessionId.ToString(), payload, ContentType.ApplicationJson);
    //}

    public PollStarVotesService(
        ILogger<PollStarVotesService> logger,
        IPollStarVotesRepositories repository, 
        IServiceBusClientFactory queueFactory)
    {
        _logger = logger;
        _repository = repository;
        _queueFactory = queueFactory;
    }
}