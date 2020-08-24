using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlightEvents.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;

namespace FlightEvents.Web
{
    public class StatusBroadcastService : BackgroundService
    {
        private readonly IHubContext<FlightEventHub, IFlightEventHub> hubContext;

        public StatusBroadcastService(IHubContext<FlightEventHub, IFlightEventHub> hubContext)
        {
            this.hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (true)
            {
                await Task.Delay(2000);
                stoppingToken.ThrowIfCancellationRequested();

                var statuses = FlightEventHub.ConnectionIdToAircraftStatuses.ToList();
                foreach (var pair in statuses)
                {
                    if (FlightEventHub.ConnectionIdToClientIds.TryGetValue(pair.Key, out var clientId))
                    {
                        await hubContext.Clients.Groups("Map", "ClientMap").UpdateAircraft(clientId, pair.Value);
                    }
                    else
                    {
                        // Client is probably removed, it is better to remove cache too
                        FlightEventHub.RemoveCacheOnConnectionId(pair.Key);
                    }
                }
            }
        }
    }
}