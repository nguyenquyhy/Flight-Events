using Discord;
using Discord.WebSocket;
using FlightEvents.Data;
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
    public class MovingWorker : BackgroundService
    {
        private readonly DiscordSocketClient botClient = new DiscordSocketClient();
        private readonly ILogger<MovingWorker> logger;
        private readonly IDiscordServerStorage discordServerStorage;
        private readonly AppOptions appOptions;
        private readonly DiscordOptions discordOptions;
        private readonly IDiscordConnectionStorage discordConnectionStorage;
        private readonly HubConnection hub;
        private readonly ChannelMaker channelMaker;

        private List<DiscordServer> servers;

        public MovingWorker(ILogger<MovingWorker> logger,
            IOptionsMonitor<AppOptions> appOptionsAccessor,
            IOptionsMonitor<DiscordOptions> discordOptionsAccessor,
            IDiscordServerStorage discordServerStorage,
            IDiscordConnectionStorage discordConnectionStorage,
            HubConnection hub,
            ChannelMaker channelMaker)
        {
            this.logger = logger;
            this.discordServerStorage = discordServerStorage;
            this.appOptions = appOptionsAccessor.CurrentValue;
            this.discordOptions = discordOptionsAccessor.CurrentValue;
            this.discordConnectionStorage = discordConnectionStorage;
            this.hub = hub;
            this.channelMaker = channelMaker;
            hub.Reconnecting += Hub_Reconnecting;
            hub.Reconnected += Hub_Reconnected;
            hub.Closed += Hub_Closed;

            hub.On<string, int?, int?>("ChangeFrequency", async (clientId, from, to) =>
            {
                try
                {
                    logger.LogDebug("Got ChangeFrequency message from {clientId} to change from {fromFrequency} to {toFrequency}", clientId, from, to);
                    await CreateVoiceChannelAndMoveAsync(clientId, to);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Cannot handle changing frequency of {0} from {1} to {2}!", clientId, from, to);
                }
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            servers = await discordServerStorage.GetDiscordServersAsync();

            await ConnectToSignalR();
            await ConnectToDiscord();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }

            await hub.StopAsync();
            logger.LogInformation("{worker} disconnected from SignalR server", nameof(MovingWorker));

            await botClient.LogoutAsync();
            await botClient.StopAsync();
            logger.LogInformation("{worker} disconnected from Discord", nameof(MovingWorker));
        }

        private async Task ConnectToSignalR()
        {
            while (true)
            {
                try
                {
                    await hub.StartAsync();
                    logger.LogInformation("{worker} connected to SignalR server", nameof(MovingWorker));
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{worker} cannot connect to SignalR. Reconnect in 30s...", nameof(MovingWorker));
                }

                await Task.Delay(30000);
            }
        }

        private async Task ConnectToDiscord()
        {
            botClient.GuildAvailable += BotClient_GuildAvailable;
            while (true)
            {
                try
                {
                    await botClient.LoginAsync(TokenType.Bot, discordOptions.BotToken);
                    await botClient.StartAsync();
                    logger.LogInformation("{worker} connected to Discord", nameof(MovingWorker));
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{worker} cannot connect to Discord. Reconnect in 30s...", nameof(MovingWorker));
                }

                await Task.Delay(30000);
            }
        }

        private Task Hub_Reconnecting(Exception ex)
        {
            logger.LogError(ex, "Reconnecting to SignalR");
            return Task.CompletedTask;
        }

        private Task Hub_Reconnected(string arg)
        {
            logger.LogInformation("Reconnected to SignalR. {arg}", arg);
            return Task.CompletedTask;
        }

        private Task Hub_Closed(Exception ex)
        {
            logger.LogError(ex, "Close SignalR connection!");
            return Task.CompletedTask;
        }

        private async Task BotClient_GuildAvailable(SocketGuild guild)
        {
            try
            {
                logger.LogInformation("{guildName} is available.", guild.Name);

                var serverOptions = servers.SingleOrDefault(o => o.ServerId == guild.Id);

                if (serverOptions != null)
                {
                    var category = guild.GetCategoryChannel(serverOptions.ChannelCategoryId);
                    if (category != null && !string.IsNullOrWhiteSpace(serverOptions.LoungeChannelName))
                    {
                        var lounge = category.Channels.SingleOrDefault(o => o.Name == serverOptions.LoungeChannelName);
                        if (lounge == null)
                        {
                            logger.LogInformation("Create lounge channel named {lounge} in {guildName}.", serverOptions.LoungeChannelName, guild.Name);
                            var newLounge = await guild.CreateVoiceChannelAsync(serverOptions.LoungeChannelName, props =>
                            {
                                props.CategoryId = category.Id;
                            });
                            logger.LogInformation("Created lounge channel {loungeId} in {guildName}.", newLounge.Id, guild.Name);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot prepare server [{guildId}] {guildName}", guild.Id, guild.Name);
            }
        }

        private async Task CreateVoiceChannelAndMoveAsync(string clientId, int? toFrequency)
        {
            var connection = await discordConnectionStorage.GetConnectionAsync(clientId);
            if (connection == null)
            {
                return;
            }

            SocketGuildUser guildUser = null;
            DiscordServer serverOptions = null;
            foreach (var options in servers)
            {
                guildUser = botClient.Guilds.SingleOrDefault(o => o.Id == options.ServerId)?.GetUser(connection.UserId);
                serverOptions = options;
                if (guildUser?.VoiceChannel != null) break;
            }

            if (guildUser == null)
            {
                logger.LogDebug("Cannot find connected user {userId} in any server!", connection.UserId);
                return;
            }

            if (guildUser.VoiceChannel?.CategoryId != serverOptions.ChannelCategoryId)
            {
                // Do not touch user not connecting to voice or connecting outside the channel
                logger.LogDebug("Cannot move because connected user {userId} is in another voice channel category {categoryId}!", connection.UserId, guildUser.VoiceChannel?.CategoryId);
                return;
            }

            var guild = guildUser.Guild;

            var channel = await channelMaker.GetOrCreateVoiceChannelAsync(serverOptions, guild, toFrequency);
            await MoveMemberAsync(guildUser, channel);
        }

        private async Task MoveMemberAsync(SocketGuildUser guildUser, IGuildChannel channel)
        {
            await guildUser.ModifyAsync(props =>
            {
                props.ChannelId = channel.Id;
            });
            logger.LogInformation("Moved user {username}#{discriminator} to channel {channelName}", guildUser.Username, guildUser.Discriminator, channel.Name);
        }
    }
}
