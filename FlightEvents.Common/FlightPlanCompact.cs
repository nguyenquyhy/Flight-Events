using System;
using System.Linq;

namespace FlightEvents
{
    public class FlightPlanCompact
    {
        public FlightPlanCompact()
        {

        }

        public FlightPlanCompact(FlightPlanData flightPlan, string callsign, string aircraftType, int? estimatedCruisingSpeed)
        {
            Callsign = callsign;
            AircraftType = aircraftType;

            Type = flightPlan.Type;
            RouteType = flightPlan.RouteType;
            CruisingAltitude = flightPlan.CruisingAltitude;
            Departure = flightPlan.Departure?.ID;
            Destination = flightPlan.Destination?.ID;
            Route = flightPlan.Waypoints == null ? null : string.Join(" ", flightPlan.Waypoints.Where(o => o.Id != "TIMECRUIS" && o.Id != "TIMEDSCNT").Select(o => o.Id));

            CruisingSpeed = estimatedCruisingSpeed;
            if (estimatedCruisingSpeed != null && estimatedCruisingSpeed != 0 && flightPlan.Waypoints != null && flightPlan.Waypoints.Count() > 1)
            {
                var dist = 0d;
                for (var i = 1; i < flightPlan.Waypoints.Count(); i++)
                {
                    var p1 = flightPlan.Waypoints.ElementAt(i - 1);
                    var p2 = flightPlan.Waypoints.ElementAt(i);
                    dist += GpsHelper.CalculateDistance(p1.Latitude, p1.Longitude, p2.Latitude, p2.Longitude);
                }
                EstimatedEnroute = TimeSpan.FromHours(dist / estimatedCruisingSpeed.Value);
            }
        }

        public string Callsign { get; set; }
        public string AircraftType { get; set; }

        public string Type { get; set; }
        public string RouteType { get; set; }
        public int CruisingAltitude { get; set; }
        public int? CruisingSpeed { get; set; }
        public TimeSpan? EstimatedEnroute { get; set; }

        public string Departure { get; set; }
        public string Destination { get; set; }

        public string Route { get; set; }
    }
}
