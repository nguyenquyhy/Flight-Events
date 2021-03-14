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
