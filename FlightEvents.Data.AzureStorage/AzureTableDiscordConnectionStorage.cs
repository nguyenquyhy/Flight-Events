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

        public async Task<DiscordConnection> GetConnectionAsync(string clientId)
        {
            await table.CreateIfNotExistsAsync();

            var result = await table.ExecuteAsync(TableOperation.Retrieve<ConnectionEntity>("Discord", clientId));

            if (result.Result is ConnectionEntity entity)
            {
                return new DiscordConnection
                {
                    UserId = (ulong)entity.UserId,
                    Username = entity.Username,
                    Discriminator = entity.Discriminator
                };
            }
            return null;
        }

        public async Task<DiscordConnection> StoreConnectionAsync(string clientId, ulong userId, string username, string discriminator)
        {
            await table.CreateIfNotExistsAsync();
            var result = await table.ExecuteAsync(TableOperation.InsertOrMerge(new ConnectionEntity(clientId, userId, username, discriminator)));
            var entity = result.Result as ConnectionEntity;

            return new DiscordConnection
            {
                UserId = (ulong)entity.UserId,
                Username = entity.Username,
                Discriminator = entity.Discriminator
            };
        }

        public async Task DeleteConnectionAsync(string clientId)
        {
            await table.CreateIfNotExistsAsync();

            var result = await table.ExecuteAsync(TableOperation.Retrieve<ConnectionEntity>("Discord", clientId));
            if (result.Result is ConnectionEntity entity)
            {
                await table.ExecuteAsync(TableOperation.Delete(entity));
            }
        }
    }

    public class ConnectionEntity : TableEntity
    {
        public ConnectionEntity()
        {
        }

        public ConnectionEntity(string clientId, ulong userId, string username, string discriminator)
        {
            PartitionKey = "Discord";
            RowKey = clientId;
            UserId = (long)userId;
            Username = username;
            Discriminator = discriminator;
        }

        public long UserId { get; set; }
        public string Username { get; set; }
        public string Discriminator { get; set; }
    }
}
