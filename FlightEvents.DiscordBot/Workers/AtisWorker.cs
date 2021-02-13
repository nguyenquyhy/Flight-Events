using Discord;
using Discord.Rest;
using Discord.WebSocket;
using FlightEvents.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FlightEvents.DiscordBot
{
    public class AtisWorker : BackgroundService
    {
        private readonly Regex regexStart = new Regex("^!atis ([0-9.]+)(.*)$", RegexOptions.IgnoreCase);
        private readonly Regex regexStop = new Regex("!atis stop ([0-9.]+)", RegexOptions.IgnoreCase);

        private readonly ILogger<AtisWorker> logger;
        private readonly ChannelMaker channelMaker;
        private readonly RegexMatcher regexMatcher;
        private readonly AtisProcessManager atisProcessManager;
        private readonly IAtisChannelStorage atisChannelStorage;
        private readonly AtisOptions atisOptions;
        private readonly DiscordOptions discordOptions;

        public AtisWorker(ILogger<AtisWorker> logger,
            IOptionsMonitor<AtisOptions> atisOptions,
            IOptionsMonitor<DiscordOptions> discordOptions,
            ChannelMaker channelMaker,
            RegexMatcher regexMatcher,
            AtisProcessManager atisProcessManager,
            IAtisChannelStorage atisChannelStorage)
        {
            this.logger = logger;
            this.channelMaker = channelMaker;
            this.regexMatcher = regexMatcher;
            this.atisProcessManager = atisProcessManager;
            this.atisChannelStorage = atisChannelStorage;
            this.atisOptions = atisOptions.CurrentValue;
            this.discordOptions = discordOptions.CurrentValue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var botClient = new DiscordSocketClient();
                botClient.GuildAvailable += BotClient_GuildAvailable;
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
            try
            {
                if (message.Channel is SocketTextChannel channel)
                {
                    var serverOptions = discordOptions.Servers.FirstOrDefault(guild => guild.ServerId == channel.Guild.Id);

                    if (serverOptions?.CommandChannelId == channel.Id)
                    {
                        var regexResult = regexMatcher.Match(new Dictionary<int, Regex>
                        {
                            [0] = regexStart,
                            [1] = regexStop
                        }, message.Content.Trim());

                        if (regexResult != null)
                        {
                            switch (regexResult.Value.key)
                            {
                                case 0:
                                    await HandleStartCommandAsync(regexResult.Value.match, message, channel, serverOptions);
                                    break;
                                case 1:
                                    await HandleStopCommandAsync(regexResult.Value.match, message, channel.Guild, serverOptions);
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot process ATIS bot command: {content}", message.Content);
            }
        }

        private async Task HandleStartCommandAsync(Match match, SocketMessage message, SocketTextChannel channel, DiscordServerOptions serverOptions)
        {
            logger.LogInformation("Process ATIS bot start command");

            if (double.TryParse(match.Groups[1].Value, out var frequency))
            {
                if (message.Attachments.Count > 0)
                {
                    Task.Run(async () =>
                    {
                        await ProcessBotRequestAsync(message, channel, serverOptions, frequency, match.Groups.Count > 2 ? match.Groups[2].Value.Trim() : null);
                    });
                }
                else
                {
                    await message.Channel.SendMessageAsync("Please attach audio file!");
                }
            }
            else
            {
                await message.Channel.SendMessageAsync("Invalid frequency!");
            }
        }

        private async Task HandleStopCommandAsync(Match match, SocketMessage message, SocketGuild guild, DiscordServerOptions serverOptions)
        {
            logger.LogInformation("Process ATIS bot stop command");

            if (double.TryParse(match.Groups[1].Value, out var frequency))
            {
                var channelName = channelMaker.CreateChannelNameFromFrequency(serverOptions, (int)(frequency * 1000));

                if (await atisProcessManager.StopAtisAsync(guild.Id, channelName))
                {
                    await message.Channel.SendMessageAsync("", embed: new EmbedBuilder().WithDescription($"✅ ATIS on channel {channelName} is stopped.").Build());
                }
                else
                {
                    await message.Channel.SendMessageAsync("", embed: new EmbedBuilder().WithDescription($"🟥 Cannot find any running bot on {channelName}!").Build());
                }
            }
            else
            {
                await message.Channel.SendMessageAsync("Invalid frequency!");
            }
        }

        private async Task ProcessBotRequestAsync(SocketMessage message, SocketTextChannel channel, DiscordServerOptions serverOptions, double frequency, string nickname)
        {
            try
            {
                logger.LogInformation("Processing ATIS bot request {guildName} {frequency} {nickname}...", channel.Guild.Name, frequency, nickname);

                var response = await message.Channel.SendMessageAsync("",
                    embed: new EmbedBuilder().WithDescription("⬜ Loading the audio...").Build());

                var filePath = await SaveAudioAsync(message);

                await response.ModifyAsync(props =>
                {
                    props.Embed = new EmbedBuilder().WithDescription("✅ Audio loaded\n⬜ Creating channel...").Build();
                });

                await CreateVoiceChannelAndStartBotAsync(channel.Guild, serverOptions, frequency, nickname, filePath, response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot process ATIS request {guildName} {frequency} {nickname}", channel.Guild.Name, frequency, nickname);
            }
        }

        private async Task CreateVoiceChannelAndStartBotAsync(SocketGuild guild, DiscordServerOptions serverOptions, double frequency, string nickname,
            string filePath, RestUserMessage response = null, ulong? previousChannelId = null, string previousFilePath = null)
        {
            var voiceChannel = await channelMaker.GetOrCreateVoiceChannelAsync(serverOptions, guild, (int)(frequency * 1000));

            if (response != null)
            {
                await response.ModifyAsync(props =>
                {
                    props.Embed = new EmbedBuilder().WithDescription("✅ Audio loaded\n✅ Channel created\n⬜ Activating ATIS bot...").Build();
                });
            }

            try
            {
                // When trying to restore channel on bot reboot, the old channel may already be removed and a new channel is created for the same frequency
                // => Some special handling & cleaning up is required
                await atisProcessManager.StartAtisAsync(voiceChannel, frequency, filePath, nickname, previousChannelId, previousFilePath);

                if (response != null)
                {
                    await response.ModifyAsync(props =>
                    {
                        props.Embed = new EmbedBuilder().WithDescription("✅ Audio loaded\n✅ Channel created\n✅ ATIS Bot activated").Build();
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot start process");

                if (response != null)
                {
                    await response?.ModifyAsync(props =>
                    {
                        props.Embed = new EmbedBuilder().WithDescription($"✅ Audio loaded\n✅ Channel created\n🟥 Failed to create bot. {ex.Message}").Build();
                    });
                }
            }
        }

        private async Task<string> SaveAudioAsync(SocketMessage message)
        {
            var attachment = message.Attachments.First();
            using var httpClient = new HttpClient();
            var stream = await httpClient.GetStreamAsync(attachment.ProxyUrl);
            var extension = Path.GetExtension(attachment.Filename);
            var fileName = Guid.NewGuid().ToString("N") + extension;
            var filePath = Path.Combine(atisOptions.AudioFolder, fileName);

            if (!Directory.Exists(atisOptions.AudioFolder)) Directory.CreateDirectory(atisOptions.AudioFolder);

            using (var fileStream = File.OpenWrite(filePath))
            {
                await stream.CopyToAsync(fileStream);
            }

            return filePath;
        }

        /// <summary>
        /// NOTE: this event can happen both on 1st launch and on Discord reconnection
        /// </summary>
        private async Task BotClient_GuildAvailable(SocketGuild guild)
        {
            var serverOptions = discordOptions.Servers.FirstOrDefault(option => option.ServerId == guild.Id);

            if (serverOptions?.CommandChannelId != null)
            {
                var atisChannels = await atisChannelStorage.GetByGuildAsync(guild.Id);
                logger.LogInformation("Try to recover {channelCount} ATIS channels in {guildName}.", atisChannels.Count(), guild.Name);
                foreach (var atisChannel in atisChannels)
                {
                    try
                    {
                        await CreateVoiceChannelAndStartBotAsync(guild, serverOptions, atisChannel.Frequency, atisChannel.Nickname, atisChannel.FilePath,
                            previousChannelId: atisChannel.ChannelId, previousFilePath: atisChannel.FilePath);
                        logger.LogInformation("Recovered ATIS channels {channelName} in {guildName}.", atisChannel.ChannelName, guild.Name);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Cannot recover ATIS channel [{channelId}] {channelName} in [{guildId}] {guildName}",
                            atisChannel.ChannelId, atisChannel.ChannelName, atisChannel.GuildId, atisChannel.GuildName);
                    }
                }
            }
        }
    }
}
