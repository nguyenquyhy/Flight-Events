using Discord;
using Discord.WebSocket;
using FlightEvents.DiscordBot.MessageHandlers;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FlightEvents.DiscordBot
{
    public class DiscordMessageWorker : BackgroundService
    {
        private readonly DiscordSocketClient botClient = new DiscordSocketClient();
        private readonly ILogger<DiscordMessageWorker> logger;
        private readonly DiscordOptions discordOptions;

        private readonly List<IMessageHandler> messageHandlers;

        public DiscordMessageWorker(
            ILoggerFactory loggerFactory,
            ILogger<DiscordMessageWorker> logger,
            IOptionsMonitor<DiscordOptions> discordOptionsAccessor,
            HubConnection hub)
        {
            this.logger = logger;
            this.discordOptions = discordOptionsAccessor.CurrentValue;

            messageHandlers = new List<IMessageHandler>
            {
                new RateChangeMessageHandler(hub),
                new FlightInfoMessageHandler(loggerFactory.CreateLogger<FlightInfoMessageHandler>(), discordOptionsAccessor,  hub)
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await ConnectToDiscord();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }

            await botClient.LogoutAsync();
            await botClient.StopAsync();
            logger.LogInformation("{worker} disconnected from Discord", nameof(DiscordMessageWorker));
        }

        private async Task ConnectToDiscord()
        {
            botClient.MessageReceived += BotClient_MessageReceived;
            botClient.GuildAvailable += BotClient_GuildAvailable;
            while (true)
            {
                try
                {
                    await botClient.LoginAsync(TokenType.Bot, discordOptions.BotToken);
                    await botClient.StartAsync();
                    logger.LogInformation("{worker} connected to Discord", nameof(DiscordMessageWorker));
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{worker} cannot connect to Discord. Reconnect in 60s...", nameof(DiscordMessageWorker));
                }

                await Task.Delay(60000);
            }
        }

        private Task BotClient_GuildAvailable(SocketGuild guild)
        {
            logger.LogInformation("Guild {guildId} {guildName} is available.", guild.Id, guild.Name);
            logger.LogInformation($"Roles received:\n{string.Join("\n", guild.Roles.Select(role => $"- {role.Id} {role.Name}"))}");
            return Task.CompletedTask;
        }

        private async Task BotClient_MessageReceived(SocketMessage message)
        {
            foreach (var handler in messageHandlers)
            {
                try
                {
                    if (await handler.ProcessAsync(message))
                    {
                        logger.LogInformation($"Message {message.Id} \"{message.Content}\" is processed by {handler.GetType().Name}.");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error when {handler.GetType().Name} processes message {message.Id} \"{message.Content}\"");
                }
            }
        }
    }
}
