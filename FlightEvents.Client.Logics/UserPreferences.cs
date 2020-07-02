using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FlightEvents.Client.Logics
{
    public class UserPreferences
    {
        public string LastCallsign { get; set; }
        public string ClientId { get; set; }
        public bool DisableDiscordRP { get; set; }
        public bool BroadcastUDP { get; set; }
        public string BroadcastIP { get; set; }
    }
}
