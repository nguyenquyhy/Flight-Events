using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlightEvents.Data
{
    public interface IATCFlightPlanStorage
    {
        Task<IEnumerable<(string clientId, FlightPlanCompact flightPlan)>> GetFlightPlansAsync();
        Task<(string clientId, FlightPlanCompact flightPlan)> GetFlightPlanAsync(string callsign);
        Task SetFlightPlanAsync(string callsign, string clientId, FlightPlanCompact flightPlanCompact);
        Task DeleteFlightPlanAsync(string callsign);
    }

    public class InMemoryATCFlightPlanStorage : IATCFlightPlanStorage
    {
        private readonly ConcurrentDictionary<string, (string clientId, FlightPlanCompact flightPlan)> storage
            = new ConcurrentDictionary<string, (string clientId, FlightPlanCompact flightPlan)>();

        public Task<IEnumerable<(string clientId, FlightPlanCompact flightPlan)>> GetFlightPlansAsync()
        {
            return Task.FromResult(storage.Values.AsEnumerable());
        }

        public Task<(string clientId, FlightPlanCompact flightPlan)> GetFlightPlanAsync(string callsign)
        {
            if (storage.TryGetValue(callsign, out var value))
            {
                return Task.FromResult((value.clientId, value.flightPlan));
            }
            else
            {
                return Task.FromResult<(string clientId, FlightPlanCompact flightPlan)>((null, null));
            }
        }

        public Task SetFlightPlanAsync(string callsign, string clientId, FlightPlanCompact flightPlanCompact)
        {
            storage.AddOrUpdate(callsign, (clientId, flightPlanCompact), (a, b) => (clientId, flightPlanCompact));
            return Task.CompletedTask;
        }

        public Task DeleteFlightPlanAsync(string callsign)
        {
            storage.Remove(callsign, out _);
            return Task.CompletedTask;
        }
    }
}
