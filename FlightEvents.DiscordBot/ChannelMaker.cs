﻿using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace FlightEvents.DiscordBot
{
    public class ChannelMaker
    {
        private readonly ILogger<ChannelMaker> logger;

        public ChannelMaker(ILogger<ChannelMaker> logger)
        {
            this.logger = logger;
        }

        public async Task<IGuildChannel> GetOrCreateVoiceChannelAsync(DiscordServerOptions serverOptions, SocketGuild guild, int? frequency)
        {
            var channelName = frequency.HasValue ?
                CreateChannelNameFromFrequency(serverOptions, frequency) :
                serverOptions.LoungeChannelName;

            if (!string.IsNullOrWhiteSpace(channelName))
            {
                var channel = guild.Channels.FirstOrDefault(c => c.Name == channelName);
                if (channel != null) return channel;

                var voiceChannel = await guild.CreateVoiceChannelAsync(channelName, props =>
                {
                    props.CategoryId = serverOptions.ChannelCategoryId;
                    if (serverOptions.ChannelBitrate.HasValue)
                    {
                        props.Bitrate = serverOptions.ChannelBitrate.Value;
                    }
                });

                // Note: Bot will not try to add permission.
                // Instead, permission should be set at the category level so that the channel can inherit.

                logger.LogInformation("Created new channel {channelName}", channelName);

                return voiceChannel;
            }
            else if (serverOptions.LoungeChannelId.HasValue)
            {
                return guild.Channels.FirstOrDefault(c => c.Id == serverOptions.LoungeChannelId.Value);
            }
            else
            {
                return null;
            }
        }

        public string CreateChannelNameFromFrequency(DiscordServerOptions serverOptions, int? toFrequency)
            => (toFrequency.Value / 1000d).ToString("N3") + (serverOptions.ChannelNameSuffix ?? "");
    }
}
