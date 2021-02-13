using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightEvents.Data
{
    public class AtisChannel
    {
        public ulong GuildId { get; set; }
        public string GuildName { get; set; }
        public ulong ChannelId { get; set; }
        public string ChannelName { get; set; }
        public double Frequency { get; set; }
        public string BotToken { get; set; }

        public string FilePath { get; set; }
        public string Nickname { get; set; }
        public int ProcessId { get; set; }

    }

    public interface IAtisChannelStorage
    {
        Task<AtisChannel> GetByChannelNameAsync(ulong guildId, string channelName);
        Task<IEnumerable<AtisChannel>> GetByGuildAsync(ulong guildId);
        Task<AtisChannel> AddChannelAsync(AtisChannel atisChannel);
        Task RemoveAsync(ulong guildId, ulong channelId);
    }
}
