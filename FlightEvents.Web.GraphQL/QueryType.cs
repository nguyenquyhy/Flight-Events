using FlightEvents.Data;
using HotChocolate.Types;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightEvents.Web.GraphQL
{
    public class QueryType : ObjectType<Query>
    {
    }

    public class Query
    {
        private readonly IFlightEventStorage storage;
        private readonly IAirportStorage airportStorage;

        public Query(IFlightEventStorage eventStorage, IAirportStorage airportStorage)
        {
            this.storage = eventStorage;
            this.airportStorage = airportStorage;
        }

        public Task<IEnumerable<FlightEvent>> FlightEvents() => storage.GetAllAsync();

        public Task<FlightEvent> FlightEvent(Guid id) => storage.GetAsync(id);

        public Task<List<Airport>> GetAirports(List<string> idents) => airportStorage.GetAirportsAsync(idents);
    }
}
