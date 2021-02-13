using Discord;
using FlightEvents.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FlightEvents.DiscordBot
{
    public class AtisGuildRecord
    {
        public Dictionary<string, Process> ProcessesByToken { get; } = new Dictionary<string, Process>();
        public Dictionary<string, string> BotTokenByChannelName { get; } = new Dictionary<string, string>();
    }

    public class AtisProcessManager
    {
        private readonly ILogger<AtisProcessManager> logger;
        private readonly IAtisChannelStorage atisChannelStorage;
        private readonly AtisOptions atisOptions;

        private readonly ConcurrentDictionary<string, Process> cachedProcesses = new ConcurrentDictionary<string, Process>();
        private static readonly SemaphoreSlim sm = new SemaphoreSlim(1);

        public AtisProcessManager(
            ILogger<AtisProcessManager> logger,
            IOptionsMonitor<AtisOptions> atisOptions,
            IAtisChannelStorage atisChannelStorage)
        {
            this.logger = logger;
            this.atisChannelStorage = atisChannelStorage;
            this.atisOptions = atisOptions.CurrentValue;
        }

        public async Task StartAtisAsync(IGuildChannel channel, double frequency, string filePath, string nickname, ulong? previousChannelId, string previousFilePath)
        {
            await sm.WaitAsync();
            try
            {
                var botToken = await GetAvailableBotTokenAsync(channel, filePath == previousFilePath);

                if (botToken != null)
                {
                    var process = StartProcess(channel.GuildId, channel.Id, filePath, botToken, nickname);

                    await atisChannelStorage.AddChannelAsync(new AtisChannel
                    {
                        GuildId = channel.GuildId,
                        GuildName = channel.Guild.Name,
                        ChannelId = channel.Id,
                        ChannelName = channel.Name,
                        Frequency = frequency,
                        BotToken = botToken,
                        FilePath = filePath,
                        Nickname = nickname,
                        ProcessId = process.Id
                    });

                    if (previousChannelId.HasValue && previousChannelId != channel.Id)
                    {
                        logger.LogInformation("Removing deleted ATIS channel [{channelId}] {channelName} from storage of guild [{guildId}] {guildName}",
                            previousChannelId, channel.Name, channel.GuildId, channel.Guild.Name);
                        await atisChannelStorage.RemoveAsync(channel.GuildId, previousChannelId.Value);
                    }
                }
            }
            finally
            {
                sm.Release();
            }
        }

        public async Task<bool> StopAtisAsync(ulong serverId, string channelName)
        {
            await sm.WaitAsync();
            try
            {
                var atisChannel = await atisChannelStorage.GetByChannelNameAsync(serverId, channelName);
                if (atisChannel != null)
                {
                    var cacheKey = atisChannel.GuildId + ":" + atisChannel.ChannelId;
                    if (cachedProcesses.TryGetValue(cacheKey, out var process))
                    {
                        await TerminateProcessAndCleanUpAsync(process, atisChannel);
                        return true;
                    }
                }
            }
            finally
            {
                sm.Release();
            }
            return false;
        }

        private async Task<string> GetAvailableBotTokenAsync(IGuildChannel channel, bool sameFile)
        {
            var atis = await atisChannelStorage.GetByChannelNameAsync(channel.GuildId, channel.Name);
            if (atis != null)
            {
                // If the channel is already handled by a bot, restart to change audio and return current token
                if (cachedProcesses.TryGetValue(channel.GuildId + ":" + channel.Id, out var currentProcess))
                {
                    if (sameFile)
                    {
                        // If the request is for the same file, and there is already a running process, we can skip creating new one
                        logger.LogInformation("Process {processId} is already playing {filePath}.", currentProcess.Id, atis.FilePath);
                        return null;
                    }
                    else
                    {
                        await TerminateProcessAndCleanUpAsync(currentProcess, atis);
                    }
                }
                return atis.BotToken;
            }
            else
            {
                // Find available token
                var atisChannels = await atisChannelStorage.GetByGuildAsync(channel.GuildId);
                var usedTokens = atisChannels.Select(o => o.BotToken).ToList();

                var availableToken = atisOptions.BotTokens.Except(usedTokens).FirstOrDefault();

                if (availableToken == null)
                {
                    throw new Exception("Cannot find any available ATIS bot! Please try to stop one first.");
                }

                return availableToken;
            }
        }

        private async Task TerminateProcessAndCleanUpAsync(Process currentProcess, AtisChannel atisChannel)
        {
            var processId = currentProcess.Id;
            currentProcess.Kill();
            currentProcess.Dispose();
            cachedProcesses.Remove(atisChannel.GuildId + ":" + atisChannel.ChannelId, out _);
            logger.LogInformation("Killed existing process {processId}", processId);

            try
            {
                File.Delete(atisChannel.FilePath);
                logger.LogInformation("Deleted file {filePath}", atisChannel.FilePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot deleted file {filePath}", atisChannel.FilePath);
            }

            await atisChannelStorage.RemoveAsync(atisChannel.GuildId, atisChannel.ChannelId);
        }

        private Process StartProcess(ulong guildId, ulong channelId, string filePath, string botToken, string nickname)
        {
            var arguments = $"--BotToken {botToken} --AudioFilePath {filePath} --ServerId {guildId} --ChannelId {channelId} --Nickname \"{nickname}\"";
            logger.LogDebug("Launching Bot {filePath} {arguments}", atisOptions.BotExecutionPath, arguments);
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = atisOptions.BotExecutionPath,
                    WorkingDirectory = Path.GetDirectoryName(atisOptions.BotExecutionPath),
                    Arguments = arguments
                }
            };

            process.Start();

            cachedProcesses.TryAdd(guildId + ":" + channelId, process);

            //if (!process.HasExited)
            //{
            //    process.WaitForExit();
            //}

            return process;
        }
    }
}
