﻿using Azure.Core;
using Azure.Messaging.WebPubSub;
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
    private readonly IPollStarVotesRepositories _repository;
    private readonly IOptions<AzureConfiguration> _cloudConfiguration;
    private readonly IServiceBusClientFactory _queueFactory;
    private const string QueueName = "votes";


    public Task<VotesDto> GetVotesAsync(Guid pollId)
    {
        return _repository.GetPollVostesAsync(pollId);
    }

    public async Task CastVoteAsync(CastVoteDto dto)
    {
        var queueClient = _queueFactory.CreateSender(QueueName);
        await queueClient.SendMessageAsync(dto.ToServiceBusMessage());
    }

    private async Task BroadcastPollVotes(Guid sessionId, VotesDto dto)
    {
        var pubsubClient = new WebPubSubServiceClient(_cloudConfiguration.Value.WebPubSub, _cloudConfiguration.Value.PollStarHub);
        var payload = RealtimeEvent.FromDto("poll-votes", dto.Votes);
        await pubsubClient.SendToGroupAsync(sessionId.ToString(), payload, ContentType.ApplicationJson);
    }

    public PollStarVotesService(IPollStarVotesRepositories repository, IOptions<AzureConfiguration> cloudConfiguration, IServiceBusClientFactory queueFactory)
    {
        _repository = repository;
        _cloudConfiguration = cloudConfiguration;
        _queueFactory = queueFactory;
    }
}