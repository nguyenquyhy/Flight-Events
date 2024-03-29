﻿using Discord;
using Discord.Audio;
using Discord.WebSocket;
using FlightEvents.Bots.Logics;
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

        private CancellationTokenSource cts = null;
        /// <summary>
        /// The audio stream is cached so that it can be reused on reconnection.
        /// </summary>
        private Stream stream = null;

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
                discord.VoiceServerUpdated += Discord_VoiceServerUpdated;
                discord.Disconnected += Discord_Disconnected;
                discord.Connected += Discord_Connected;

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

        private Task Discord_Connected()
        {
            logger.LogInformation("Connected to Discord");

            return Task.CompletedTask;
        }

        private Task Discord_Disconnected(Exception error)
        {
            logger.LogInformation(error, "Disconnected from Discord");

            cts?.Cancel();

            return Task.CompletedTask;
        }

        private Task Discord_VoiceServerUpdated(SocketVoiceServer server)
        {
            logger.LogInformation("Voice server updated to {endpoint}", server.Endpoint);

            return Task.CompletedTask;
        }

        private async Task Discord_GuildAvailable(SocketGuild guild)
        {
            logger.LogInformation("Connected to {guildId} {guildName}", guild.Id, guild.Name);
            if (guild.Id == appOptions.ServerId)
            {
                cts?.Cancel();
                cts = new CancellationTokenSource();
                var token = cts.Token;

                var channel = guild.GetVoiceChannel(appOptions.ChannelId);
                if (channel != null)
                {
                    await SetNicknameAsync(guild);

                    logger.LogInformation("Start loop on channel {channelId} {channelName} of {guildName}", appOptions.ChannelId, channel.Name, guild.Name);
                    PlayLoopAsync(channel, token).RunInThreadPool(logger);
                }
                else
                {
                    logger.LogError("Cannot find channel {channelId}", appOptions.ChannelId);
                }
            }
        }

        private async Task SetNicknameAsync(SocketGuild guild)
        {
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
        }

        private async Task PlayLoopAsync(SocketVoiceChannel voiceChannel, CancellationToken cancellationToken)
        {
            if (stream == null)
            {
                if (!File.Exists(appOptions.AudioFilePath))
                {
                    logger.LogError("Cannot find file {filePath}!", appOptions.AudioFilePath);
                    return;
                }

                stream = new MemoryStream();

                using (var ffmpeg = CreateStream(appOptions.AudioFilePath))
                using (var output = ffmpeg.StandardOutput.BaseStream)
                {
                    await output.CopyToAsync(stream);
                }
                // NOTE: let FE bot handle cleaning up the file
                //File.Delete(appOptions.AudioFilePath);
                logger.LogInformation("Read file {fileName}", appOptions.AudioFilePath);
            }
            else
            {
                logger.LogInformation("Use cached stream of {byteCount} bytes", stream.Length);
            }

            using var audioClient = await voiceChannel.ConnectAsync();
            logger.LogInformation("Connected to voice channel {channelId} {channelName}", voiceChannel.Id, voiceChannel.Name);

            try
            {
                using var discordStream = audioClient.CreatePCMStream(AudioApplication.Voice);
                logger.LogInformation("Created Discord audio stream");

                while (true)
                {
                    logger.LogDebug("Start playing audio");
                    try
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        await stream.CopyToAsync(discordStream, cancellationToken);
                    }
                    finally
                    {
                        await discordStream.FlushAsync();
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(1000);
                }
            }
            catch (TaskCanceledException)
            {
                logger.LogInformation("Audio loop is canceled");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot play ATIS message");
            }
            finally
            {
                await audioClient.StopAsync();
                logger.LogInformation("Stopped voice channel {channelId} {channelName}", voiceChannel.Id, voiceChannel.Name);
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
