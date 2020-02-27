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

        public Query(IFlightEventStorage storage)
        {
            this.storage = storage;
        }

        public Task<IEnumerable<FlightEvent>> FlightEvents() => storage.GetAllAsync();

        public Task<FlightEvent> FlightEvent(Guid id) => storage.GetAsync(id);
    }
}
