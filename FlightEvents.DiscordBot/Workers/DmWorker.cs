using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FlightEvents.DiscordBot
{
    public class DmWorker : BackgroundService
    {
        private readonly Regex updateRateCommand = new Regex("!rate (.*) ([0-9]+)hz");

        private DiscordSocketClient botClient;
        private readonly ILogger<DmWorker> logger;
        private readonly HubConnection hub;
        private readonly DiscordOptions discordOptions;

        public DmWorker(ILogger<DmWorker> logger,
            IOptionsMonitor<DiscordOptions> discordOptionsAccessor,
            HubConnection hub)
        {
            this.logger = logger;
            this.hub = hub;
            this.discordOptions = discordOptionsAccessor.CurrentValue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                botClient = new DiscordSocketClient();

                botClient.MessageReceived += BotClient_MessageReceived;
                await botClient.LoginAsync(TokenType.Bot, discordOptions.BotToken);
                await botClient.StartAsync();
                logger.LogInformation("Connected to Discord");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot connect to Discord");
                throw;
            }
        }

        private async Task BotClient_MessageReceived(SocketMessage message)
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
                }
            }
        }
    }
}
