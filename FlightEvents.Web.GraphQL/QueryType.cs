using FlightEvents.Data;
using HotChocolate.Types;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightEvents.Web.GraphQL
{
    public class QueryType : ObjectType<Query>
    {
        //protected override void Configure(IObjectTypeDescriptor descriptor)
        //{
        //    descriptor.Field("events").Resolver(context => new List<FlightEvent>());
        //}
    }

    public class Query
    {
        private readonly IFlightEventStorage storage;

        public Query(IFlightEventStorage storage)
        {
            this.storage = storage;
        }

        public async Task<IEnumerable<FlightEvent>> FlightEvents()
        {
            return await storage.GetAllAsync();
        }
    }
}
