using Azure;
using Azure.Data.Tables;

namespace PollStar.Votes.Repositories.Entities
{
    public  class ChartSumEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public int Total { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
