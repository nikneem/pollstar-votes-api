using Azure;
using Azure.Data.Tables;

namespace PollStar.Votes.Repositories.Entities;

public class VoteTableEntity : ITableEntity
{
    public string PartitionKey { get; set; } = default!;
    public string RowKey { get; set; } = default!;
    public string OptionId { get; set; } = default!;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}