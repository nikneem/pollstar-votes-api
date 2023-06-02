namespace PollStar.Votes.Abstractions.Commands
{
    public class ChartCalculationCommand
    {
        public Guid SessionId { get; set; }
        public Guid PollId { get; set; }
    }
}
