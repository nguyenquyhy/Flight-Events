using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace FlightEvents.Web.Logics
{
    public class Racer
    {
        public Guid EventId { get; set; }
        public string Callsign { get; set; }
        public double? Longitude { get; set; }
        public double? Latitude { get; set; }
        /// <summary>
        /// Checkout time in milliseconds
        /// </summary>
        public List<long> CheckpointTimes { get; set; }
    }

    public class Race
    {
        public Guid Id { get; set; }
    }

    public interface IRaceStorage
    {
        Task<(Racer racer, long time)> GetRacerAsync(string callsign);
        Task<IEnumerable<Racer>> GetRacersAsync(Guid id);
        Task InitializeRaceAsync(Guid id, IEnumerable<string> callsigns);
        Task RemoveAsync(Guid id);
        Task UpdateAsync(string callsign, double latitude, double longitude);
        Task UpdateTimeAsync(string callsign, int checkpoint, long time);
    }

    public class RaceStorage : IRaceStorage
    {
        private readonly ConcurrentDictionary<Guid, Stopwatch> stopwatches = new ConcurrentDictionary<Guid, Stopwatch>();
        private readonly ConcurrentDictionary<string, Racer> racers = new ConcurrentDictionary<string, Racer>();
        private readonly ConcurrentDictionary<Guid, List<string>> racersInEvent = new ConcurrentDictionary<Guid, List<string>>();

        public Task InitializeRaceAsync(Guid id, IEnumerable<string> callsigns)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            if (!stopwatches.TryAdd(id, stopwatch))
            {
                stopwatch.Stop();
            }

            var currentCallsigns = racersInEvent.GetOrAdd(id, new List<string>());
            foreach (var callsign in callsigns)
            {
                if (!racers.TryGetValue(callsign, out _))
                {
                    // Assume everyone start at 0
                    var racer = new Racer
                    {
                        EventId = id,
                        Callsign = callsign,
                        CheckpointTimes = new List<long> { 0 }
                    };
                    racers.TryAdd(callsign, racer);
                }
                if (!currentCallsigns.Contains(callsign))
                {
                    currentCallsigns.Add(callsign);
                }
            }

            return Task.CompletedTask;
        }

        public Task<(Racer racer, long time)> GetRacerAsync(string callsign)
        {
            if (racers.TryGetValue(callsign, out var racer))
            {
                var stopwatch = stopwatches[racer.EventId];
                return Task.FromResult((racer, stopwatch.ElapsedMilliseconds));
            }
            return Task.FromResult(((Racer)null, 0L));
        }

        public Task<IEnumerable<Racer>> GetRacersAsync(Guid id)
        {
            if (racersInEvent.TryGetValue(id, out var callsigns))
            {
                var result = new List<Racer>();
                foreach (var callsign in callsigns)
                {
                    if (racers.TryGetValue(callsign, out var racer))
                    {
                        result.Add(racer);
                    }
                }
                return Task.FromResult(result.AsEnumerable());
            }
            return Task.FromResult<IEnumerable<Racer>>(null);
        }

        public Task UpdateAsync(string callsign, double latitude, double longitude)
        {
            if (racers.TryGetValue(callsign, out var racer))
            {
                racer.Longitude = longitude;
                racer.Latitude = latitude;
            }
            return Task.CompletedTask;
        }

        public Task UpdateTimeAsync(string callsign, int checkpoint, long time)
        {
            if (racers.TryGetValue(callsign, out var racer))
            {
                if (checkpoint == racer.CheckpointTimes.Count)
                {
                    racer.CheckpointTimes.Add(time);
                }
                else
                {
                    racer.CheckpointTimes[checkpoint] = time;
                }
            }
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid id)
        {
            stopwatches.TryRemove(id, out _);
            racersInEvent.TryRemove(id, out var racerCallsigns);
            foreach (var racer in racerCallsigns)
            {
                racers.TryRemove(racer, out _);
            }
            return Task.CompletedTask;
        }
    }
}
