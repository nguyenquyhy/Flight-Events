using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FlightEvents.DiscordBot.AtisBot
{
    public class AtisService : BackgroundService
    {
        private readonly ILogger<AtisService> logger;
        private readonly AppOptions appOptions;
        private DiscordSocketClient discord;

        public AtisService(ILogger<AtisService> logger, IOptionsMonitor<AppOptions> appOptions)
        {
            this.logger = logger;
            this.appOptions = appOptions.CurrentValue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                discord = new DiscordSocketClient();

                discord.GuildAvailable += Discord_GuildAvailable;

                await discord.LoginAsync(TokenType.Bot, appOptions.BotToken);
                await discord.StartAsync();
                logger.LogInformation("Started Discord");

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot initialize AtisService");
            }
        }

        private async Task Discord_GuildAvailable(SocketGuild guild)
        {
            logger.LogInformation("Connected to {guildId} {guildName}", guild.Id, guild.Name);
            if (guild.Id == appOptions.ServerId)
            {
                var channel = guild.GetVoiceChannel(appOptions.ChannelId);

                try
                {
                    if (!string.IsNullOrWhiteSpace(appOptions.Nickname))
                    {
                        if (guild.CurrentUser.Nickname != appOptions.Nickname)
                        {
                            logger.LogInformation("Changing nickname to {nickname}", appOptions.Nickname);
                            await guild.CurrentUser.ModifyAsync(props =>
                            {
                                props.Nickname = appOptions.Nickname;
                            });
                        }
                    }
                    else
                    {
                        if (guild.CurrentUser != null)
                        {
                            logger.LogInformation("Clear nickname");
                            await guild.CurrentUser.ModifyAsync(props =>
                            {
                                props.Nickname = appOptions.Nickname;
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Cannot change nick name to {nickname} in guild {guildName}", appOptions.Nickname, guild.Name);
                }

                if (channel != null)
                {
                    logger.LogInformation("Start loop on channel {channelId}", appOptions.ChannelId);
                    Task.Run(async () =>
                    {
                        await PlayLoopAsync(channel);
                    });
                }
                else
                {
                    logger.LogError("Cannot find channel {channelId}", appOptions.ChannelId);
                }
            }
        }

        private async Task PlayLoopAsync(SocketVoiceChannel voiceChannel)
        {
            using var stream = new MemoryStream();

            using (var ffmpeg = CreateStream(appOptions.AudioFilePath))
            using (var output = ffmpeg.StandardOutput.BaseStream)
            {
                await output.CopyToAsync(stream);
            }
            File.Delete(appOptions.AudioFilePath);
            logger.LogInformation("Read & deleted file {fileName}", appOptions.AudioFilePath);

            try
            {
                var audioClient = await voiceChannel.ConnectAsync();
                logger.LogInformation("Connected to voice channel {channelId} {channelName}", voiceChannel.Id, voiceChannel.Name);

                while (true)
                {
                    logger.LogInformation("Start playing audio");
                    using var discordStream = audioClient.CreatePCMStream(AudioApplication.Voice);
                    try
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        await stream.CopyToAsync(discordStream);
                    }
                    finally
                    {
                        await discordStream.FlushAsync();
                    }
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot play ATIS message");
            }
        }

        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }
    }
}
