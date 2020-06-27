using Discord.WebSocket;
using System.Threading.Tasks;

namespace FlightEvents.DiscordBot.MessageHandlers
{
    public interface IMessageHandler
    {
        Task<bool> ProcessAsync(SocketMessage message);
    }
}
