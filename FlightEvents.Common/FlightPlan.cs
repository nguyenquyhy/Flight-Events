using HotChocolate;
using System.Collections.Generic;

namespace FlightEvents
{
    public class FlightPlanData
    {
        public string Title { get; set; }
        public string Description { get; set; }

        public string Type { get; set; }
        public string RouteType { get; set; }
        public int CruisingAltitude { get; set; }

        public FlightPlanPosition Departure { get; set; }
        public FlightPlanPosition Destination { get; set; }

        public IEnumerable<FlightPlanWaypoint> Waypoints { get; set; }
    }

    public class FlightPlanPosition
    {
        [GraphQLName("id")]
        public string ID { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Altitude { get; set; }
        public string Name { get; set; }
    }

    public partial class FlightPlanWaypoint
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Altitude { get; set; }
        public string Airway { get; set; }
        [GraphQLName("icao")]
        public WaypointICAO ICAO { get; set; }
    }

    public partial class WaypointICAO
    {
        public string Region { get; set; }
        public string Ident { get; set; }
        public string Airport { get; set; }
    }
}
