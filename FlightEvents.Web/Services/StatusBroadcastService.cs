using FlightEvents.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
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
        private static readonly TimeSpan timeout = TimeSpan.FromSeconds(10);

        private readonly IHubContext<FlightEventHub, IFlightEventHub> hubContext;
        private readonly IOptionsMonitor<BroadcastOptions> optionsAccessor;
        private readonly ILogger<StatusBroadcastService> logger;

        public StatusBroadcastService(IHubContext<FlightEventHub, IFlightEventHub> hubContext, IOptionsMonitor<BroadcastOptions> optionsAccessor, ILogger<StatusBroadcastService> logger)
        {
            this.hubContext = hubContext;
            this.optionsAccessor = optionsAccessor;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (true)
            {
                try
                {
                    await Task.Delay(optionsAccessor.CurrentValue.MapDelayMilliseconds);
                    stoppingToken.ThrowIfCancellationRequested();

                    var statuses = FlightEventHub.ConnectionIdToAircraftStatuses.ToList();
                    foreach (var pair in statuses)
                    {
                        if (DateTimeOffset.Now - pair.Value.updateTime < timeout
                            && FlightEventHub.ConnectionIdToClientIds.TryGetValue(pair.Key, out var clientId))
                        {
                            await hubContext.Clients.Groups("Map", "ClientMap").UpdateAircraft(clientId, pair.Value.status);

                            await hubContext.Clients.Groups("Admin").UpdateAircraft(clientId, new AircraftStatus(pair.Value.status)
                            {
                                ClientVersion = FlightEventHub.ConnetionIdToClientInfos.ContainsKey(pair.Key) ? FlightEventHub.ConnetionIdToClientInfos[pair.Key].ClientVersion : "?"
                            });
                        }
                        else
                        {
                            // Client is probably removed, it is better to remove cache too
                            FlightEventHub.RemoveCacheOnConnectionId(pair.Key);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Cannot execute status broadcast!");
                    await Task.Delay(10000);
                }
            }
        }
    }
}