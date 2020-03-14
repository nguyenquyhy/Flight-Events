using System.Threading.Tasks;

namespace FlightEvents.Data
{
    public interface IDiscordConnectionStorage
    {
        Task<ulong?> GetUserIdAsync(string clientId);
        Task StoreConnectionAsync(string clientId, ulong userId);
    }
}
