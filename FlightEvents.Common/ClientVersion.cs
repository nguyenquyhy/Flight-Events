using System.Collections.Generic;

namespace FlightEvents
{
    public class ClientVersion
    {
        public string Version { get; set; }
        public List<ClientAnnouncement> Announcements { get; set; } = new List<ClientAnnouncement>();
        public ClientFeatures Features { get; set; } = new ClientFeatures();
    }

    public class ClientAnnouncement
    {
        public string Content { get; set; }
    }

    public class ClientFeatures
    {
        public bool UseWebpack { get; set; } = true;
    }
}
