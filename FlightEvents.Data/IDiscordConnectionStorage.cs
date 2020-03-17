using System.Threading.Tasks;

namespace FlightEvents.Data
{
    public interface IDiscordConnectionStorage
    {
        Task<DiscordConnection> GetConnectionAsync(string clientId);
        Task<DiscordConnection> StoreConnectionAsync(string clientId, ulong userId, string username, string discriminator);
        Task DeleteConnectionAsync(string clientId);
    }
}
