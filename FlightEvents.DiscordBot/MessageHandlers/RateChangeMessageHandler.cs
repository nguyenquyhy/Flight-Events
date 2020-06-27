using Discord.WebSocket;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlightEvents.DiscordBot.MessageHandlers
{
    public class RateChangeMessageHandler : IMessageHandler
    {
        private readonly Regex updateRateCommand = new Regex("!rate (.*) ([0-9]+)hz");
        private readonly HubConnection hub;

        public RateChangeMessageHandler(HubConnection hub)
        {
            this.hub = hub;
        }

        public async Task<bool> ProcessAsync(SocketMessage message)
        {
            if (message.Channel is SocketDMChannel channel)
            {
                var match = updateRateCommand.Match(message.Content);
                if (match.Success)
                {
                    var callsign = match.Groups[1].Value;
                    var rate = int.Parse(match.Groups[2].Value);
                    await hub.SendAsync("ChangeUpdateRateByCallsign", callsign, rate);
                    await channel.SendMessageAsync($"Sent request to change update rate of '{callsign}' to '{rate}'Hz");

                    return true;
                }
            }
            return false;
        }
    }
}
