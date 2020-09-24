using Discord;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private readonly Dictionary<ulong, AtisGuildRecord> memory = new Dictionary<ulong, AtisGuildRecord>();

        private readonly ILogger<AtisProcessManager> logger;
        private readonly AtisOptions atisOptions;

        public AtisProcessManager(
            ILogger<AtisProcessManager> logger,
            IOptionsMonitor<AtisOptions> atisOptions)
        {
            this.logger = logger;
            this.atisOptions = atisOptions.CurrentValue;
        }

        private static readonly SemaphoreSlim sm = new SemaphoreSlim(1);

        public async Task StartAtisAsync(IGuildChannel channel, string filePath, string nickname)
        {
            try
            {
                await sm.WaitAsync();


                if (!memory.ContainsKey(channel.GuildId))
                {
                    memory.Add(channel.GuildId, new AtisGuildRecord());
                }
                var guildData = memory[channel.GuildId];

                var botToken = FindToken(channel, guildData);

                StartProcess(channel, filePath, botToken, guildData, nickname);
            }
            finally
            {
                sm.Release();
            }
        }

        public async Task<bool> StopAtisAsync(ulong serverId, string channelName)
        {
            try
            {
                await sm.WaitAsync();
                if (memory.TryGetValue(serverId, out var guildData))
                {
                    if (guildData.BotTokenByChannelName.TryGetValue(channelName, out var botToken))
                    {
                        if (guildData.ProcessesByToken.TryGetValue(botToken, out var process))
                        {
                            process.Kill();
                            guildData.BotTokenByChannelName.Remove(channelName);
                            guildData.ProcessesByToken.Remove(botToken);
                            return true;
                        }
                    }
                }
            }
            finally
            {
                sm.Release();
            }
            return false;
        }

        private string FindToken(IGuildChannel channel, AtisGuildRecord guildData)
        {
            if (guildData.BotTokenByChannelName.ContainsKey(channel.Name))
            {
                // If the channel is already handled by a bot, restart to change audio
                var token = guildData.BotTokenByChannelName[channel.Name];
                if (guildData.ProcessesByToken.TryGetValue(token, out var currentProcess))
                {
                    currentProcess.Kill();
                    logger.LogDebug("Kill existing process");
                    guildData.ProcessesByToken.Remove(token);
                }
                return token;
            }
            else
            {
                // Find available token
                string availableToken = null;
                foreach (var token in atisOptions.BotTokens)
                {
                    if (!guildData.ProcessesByToken.ContainsKey(token))
                    {
                        availableToken = token;
                        break;
                    }
                }

                if (availableToken == null)
                {
                    throw new Exception("Cannot find any available ATIS bot! Please try to stop one first.");
                }

                return availableToken;
            }
        }

        private void StartProcess(IGuildChannel channel, string filePath, string botToken, AtisGuildRecord guildData, string nickname)
        {
            var arguments = $"--BotToken {botToken} --AudioFilePath {filePath} --ServerId {channel.Guild.Id} --ChannelId {channel.Id} --Nickname \"{nickname}\"";
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

            guildData.ProcessesByToken.TryAdd(botToken, process);
            guildData.BotTokenByChannelName.TryAdd(channel.Name, botToken);

            //if (!process.HasExited)
            //{
            //    process.WaitForExit();
            //}
        }
    }
}
