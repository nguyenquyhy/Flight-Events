using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightEvents.Data
{
    public class DiscordServer
    {
        public ulong ServerId { get; set; }
        public ulong ChannelCategoryId { get; set; }
        public ulong? CommandChannelId { get; set; }

        public string LoungeChannelName { get; set; }
        public ulong? LoungeChannelId { get; set; }

        public ulong[] ExternalChannelIds { get; set; }

        public int? ChannelBitrate { get; set; }
        public string ChannelNameSuffix { get; set; }

        /// <summary>
        /// Restrict the role that can use !finfo
        /// </summary>
        public ulong? FlightInfoRoleId { get; set; }

        public string Remarks { get; set; }
    }

    public interface IDiscordServerStorage
    {
        Task<List<DiscordServer>> GetDiscordServersAsync();
        Task<DiscordServer> AddDiscordServersAsync(DiscordServer discordServer);
    }
}
