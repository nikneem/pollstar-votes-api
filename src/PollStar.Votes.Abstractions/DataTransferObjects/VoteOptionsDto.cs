namespace PollStar.Votes.Abstractions.DataTransferObjects;

public class VoteOptionsDto
{
    public Guid OptionId { get; set; }
    public int Votes { get; set; }
}