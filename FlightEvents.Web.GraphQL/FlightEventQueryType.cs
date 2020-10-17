using FlightEvents.Data;
using HotChocolate;
using HotChocolate.Types;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlightEvents.Web.GraphQL
{
    public class FlightEventQueryType : ObjectType<FlightEvent>
    {
        protected override void Configure(IObjectTypeDescriptor<FlightEvent> descriptor)
        {
            descriptor.Field(o => o.FlightPlanIds).Ignore();
            descriptor.Field<FlightPlansResolver>(t => t.GetFlightPlans(default)).Type<ListType<FlightPlanQueryType>>();
            descriptor.Field<CheckpointsResolver>(t => t.GetCheckpointsAsync(default, default));
        }
    }

    public class FlightPlansResolver
    {
        public IEnumerable<string> GetFlightPlans([Parent] FlightEvent flightEvent) => flightEvent.FlightPlanIds ?? new List<string>();
    }

    public class CheckpointsResolver
    {
        public async Task<List<FlightPlanWaypoint>> GetCheckpointsAsync([Parent] FlightEvent @event, [Service] IFlightPlanFileStorage flightPlanFileStorage)
        {
            //var @event = await flightEventStorage.GetAsync(id);
            if (@event.FlightPlanIds == null || @event.FlightPlanIds.Count == 0) return null;
            if (@event.MarkedWaypoints == null || @event.MarkedWaypoints.Count == 0) return new List<FlightPlanWaypoint>();

            var flightPlan = await flightPlanFileStorage.GetFlightPlanAsync(@event.FlightPlanIds[0]);

            var result = new List<FlightPlanWaypoint>();
            var waypoints = flightPlan.Waypoints.ToList();
            foreach (var waypointId in @event.MarkedWaypoints)
            {
                var waypointIndex = waypoints.FindIndex(o => o.Id.Trim() == waypointId);
                var (deltaLatitude, deltaLongitude) = GpsHelper.CalculatePerpendicular(
                    waypoints[waypointIndex - 1].Latitude, waypoints[waypointIndex - 1].Longitude,
                    waypoints[waypointIndex].Latitude, waypoints[waypointIndex].Longitude, 0.0539957 * 5);
                result.Add(new FlightPlanWaypoint { Latitude = waypoints[waypointIndex].Latitude - deltaLatitude, Longitude = waypoints[waypointIndex].Longitude - deltaLongitude });
                result.Add(new FlightPlanWaypoint { Latitude = waypoints[waypointIndex].Latitude + deltaLatitude, Longitude = waypoints[waypointIndex].Longitude + deltaLongitude });
            }

            return result;
        }
    }
}
