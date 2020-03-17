namespace FlightEvents
{

    public class DiscordConnection
    {
        public ulong UserId { get; set; }
        public string Username { get; set; }
        public string Discriminator { get; set; }

        public string DisplayName => $"{Username}#{Discriminator}";
    }
}
