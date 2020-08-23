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
        [Required]
        public string LoungeChannelName { get; set; }
        [Required]
        public int ChannelBitrate { get; set; }
        public string ChannelNameSuffix { get; set; }

        /// <summary>
        /// Restrict the role that can use !finfo
        /// </summary>
        public ulong? FlightInfoRoleId { get; set; }
    }

}
