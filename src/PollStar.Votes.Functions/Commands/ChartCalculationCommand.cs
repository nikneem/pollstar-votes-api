using System;

namespace PollStar.Votes.Functions.Commands
{
    public class ChartCalculationCommand
    {
        public Guid SessionId { get; set; }
        public Guid PollId { get; set; }
    }
}
