using Discord;
using Discord.WebSocket;
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
        private readonly AtisOptions atisOptions;
        private readonly DiscordOptions discordOptions;

        public AtisWorker(ILogger<AtisWorker> logger,
            IOptionsMonitor<AtisOptions> atisOptions,
            IOptionsMonitor<DiscordOptions> discordOptions,
            ChannelMaker channelMaker,
            RegexMatcher regexMatcher,
            AtisProcessManager atisProcessManager)
        {
            this.logger = logger;
            this.channelMaker = channelMaker;
            this.regexMatcher = regexMatcher;
            this.atisProcessManager = atisProcessManager;
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

                var voiceChannel = await channelMaker.CreateVoiceChannelAsync(serverOptions, channel.Guild, (int)(frequency * 1000));

                await response.ModifyAsync(props =>
                {
                    props.Embed = new EmbedBuilder().WithDescription("✅ Audio loaded\n✅ Channel created\n⬜ Activating ATIS bot...").Build();
                });

                try
                {
                    await atisProcessManager.StartAtisAsync(voiceChannel, filePath, nickname);

                    await response.ModifyAsync(props =>
                    {
                        props.Embed = new EmbedBuilder().WithDescription("✅ Audio loaded\n✅ Channel created\n✅ ATIS Bot activated").Build();
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Cannot start process");

                    await response.ModifyAsync(props =>
                    {
                        props.Embed = new EmbedBuilder().WithDescription($"✅ Audio loaded\n✅ Channel created\n🟥 Failed to create bot. {ex.Message}").Build();
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot process ATIS request {guildName} {frequency} {nickname}", channel.Guild.Name, frequency, nickname);
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

        private async Task BotClient_GuildAvailable(SocketGuild guild)
        {

        }
    }
}
