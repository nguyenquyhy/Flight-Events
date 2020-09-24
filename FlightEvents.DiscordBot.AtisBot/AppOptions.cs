namespace FlightEvents.DiscordBot.AtisBot
{
    public class AppOptions
    {
        public string BotToken { get; set; }
        public string AudioFilePath { get; set; }
        public ulong ServerId { get; set; }
        public ulong ChannelId { get; set; }
        public string Nickname { get; set; }
    }
}
