using FlightEvents.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FlightEvents.Web
{
    public class BroadcastOptions
    {
        public int MapDelayMilliseconds { get; set; }
    }

    public class StatusBroadcastService : BackgroundService
    {
        private readonly IHubContext<FlightEventHub, IFlightEventHub> hubContext;
        private readonly IOptionsMonitor<BroadcastOptions> optionsAccessor;

        public StatusBroadcastService(IHubContext<FlightEventHub, IFlightEventHub> hubContext, IOptionsMonitor<BroadcastOptions> optionsAccessor)
        {
            this.hubContext = hubContext;
            this.optionsAccessor = optionsAccessor;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (true)
            {
                await Task.Delay(optionsAccessor.CurrentValue.MapDelayMilliseconds);
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