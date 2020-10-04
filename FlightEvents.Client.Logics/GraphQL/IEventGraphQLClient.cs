using FlightEvents.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightEvents.Client.Logics
{
    public interface IEventGraphQLClient
    {
        Task<IEnumerable<FlightEvent>> GetFlightEventsAsync();
    }
}
