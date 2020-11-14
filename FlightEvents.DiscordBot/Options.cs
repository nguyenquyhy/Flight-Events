using System.ComponentModel.DataAnnotations;

namespace FlightEvents.DiscordBot
{

    public class AppOptions
    {
        [Required]
        public string WebServerUrl { get; set; }
    }

    public class DiscordOptions
    {
        [Required]
        public string BotToken { get; set; }
        [Required]
        public DiscordServerOptions[] Servers { get; set; }
    }

    public class DiscordServerOptions
    {
        [Required]
        public ulong ServerId { get; set; }
        [Required]
        public ulong ChannelCategoryId { get; set; }
        public ulong? CommandChannelId { get; set; }

        public string LoungeChannelName { get; set; }
        public ulong? LoungeChannelId { get; set; }

        public ulong[] ExternalChannelIds { get; set; }

        [Required]
        public int ChannelBitrate { get; set; }
        public string ChannelNameSuffix { get; set; }

        /// <summary>
        /// Restrict the role that can use !finfo
        /// </summary>
        public ulong? FlightInfoRoleId { get; set; }
    }

    public class AtisOptions
    {
        public string[] BotTokens { get; set; }
        [Required]
        public string AudioFolder { get; set; }
        [Required]
        public string BotExecutionPath { get; set; }
    }

}
