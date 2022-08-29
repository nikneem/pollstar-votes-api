using Microsoft.Extensions.DependencyInjection;
using PollStar.Votes.Abstractions.Repositories;
using PollStar.Votes.Abstractions.Services;
using PollStar.Votes.Repositories;
using PollStar.Votes.Services;

namespace PollStar.Votes;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPollStarVotes(this IServiceCollection services)
    {
        services.AddTransient<IPollStarVotesService, PollStarVotesService>();
        services.AddTransient<IPollStarVotesRepositories, PollStarVotesRepositories>();
        return services;
    }
}