using System;
using System.Collections.Generic;

namespace FlightEvents.Data
{
    public class EventStopwatch
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public string LeaderboardName { get; set; }
        public string Name { get; set; }
        public DateTimeOffset AddedDateTime { get; set; }
        public DateTimeOffset? StartedDateTime { get; set; }
        public List<DateTimeOffset> LapsDateTime { get; set; } = new List<DateTimeOffset>();
        public DateTimeOffset? StoppedDateTime { get; set; }
    }
}
