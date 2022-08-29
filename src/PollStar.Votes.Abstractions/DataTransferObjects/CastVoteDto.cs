namespace PollStar.Votes.Abstractions.DataTransferObjects;

public class CastVoteDto
{
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }
    public Guid PollId { get; set; }
    public Guid OptionId { get; set; }
}