﻿using FlightEvents.Data;
using HotChocolate.Types;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IFlightPlanStorage flightPlanStorage;

        public Query(IFlightEventStorage eventStorage, IAirportStorage airportStorage, IFlightPlanStorage flightPlanStorage)
        {
            this.storage = eventStorage;
            this.airportStorage = airportStorage;
            this.flightPlanStorage = flightPlanStorage;
        }

        public async Task<IEnumerable<FlightEvent>> GetFlightEventsAsync(bool upcoming = false)
        {
            var result = await storage.GetAllAsync();
            if (upcoming)
            {
                result = result.Where(o => (o.EndDateTime ?? (o.StartDateTime.AddHours(4))) > DateTimeOffset.Now)
                    .OrderBy(o => o.StartDateTime);
            }
            return result;
        }

        public Task<FlightEvent> GetFlightEventAsync(Guid id) => storage.GetAsync(id);

        public Task<FlightEvent> GetFlightEventByStopwatchCodeAsync(string code) => storage.GetByStopwatchCodeAsync(code);

        public Task<List<Airport>> GetAirportsAsync(List<string> idents) => airportStorage.GetAirportsAsync(idents);

        public Task<FlightPlanData> GetFlightPlanAsync(string id) => flightPlanStorage.GetFlightPlanAsync(id);
    }
}
