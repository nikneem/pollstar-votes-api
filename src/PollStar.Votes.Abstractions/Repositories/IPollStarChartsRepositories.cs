using PollStar.Votes.Abstractions.DataTransferObjects;

namespace PollStar.Votes.Abstractions.Repositories;

public interface IPollStarChartsRepositories
{
    Task<VotesDto> GetVotesSummary(Guid pollId);
}