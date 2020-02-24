using System;

namespace FlightEvents.Data
{
    public class FlightEvent
    {
        public Guid Id { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }

        public DateTimeOffset StartDateTime { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Route { get; set; }
        public string Description { get; set; }
    }
}
