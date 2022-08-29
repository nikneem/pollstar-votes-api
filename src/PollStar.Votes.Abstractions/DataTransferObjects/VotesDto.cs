namespace PollStar.Votes.Abstractions.DataTransferObjects;

public class VotesDto
{
    public Guid PollId { get; set; }
    public List<VoteOptionsDto> Votes { get; set; }

    public VotesDto()
    {
        Votes = new List<VoteOptionsDto>();
    }
}