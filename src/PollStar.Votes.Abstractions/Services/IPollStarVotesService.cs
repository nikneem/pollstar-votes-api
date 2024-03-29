﻿using PollStar.Votes.Abstractions.DataTransferObjects;

namespace PollStar.Votes.Abstractions.Services;

public interface IPollStarVotesService
{
    Task<VotesDto> GetVotesAsync(Guid pollId);
    Task CastVoteAsync(CastVoteDto dto);
}