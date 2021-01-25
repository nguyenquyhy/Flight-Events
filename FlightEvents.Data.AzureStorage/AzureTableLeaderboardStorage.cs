using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlightEvents.Data.AzureStorage
{
    public class LeaderboardRecordEntity : TableEntity
    {
        public LeaderboardRecordEntity()
        {
        }

        public LeaderboardRecordEntity(LeaderboardRecord record)
        {
            PartitionKey = record.EventId.ToString("N");
            RowKey = Guid.NewGuid().ToString("N");
            EventId = record.EventId;
            PlayerName = record.PlayerName;
            LeaderboardName = record.LeaderboardName;
            SubIndex = record.SubIndex;
            Score = record.Score;
            ScoreDisplay = record.ScoreDisplay;
            TimeSinceStart = record.TimeSinceStart;
            TimeSinceLast = record.TimeSinceLast;
        }

        public LeaderboardRecord ToRecord()
        {
            return new LeaderboardRecord
            {
                EventId = EventId,
                LeaderboardName = LeaderboardName,
                SubIndex = SubIndex,
                PlayerName = PlayerName,
                Score = Score,
                ScoreDisplay = ScoreDisplay,
                TimeSinceStart = TimeSinceStart,
                TimeSinceLast = TimeSinceLast
            };
        }

        public Guid EventId { get; set; }
        public string PlayerName { get; set; }
        public string LeaderboardName { get; set; }
        public int SubIndex { get; set; }
        public long Score { get; set; }
        public string ScoreDisplay { get; set; }
        public long TimeSinceStart { get; set; }
        public long TimeSinceLast { get; set; }
    }

    public class AzureTableLeaderboardStorage : ILeaderboardStorage
    {
        private readonly CloudTable table;

        public AzureTableLeaderboardStorage(IOptionsMonitor<AzureTableLeaderboardOptions> options)
        {
            var account = CloudStorageAccount.Parse(options.CurrentValue.ConnectionString);
            var tableClient = account.CreateCloudTableClient();
            table = tableClient.GetTableReference(options.CurrentValue.LeaderboardTable);
        }

        public async Task<List<LeaderboardRecord>> LoadAsync(Guid eventId)
        {
            await table.CreateIfNotExistsAsync();

            var result = table.CreateQuery<LeaderboardRecordEntity>()
                .Where(o => o.PartitionKey == eventId.ToString("N"))
                .AsEnumerable();

            return result.Select(o => o.ToRecord()).ToList();
        }

        public async Task<LeaderboardRecord> SaveAsync(LeaderboardRecord record)
        {
            await table.CreateIfNotExistsAsync();
            var result = await table.ExecuteAsync(TableOperation.InsertOrMerge(new LeaderboardRecordEntity(record)));
            var entity = result.Result as LeaderboardRecordEntity;

            return entity.ToRecord();
        }
    }
}
