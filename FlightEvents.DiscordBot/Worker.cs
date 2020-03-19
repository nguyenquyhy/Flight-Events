using Discord;
using Discord.WebSocket;
using FlightEvents.Data;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FlightEvents.DiscordBot
{
    public class AppOptions
    {
        [Required]
        public string WebServerUrl { get; set; }
    }

    public class DiscordOptions
    {
        [Required]
        public string BotToken { get; set; }
        [Required]
        public ulong ServerId { get; set; }
        [Required]
        public ulong ChannelCategoryId { get; set; }
        [Required]
        public int ChannelBitrate { get; set; }
    }

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;
        private readonly AppOptions appOptions;
        private readonly DiscordOptions discordOptions;
        private readonly IDiscordConnectionStorage discordConnectionStorage;
        private DiscordSocketClient botClient;

        public Worker(ILogger<Worker> logger,
            IOptionsMonitor<AppOptions> appOptionsAccessor,
            IOptionsMonitor<DiscordOptions> discordOptionsAccessor,
            IDiscordConnectionStorage discordConnectionStorage)
        {
            this.logger = logger;
            this.appOptions = appOptionsAccessor.CurrentValue;
            this.discordOptions = discordOptionsAccessor.CurrentValue;
            this.discordConnectionStorage = discordConnectionStorage;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Bot running at: {time}", DateTimeOffset.Now);

            var webServerUrl = appOptions.WebServerUrl;
            logger.LogInformation("Connecting to server URL {serverUrl}", webServerUrl);

            var hub = new HubConnectionBuilder()
                .WithUrl(webServerUrl + "/FlightEventHub")
                .WithAutomaticReconnect()
                .Build();

            hub.Reconnecting += Hub_Reconnecting;
            hub.Reconnected += Hub_Reconnected;

            hub.On<string, int?, int>("ChangeFrequency", async (clientId, from, to) =>
            {
                logger.LogDebug("Got ChangeFrequency message from {clientId} to change from {fromFrequency} to {toFrequency}", clientId, from, to);
                await MoveVoiceChannelAsync(clientId, to);
            });

            await hub.StartAsync();
            logger.LogInformation("Connected to SignalR server");

            try
            {
                botClient = new DiscordSocketClient();
                botClient.GuildAvailable += BotClient_GuildAvailable;
                await botClient.LoginAsync(TokenType.Bot, discordOptions.BotToken);
                await botClient.StartAsync();
                logger.LogInformation("Connected to Discord");

                await hub.SendAsync("Join", "Bot");
                logger.LogInformation("Joined Bot group");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot connect to Discord");
                throw;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        private Task Hub_Reconnecting(Exception arg)
        {
            logger.LogInformation("Reconnecting to SignalR");
            return Task.CompletedTask;
        }

        private Task Hub_Reconnected(string arg)
        {
            logger.LogInformation("Reconnected to SignalR");
            return Task.CompletedTask;
        }

        private Task BotClient_GuildAvailable(SocketGuild guild)
        {
            logger.LogInformation("{guildName} is available.", guild.Name);
            return Task.CompletedTask;
        }

        private async Task MoveVoiceChannelAsync(string clientId, int toFrequency)
        {
            var connection = await discordConnectionStorage.GetConnectionAsync(clientId);
            if (connection == null) return;

            ulong guildId = discordOptions.ServerId;
            ulong channelCategoryId = discordOptions.ChannelCategoryId;

            var channelName = (toFrequency / 1000d).ToString("N3");

            var guild = botClient.Guilds.FirstOrDefault(o => o.Id == guildId);
            if (guild == null) return;

            var channel = guild.Channels.FirstOrDefault(c => c.Name == channelName);
            if (channel == null)
            {
                await guild.CreateVoiceChannelAsync(channelName, props =>
                {
                    props.CategoryId = channelCategoryId;
                    props.Bitrate = discordOptions.ChannelBitrate;
                });

                logger.LogInformation("Created new channel {channelName}", channelName);

                await MoveVoiceChannelAsync(clientId, toFrequency);
            }
            else
            {
                var guildUser = guild.GetUser(connection.UserId);
                var oldChannel = guildUser.VoiceChannel;

                await guildUser.ModifyAsync(props =>
                {
                    props.ChannelId = channel.Id;
                });
                logger.LogInformation("Moved user {username}#{discriminator} to channel {channelName}", guildUser.Username, guildUser.Discriminator, channelName);

                if (oldChannel?.CategoryId == channelCategoryId)
                {
                    await Task.Delay(2000);

                    if (!oldChannel.Users.Any())
                    {
                        await oldChannel.DeleteAsync();
                        logger.LogInformation("Removed empty channel {channelName}", channelName);
                    }
                }
            }
        }
    }
}
