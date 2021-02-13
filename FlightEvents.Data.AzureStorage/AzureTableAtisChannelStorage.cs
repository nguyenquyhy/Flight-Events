using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlightEvents.Data
{
    public class AtisChannelEntity : TableEntity
    {
        public AtisChannelEntity()
        {

        }

        public AtisChannelEntity(AtisChannel atisChannel)
        {
            PartitionKey = atisChannel.GuildId.ToString();
            RowKey = atisChannel.ChannelId.ToString();
            GuildId = (long)atisChannel.GuildId;
            GuildName = atisChannel.GuildName;
            ChannelId = (long)atisChannel.ChannelId;
            ChannelName = atisChannel.ChannelName;
            Frequency = atisChannel.Frequency;
            BotToken = atisChannel.BotToken;
            FilePath = atisChannel.FilePath;
            Nickname = atisChannel.Nickname;
            ProcessId = atisChannel.ProcessId;
        }

        public AtisChannel ToData()
            => new AtisChannel
            {
                GuildId = (ulong)GuildId,
                GuildName = GuildName,
                ChannelId = (ulong)ChannelId,
                ChannelName = ChannelName,
                Frequency = Frequency,
                BotToken = BotToken,
                FilePath = FilePath,
                Nickname = Nickname,
                ProcessId = ProcessId
            };

        public long GuildId { get; set; }
        public string GuildName { get; set; }
        public long ChannelId { get; set; }
        public string ChannelName { get; set; }
        public double Frequency { get; set; }
        public string BotToken { get; set; }

        public string FilePath { get; set; }
        public string Nickname { get; set; }
        public int ProcessId { get; set; }

    }

    public class AzureTableAtisChannelStorage : IAtisChannelStorage
    {

        private readonly CloudTable table;

        public AzureTableAtisChannelStorage(IOptionsMonitor<AzureTableAtisChannelOptions> options)
        {
            var account = CloudStorageAccount.Parse(options.CurrentValue.ConnectionString);
            var tableClient = account.CreateCloudTableClient();
            table = tableClient.GetTableReference(options.CurrentValue.AtisChannelTable);
        }

        public async Task<IEnumerable<AtisChannel>> GetByGuildAsync(ulong guildId)
        {
            await table.CreateIfNotExistsAsync();
            var result = table.ExecuteQuery(
                new TableQuery<AtisChannelEntity>()
                .Where(TableQuery.GenerateFilterCondition(nameof(AtisChannelEntity.PartitionKey), QueryComparisons.Equal, guildId.ToString())));

            return result.Select(o => o.ToData()).ToList();
        }

        public async Task<AtisChannel> GetByChannelNameAsync(ulong guildId, string channelName)
        {
            await table.CreateIfNotExistsAsync();

            var result = table.ExecuteQuery(
                new TableQuery<AtisChannelEntity>()
                .Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition(nameof(AtisChannelEntity.PartitionKey), QueryComparisons.Equal, guildId.ToString()),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition(nameof(AtisChannelEntity.ChannelName), QueryComparisons.Equal, channelName)
                )));

            return result.FirstOrDefault()?.ToData();
        }

        public async Task<AtisChannel> AddChannelAsync(AtisChannel atisChannel)
        {
            await table.CreateIfNotExistsAsync();
            var result = await table.ExecuteAsync(TableOperation.InsertOrMerge(new AtisChannelEntity(atisChannel)));
            var entity = result.Result as AtisChannelEntity;

            return entity.ToData();
        }

        public async Task RemoveAsync(ulong guildId, ulong channelId)
        {
            await table.CreateIfNotExistsAsync();

            var result = await table.ExecuteAsync(TableOperation.Retrieve<AtisChannelEntity>(guildId.ToString(), channelId.ToString()));
            if (result.Result is AtisChannelEntity entity)
            {
                await table.ExecuteAsync(TableOperation.Delete(entity));
            }
        }
    }
}
