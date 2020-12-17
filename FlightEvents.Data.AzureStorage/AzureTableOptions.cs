using System.ComponentModel.DataAnnotations;

namespace FlightEvents.Data
{
    public class AzureTableOptions
    {
        [Required]
        public string ConnectionString { get; set; }
        [Required]
        public string DiscordConnectionTable { get; set; }
        [Required]
        public string LeaderboardTable { get; set; }
        [Required]
        public string UserTable { get; set; }
    }
}
