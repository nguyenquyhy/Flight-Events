using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;

namespace FlightEvents.Data
{
    public class UserEntity : TableEntity
    {
        public UserEntity()
        {
        }

        public UserEntity(User user)
        {
            PartitionKey = "User";
            RowKey = user.Username;
            Username = user.Username;
            Name = user.Name;
            Roles = string.Join(",", user.Roles ?? new string[0]);
        }

        public User ToUser()
        {
            return new User
            {
                Username = Username,
                Name = Name,
                Roles = Roles.Split(",")
            };
        }

        public string Username { get; set; }
        public string Name { get; set; }
        public string Roles { get; set; }
    }

    public class AzureTableUserStorage : IUserStorage
    {
        private readonly CloudTable table;

        public AzureTableUserStorage(IOptionsMonitor<AzureTableOptions> options)
        {
            var account = CloudStorageAccount.Parse(options.CurrentValue.ConnectionString);
            var tableClient = account.CreateCloudTableClient();
            table = tableClient.GetTableReference(options.CurrentValue.UserTable);
        }

        public async Task<User> GetAsync(string username)
        {
            await table.CreateIfNotExistsAsync();

            var result = table.CreateQuery<UserEntity>()
                .Where(o => o.PartitionKey == "User" && o.RowKey == username).FirstOrDefault();

            return result?.ToUser();
        }
    }
}
