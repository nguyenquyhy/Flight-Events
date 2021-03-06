﻿using System.ComponentModel.DataAnnotations;

namespace FlightEvents.Data
{
    public class AzureTableBaseOptions
    {
        [Required]
        public string ConnectionString { get; set; }
    }

    public class AzureTableDiscordOptions : AzureTableBaseOptions
    {
        [Required]
        public string DiscordConnectionTable { get; set; }
        [Required]
        public string DiscordServerTable { get; set; }
    }

    public class AzureTableLeaderboardOptions : AzureTableBaseOptions
    {
        [Required]
        public string LeaderboardTable { get; set; }
    }

    public class AzureTableUserOptions : AzureTableBaseOptions
    {
        [Required]
        public string UserTable { get; set; }
    }

    public class AzureTableAtisChannelOptions : AzureTableBaseOptions
    {
        [Required]
        public string AtisChannelTable { get; set; }
    }
}
