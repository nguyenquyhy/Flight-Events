using FlightEvents.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FlightEvents.Web.Logics
{
    public interface IRaceManager
    {
        Task<(Racer racer, bool crossedCheckpoint)> UpdatePositionAsync(string callsign, double latitude, double longitude);
    }

    public class RaceManager : IRaceManager
    {
        private const double CheckpointRadiusInNm = 0.0539957 * 5;
        private readonly ILogger<RaceManager> logger;
        private readonly IRaceStorage storage;
        private readonly IFlightEventStorage flightEventStorage;
        private readonly IFlightPlanFileStorage flightPlanFileStorage;

        public RaceManager(ILogger<RaceManager> logger, IRaceStorage raceStorage, IFlightEventStorage flightEventStorage, IFlightPlanFileStorage flightPlanFileStorage)
        {
            this.logger = logger;
            this.storage = raceStorage;
            this.flightEventStorage = flightEventStorage;
            this.flightPlanFileStorage = flightPlanFileStorage;
        }

        public async Task<(Racer racer, bool crossedCheckpoint)> UpdatePositionAsync(string callsign, double latitude, double longitude)
        {
            var (racer, time) = await storage.GetRacerAsync(callsign);

            if (racer != null)
            {
                var previousLatitude = racer.Latitude;
                var previousLongitude = racer.Longitude;

                await storage.UpdateAsync(callsign, latitude, longitude);

                if (previousLatitude.HasValue && previousLongitude.HasValue)
                {
                    var nextCheckpoint = racer.CheckpointTimes.Count;
                    if (await CrossCheckpointAsync(racer.EventId, nextCheckpoint, CheckpointRadiusInNm, previousLatitude.Value, previousLongitude.Value, latitude, longitude))
                    {
                        await storage.UpdateTimeAsync(callsign, nextCheckpoint, time);

                        return (racer, true);
                    }
                }
                return (racer, false);
            }

            return (null, false);
        }

        private async Task<bool> CrossCheckpointAsync(Guid eventId, int checkpointIndex, double radius, double previousLatitude, double previousLongitude, double latitude, double longitude)
        {
            // Calculate checkpoint line
            var evt = await flightEventStorage.GetAsync(eventId);
            if (evt.FlightPlanIds.Count == 0) throw new InvalidOperationException("Cannot get checkpoint without a flight plan.");
            var flightPlan = await flightPlanFileStorage.GetFlightPlanAsync(evt.FlightPlanIds[0]);

            if (evt.MarkedWaypoints == null || checkpointIndex == evt.MarkedWaypoints.Count) return true;

            var waypoints = flightPlan.Waypoints.ToList();

            var waypointIndex = waypoints.FindIndex(o => o.Id?.Trim() == evt.MarkedWaypoints[checkpointIndex]);

            var checkpointLatitudeStart = waypoints[waypointIndex].Latitude;
            var checkpointLongitudeStart = waypoints[waypointIndex].Longitude;
            var checkpointLatitudeEnd = waypoints[waypointIndex].Latitude;
            var checkpointLongitudeEnd = waypoints[waypointIndex].Longitude;

            var (deltaLatitude, deltaLongitude) = GpsHelper.CalculatePerpendicular(
                waypoints[waypointIndex - 1].Latitude, waypoints[waypointIndex - 1].Longitude,
                waypoints[waypointIndex].Latitude, waypoints[waypointIndex].Longitude,
                radius);

            checkpointLatitudeStart -= deltaLatitude;
            checkpointLongitudeStart -= deltaLongitude;
            checkpointLatitudeEnd += deltaLatitude;
            checkpointLongitudeEnd += deltaLongitude;

            var (x1, y1, _) = GpsHelper.ToXyz(checkpointLatitudeStart, checkpointLongitudeStart, 0);
            var (x2, y2, _) = GpsHelper.ToXyz(checkpointLatitudeEnd, checkpointLongitudeEnd, 0);
            var (x3, y3, _) = GpsHelper.ToXyz(previousLatitude, previousLongitude, 0);
            var (x4, y4, _) = GpsHelper.ToXyz(latitude, longitude, 0);

            return GpsHelper.LineSegmentsIntersect(
                new Vector(x1, y1), new Vector(x2, y2),
                new Vector(x3, y3), new Vector(x4, y4),
                out _);
        }

    }
}
