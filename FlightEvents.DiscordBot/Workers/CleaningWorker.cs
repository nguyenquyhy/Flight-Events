using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FlightEvents.DiscordBot
{
    public class CleaningWorker : BackgroundService
    {
        private readonly ILogger<CleaningWorker> logger;
        private readonly AppOptions appOptions;
        private readonly DiscordOptions discordOptions;
        private DiscordSocketClient botClient;

        private readonly ConcurrentDictionary<ulong, Stopwatch> channelStopwatches = new ConcurrentDictionary<ulong, Stopwatch>();

        public CleaningWorker(ILogger<CleaningWorker> logger,
            IOptionsMonitor<AppOptions> appOptionsAccessor,
            IOptionsMonitor<DiscordOptions> discordOptionsAccessor)
        {
            this.logger = logger;
            this.appOptions = appOptionsAccessor.CurrentValue;
            this.discordOptions = discordOptionsAccessor.CurrentValue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                botClient = new DiscordSocketClient();

                botClient.UserVoiceStateUpdated += BotClient_UserVoiceStateUpdated;
                botClient.GuildAvailable += BotClient_GuildAvailable;
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

        private Task BotClient_GuildAvailable(SocketGuild guild)
        {
            try
            {
                logger.LogInformation("{guildName} is available.", guild.Name);

                var serverOptions = discordOptions.Servers.SingleOrDefault(o => o.ServerId == guild.Id);

                if (serverOptions != null)
                {
                    Task.Run(async () =>
                    {
                        while (true)
                        {
                            var category = guild.GetCategoryChannel(serverOptions.ChannelCategoryId);
                            if (category != null)
                            {
                                var channels = category.Channels
                                    .Where(channel => channel is SocketVoiceChannel voiceChannel)
                                    .Cast<SocketVoiceChannel>()
                                    .Where(channel => IsExtraChannel(channel, serverOptions));

                                logger.LogTrace("{guildName}: {allChannels}", guild.Name, string.Join(" | ", channels.Select(o => o.Name)));

                                foreach (var voiceChannel in channels)
                                {
                                    var stopwatch = AddOrGetStopwatch(voiceChannel);
                                    if (voiceChannel.Users.Count > 0)
                                    {
                                        stopwatch.Restart();
                                    }
                                    else if (stopwatch.ElapsedMilliseconds > 60000)
                                    {
                                        try
                                        {
                                            logger.LogInformation("Deleting {channelName} of guild {guildName}.", voiceChannel.Name, guild.Name);
                                            await voiceChannel.DeleteAsync();
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.LogError(ex, "Cannot delete channel {channelName} of guild {guildName}!", voiceChannel.Name, guild.Name);
                                        }
                                        finally
                                        {
                                            channelStopwatches.TryRemove(voiceChannel.Id, out _);
                                        }
                                    }
                                }
                            }

                            await Task.Delay(1000);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot prepare server [{guildId}] {guildName}", guild.Id, guild.Name);
            }
            return Task.CompletedTask;
        }

        private Task BotClient_UserVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
        {
            if (oldState.VoiceChannel?.Id != newState.VoiceChannel?.Id)
            {
                logger.LogInformation("Member {username} updated. Old: {oldChannel} of {oldGuild}. New: {newChannel} of {newGuild}",
                    user.Username, oldState.VoiceChannel?.Name, oldState.VoiceChannel?.Guild?.Name, newState.VoiceChannel?.Name, newState.VoiceChannel?.Guild?.Name);
                if (oldState.VoiceChannel != null)
                {
                    TouchVoiceChannel(oldState.VoiceChannel);
                }
                if (newState.VoiceChannel != null)
                {
                    TouchVoiceChannel(newState.VoiceChannel);
                }
            }
            return Task.CompletedTask;
        }

        private void TouchVoiceChannel(SocketVoiceChannel voiceChannel)
        {
            var serverOptions = discordOptions.Servers.SingleOrDefault(o => o.ServerId == voiceChannel.Guild.Id);
            if (IsExtraChannel(voiceChannel, serverOptions))
            {
                var stopwatch = AddOrGetStopwatch(voiceChannel);
                stopwatch.Restart();
            }
        }

        private bool IsExtraChannel(SocketVoiceChannel channel, DiscordServerOptions serverOptions)
            => channel.CategoryId == serverOptions.ChannelCategoryId &&
                                                    (string.IsNullOrWhiteSpace(serverOptions.LoungeChannelName) || channel.Name != serverOptions.LoungeChannelName) &&
                                                    (serverOptions.LoungeChannelId == null || channel.Id != serverOptions.LoungeChannelId.Value) &&
                                                    (serverOptions.ExternalChannelIds == null || !serverOptions.ExternalChannelIds.Contains(channel.Id));

        private Stopwatch AddOrGetStopwatch(SocketVoiceChannel voiceChannel)
            => channelStopwatches.GetOrAdd(voiceChannel.Id, id =>
            {
                logger.LogInformation("Adding stopwatch for [{channelId}] {channelName} of category {categoryName} of guild [{guildId}] {guildName}.", voiceChannel.Id, voiceChannel.Name, voiceChannel.Category.Name, voiceChannel.Guild.Id, voiceChannel.Guild.Name);
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                return stopwatch;
            });
    }
}
