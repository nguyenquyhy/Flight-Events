using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FlightEvents.Data
{
    public class AzureTableOptions
    {
        [Required]
        public string ConnectionString { get; set; }
        [Required]
        public string DiscordConnectionTable { get; set; }
    }

    public class AzureTableDiscordConnectionStorage : IDiscordConnectionStorage
    {
        private readonly CloudTable table;

        public AzureTableDiscordConnectionStorage(IOptionsMonitor<AzureTableOptions> options)
        {
            var account = CloudStorageAccount.Parse(options.CurrentValue.ConnectionString);
            var tableClient = account.CreateCloudTableClient();
            table = tableClient.GetTableReference(options.CurrentValue.DiscordConnectionTable);
        }

        public async Task<List<string>> GetClientIdsAsync(ulong discordUserId)
        {
            await table.CreateIfNotExistsAsync();
            var result = table.ExecuteQuery(
                new TableQuery<ConnectionEntity>()
                .Where(TableQuery.GenerateFilterConditionForLong(nameof(ConnectionEntity.UserId), QueryComparisons.Equal, (long)discordUserId)));

            return result.Select(o => o.RowKey).ToList();
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
