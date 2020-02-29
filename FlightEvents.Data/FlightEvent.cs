using System;
using System.Collections.Generic;

namespace FlightEvents.Data
{
    public class FlightEvent
    {
        public Guid Id { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public DateTimeOffset UpdatedDateTime { get; set; }

        public DateTimeOffset StartDateTime { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }

        public string Waypoints { get; set; }
        public string Route { get; set; }

        public List<string> FlightPlanIds { get; set; }

        public void UpdateTo(FlightEvent current)
        {
            if (StartDateTime != default) current.StartDateTime = StartDateTime;
            if (Name != default) current.Name = Name;
            if (Description != default) current.Description = Description;
            if (Url != default) current.Url = Url;

            if (Waypoints != default) current.Waypoints = Waypoints;
            if (Route != default) current.Route = Route;

            if (FlightPlanIds != default) current.FlightPlanIds = FlightPlanIds;
        }
    }
}
