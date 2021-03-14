using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlightEvents.Data
{
    public class DiscordServerEntity : TableEntity
    {
        public DiscordServerEntity()
        {

        }

        public DiscordServerEntity(DiscordServer discordServer)
        {
            PartitionKey = "DiscordServer";
            RowKey = discordServer.ServerId.ToString();

            ServerId = (long)discordServer.ServerId;
            ChannelCategoryId = (long)discordServer.ChannelCategoryId;
            CommandChannelId = (long?)discordServer.CommandChannelId;

            LoungeChannelName = discordServer.LoungeChannelName;
            LoungeChannelId = (long?)discordServer.LoungeChannelId;

            ExternalChannelIds = string.Join(",", discordServer.ExternalChannelIds ?? new ulong[0]);

            ChannelBitrate = discordServer.ChannelBitrate;
            ChannelNameSuffix = discordServer.ChannelNameSuffix;

            FlightInfoRoleId = (long?)discordServer.FlightInfoRoleId;

            Remarks = discordServer.Remarks;
        }

        public DiscordServer ToData()
            => new DiscordServer
            {
                ServerId = (ulong)ServerId,
                ChannelCategoryId = (ulong)ChannelCategoryId,
                CommandChannelId = (ulong?)CommandChannelId,

                LoungeChannelName = LoungeChannelName,
                LoungeChannelId = (ulong?)LoungeChannelId,

                ExternalChannelIds = string.IsNullOrWhiteSpace(ExternalChannelIds) ? new ulong[0] : ExternalChannelIds.Split(',').Select(o => ulong.Parse(o)).ToArray(),

                ChannelBitrate = ChannelBitrate,
                ChannelNameSuffix = ChannelNameSuffix,

                FlightInfoRoleId = (ulong?)FlightInfoRoleId,

                Remarks = Remarks
            };


        public long ServerId { get; set; }
        public long ChannelCategoryId { get; set; }
        public long? CommandChannelId { get; set; }

        public string LoungeChannelName { get; set; }
        public long? LoungeChannelId { get; set; }

        public string ExternalChannelIds { get; set; }

        public int? ChannelBitrate { get; set; }
        public string ChannelNameSuffix { get; set; }

        /// <summary>
        /// Restrict the role that can use !finfo
        /// </summary>
        public long? FlightInfoRoleId { get; set; }

        public string Remarks { get; set; }
    }

    public class AzureTableDiscordServerStorage : IDiscordServerStorage
    {
        private readonly CloudTable table;

        public AzureTableDiscordServerStorage(IOptionsMonitor<AzureTableDiscordOptions> options)
        {
            var account = CloudStorageAccount.Parse(options.CurrentValue.ConnectionString);
            var tableClient = account.CreateCloudTableClient();
            table = tableClient.GetTableReference(options.CurrentValue.DiscordServerTable);
        }

        public async Task<List<DiscordServer>> GetDiscordServersAsync()
        {
            await table.CreateIfNotExistsAsync();
            var result = table.CreateQuery<DiscordServerEntity>().AsEnumerable();

            return result.Select(o => o.ToData()).ToList();
        }

        public async Task<DiscordServer> AddDiscordServersAsync(DiscordServer discordServer)
        {
            await table.CreateIfNotExistsAsync();
            var result = await table.ExecuteAsync(TableOperation.InsertOrMerge(new DiscordServerEntity(discordServer)));
            var entity = result.Result as DiscordServerEntity;

            return entity.ToData();
        }
    }
}
