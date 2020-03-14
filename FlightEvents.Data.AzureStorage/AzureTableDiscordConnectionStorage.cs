using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace FlightEvents.Data
{
    public class AzureTableDiscordConnectionStorage : IDiscordConnectionStorage
    {
        private readonly CloudTable table;

        public AzureTableDiscordConnectionStorage(IConfiguration configuration)
        {
            var account = CloudStorageAccount.Parse(configuration["FlightPlan:AzureStorage:ConnectionString"]);
            var tableClient = account.CreateCloudTableClient();
            table = tableClient.GetTableReference(configuration["FlightPlan:AzureStorage:DiscordConnectionTable"]);
        }

        public async Task<ulong?> GetUserIdAsync(string clientId)
        {
            await table.CreateIfNotExistsAsync();

            var result = await table.ExecuteAsync(TableOperation.Retrieve<ConnectionEntity>("Discord", clientId));
            var entity = result.Result as ConnectionEntity;

            return (ulong)entity.UserId;
        }

        public async Task StoreConnectionAsync(string clientId, ulong userId)
        {
            await table.CreateIfNotExistsAsync();
            var entity = await table.ExecuteAsync(TableOperation.InsertOrMerge(new ConnectionEntity(clientId, userId)));
        }
    }

    public class ConnectionEntity : TableEntity
    {
        public ConnectionEntity()
        {
        }

        public ConnectionEntity(string clientId, ulong userId)
        {
            PartitionKey = "Discord";
            RowKey = clientId;
            UserId = (long)userId;
        }

        public long UserId { get; set; }
    }
}
