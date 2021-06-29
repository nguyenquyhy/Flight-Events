using FlightEvents.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FlightEvents.Client.Logics
{
    public interface IEventFetcher
    {
        Task<IEnumerable<FlightEvent>> GetAsync();
    }

    public class EventFetcher : IEventFetcher
    {
        private static readonly TimeSpan cacheLifetime = TimeSpan.FromSeconds(10);

        private readonly ILogger<EventFetcher> logger;
        private readonly IEventGraphQLClient graphQLClient;
        private readonly SemaphoreSlim sm = new SemaphoreSlim(1);

        private (DateTimeOffset time, IEnumerable<FlightEvent> events)? cache = null;

        public EventFetcher(ILogger<EventFetcher> logger, IEventGraphQLClient graphQLClient)
        {
            this.logger = logger;
            this.graphQLClient = graphQLClient;
        }

        public async Task<IEnumerable<FlightEvent>> GetAsync()
        {
            await sm.WaitAsync();
            try
            {
                if (cache.HasValue && DateTimeOffset.Now - cache.Value.time < cacheLifetime)
                {
                    logger.LogInformation("Return events from cache");
                    return cache.Value.events;
                }
                logger.LogDebug("Fetching new events...");
                var events = await graphQLClient.GetFlightEventsAsync();
                logger.LogDebug("Fetched new events");
                cache = (DateTimeOffset.Now, events);
                return events;
            }
            finally
            {
                sm.Release();
            }
        }
    }
}
