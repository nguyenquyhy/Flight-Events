using FlightEvents.Data;
using HotChocolate.Types;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightEvents.Web.GraphQL
{
    public class MutationType : ObjectType<Mutation>
    {
        protected override void Configure(IObjectTypeDescriptor<Mutation> descriptor)
        {
            descriptor.Field(o => o.AddFlightEventAsync(default)).Argument("flightEvent", argDescriptor => argDescriptor.Type<NonNullType<FlightEventAddInputType>>());

            descriptor.Field(o => o.UpdateFlightEventAsync(default)).Argument("flightEvent", argDescriptor => argDescriptor.Type<NonNullType<FlightEventUpdateInputType>>());
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

        public async Task<FlightEvent> UpdateFlightEventAsync(FlightEvent flightEvent)
        {
            var current = await storage.GetAsync(flightEvent.Id);
            if (current == null) return null;

            flightEvent.UpdateTo(current);

            return await storage.UpdateAsync(current);
        }

        public async Task<bool> DeleteFlightEventAsync(Guid id)
        {
            return await storage.DeleteAsync(id);
        }

        public async Task<FlightEvent> AddFlightPlanAsync(Guid eventId, string flightPlanId)
        {
            var flightEvent = await storage.GetAsync(eventId);
            if (flightEvent.FlightPlanIds == null)
            {
                flightEvent.FlightPlanIds = new List<string>();
            }
            if (!flightEvent.FlightPlanIds.Contains(flightPlanId))
            {
                flightEvent.FlightPlanIds.Add(flightPlanId);
                await storage.UpdateAsync(flightEvent);
            }
            return flightEvent;
        }
    }
}
