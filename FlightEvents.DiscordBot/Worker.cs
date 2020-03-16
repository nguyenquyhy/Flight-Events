using Discord;
using Discord.WebSocket;
using FlightEvents.Data;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FlightEvents.DiscordBot
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;
        private readonly IConfiguration configuration;
        private readonly IDiscordConnectionStorage discordConnectionStorage;
        private DiscordSocketClient botClient;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, IDiscordConnectionStorage discordConnectionStorage)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.discordConnectionStorage = discordConnectionStorage;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            var hub = new HubConnectionBuilder()
                .WithUrl(configuration["WebServerUrl"] + "/FlightEventHub")
                .WithAutomaticReconnect()
                .Build();

            hub.On<string, int?, int>("ChangeFrequency", async (clientId, from, to) =>
            {
                await MoveVoiceChannelAsync(clientId, to);
            });

            await hub.StartAsync();
            logger.LogInformation("Connected to SignalR server");

            try
            {
                botClient = new DiscordSocketClient();
                botClient.GuildAvailable += BotClient_GuildAvailable;
                await botClient.LoginAsync(TokenType.Bot, configuration["Discord:BotToken"]);
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

        private Task BotClient_GuildAvailable(SocketGuild guild)
        {
            logger.LogInformation($"{guild.Name} is available.");
            return Task.CompletedTask;
        }

        private async Task MoveVoiceChannelAsync(string clientId, int toFrequency)
        {
            var userId = await discordConnectionStorage.GetUserIdAsync(clientId);
            if (userId == null) return;

            ulong guildId = ulong.Parse(configuration["Discord:ServerId"]);
            ulong channelCategoryId = ulong.Parse(configuration["Discord:ChannelCategoryId"]);

            var channelName = (toFrequency / 1000d).ToString("N3");

            var guild = botClient.Guilds.FirstOrDefault(o => o.Id == guildId);
            if (guild == null) return;

            var channel = guild.Channels.FirstOrDefault(c => c.Name == channelName);
            if (channel == null)
            {
                await guild.CreateVoiceChannelAsync(channelName, props =>
                {
                    props.CategoryId = channelCategoryId;
                    props.Bitrate = int.Parse(configuration["Discord:ChannelBitrate"]);
                });

                logger.LogInformation($"Created new channel {channelName}");

                await MoveVoiceChannelAsync(clientId, toFrequency);
            }
            else
            {
                var guildUser = guild.GetUser(userId.Value);
                var oldChannel = guildUser.VoiceChannel;

                await guildUser.ModifyAsync(props =>
                {
                    props.ChannelId = channel.Id;
                });
                logger.LogInformation($"Moved user {guildUser.Username}#{guildUser.Discriminator} to channel {channelName}");

                if (oldChannel?.CategoryId == channelCategoryId)
                {
                    await Task.Delay(2000);

                    if (!oldChannel.Users.Any())
                    {
                        await oldChannel.DeleteAsync();
                        logger.LogInformation($"Removed empty channel {channelName}");
                    }
                }
            }
        }
    }
}
