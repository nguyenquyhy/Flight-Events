using FlightEvents.Data;
using HotChocolate.Types;
using System;
using System.Threading.Tasks;

namespace FlightEvents.Web.GraphQL
{
    public class MutationType : ObjectType<Mutation>
    {
        protected override void Configure(IObjectTypeDescriptor<Mutation> descriptor)
        {
            descriptor.Field(o => o.AddFlightEventAsync(default)).Argument("flightEvent", argDescriptor => argDescriptor.Type<NonNullType<FlightEventInputType>>());
        }
    }

    public class Mutation
    {
        private readonly IFlightEventStorage storage;

        public Mutation(IFlightEventStorage storage)
        {
            this.storage = storage;
        }

        public async Task<FlightEvent> AddFlightEventAsync(FlightEvent flightEvent)
        {
            return await storage.AddAsync(flightEvent);
        }

        public async Task<bool> DeleteFlightEventAsync(Guid id)
        {
            return await storage.DeleteAsync(id);
        }
    }
}
