using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightEvents.Web.Hubs
{
    public class FlightEventHub : Hub<IFlightEventHub>
    {
        private static readonly ConcurrentDictionary<string, string> clientIds = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, AircraftStatus> aircraftStatuses = new ConcurrentDictionary<string, AircraftStatus>();
        private static readonly ConcurrentDictionary<string, ATCInfo> atcInfos= new ConcurrentDictionary<string, ATCInfo>();
        private static readonly ConcurrentDictionary<string, ATCStatus> atcStatuses = new ConcurrentDictionary<string, ATCStatus>();

        public override Task OnConnectedAsync()
        {
            var clientId = (string)Context.GetHttpContext().Request.Query["clientId"];
            if (clientId != null)
            {
                clientIds[Context.ConnectionId] = clientId;
            }
            return base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (clientIds.TryRemove(Context.ConnectionId, out var clientId))
            {
                await Clients.Groups("Map", "ATC").UpdateATC(clientId, null, null);
            }
            aircraftStatuses.TryRemove(Context.ConnectionId, out _);
            atcStatuses.TryRemove(Context.ConnectionId, out _);
            await base.OnDisconnectedAsync(exception);
        }

        public void LoginATC(ATCInfo atc)
        {
            atcInfos[Context.ConnectionId] = atc;
        }

        public async Task UpdateATC(ATCStatus status)
        {
            if (clientIds.TryGetValue(Context.ConnectionId, out var clientId) && atcInfos.TryGetValue(Context.ConnectionId, out var atc))
            {
                int? fromFrequency = null;
                if (atcStatuses.TryGetValue(Context.ConnectionId, out var lastStatus))
                {
                    fromFrequency = lastStatus.FrequencyCom;
                }
                if (status != null)
                {
                    atcStatuses[Context.ConnectionId] = status;
                }
                if (fromFrequency != status?.FrequencyCom)
                {
                    await Clients.Groups("Bot").ChangeFrequency(clientId, fromFrequency, status?.FrequencyCom);
                }

                await Clients.Groups("Map", "ATC").UpdateATC(clientId, status, atc);
            }
        }

        public async Task UpdateAircraft(AircraftStatus status)
        {
            if (clientIds.TryGetValue(Context.ConnectionId, out var clientId))
            {
                // Sanitize status
                if (Math.Abs(status.Latitude) < 0.02 && Math.Abs(status.Longitude) < 0.02)
                {
                    // Aircraft is not loaded
                    status.FrequencyCom1 = 0;
                }

                if (!atcStatuses.TryGetValue(Context.ConnectionId, out _))
                {
                    // Detect COM1 change if ATC is not active
                    int fromFrequency = 0;
                    if (aircraftStatuses.TryGetValue(Context.ConnectionId, out var lastStatus))
                    {
                        fromFrequency = lastStatus.FrequencyCom1;
                    }
                    var toFrequency = status.FrequencyCom1;

                    aircraftStatuses[Context.ConnectionId] = status;

                    if (fromFrequency != toFrequency)
                    {
                        await Clients.Groups("Bot").ChangeFrequency(clientId, fromFrequency == 0 ? null : (int?)fromFrequency, toFrequency == 0 ? null : (int?)toFrequency);
                    }
                }
                await Clients.Groups("Map", "ATC").UpdateAircraft(clientId, status);
            }
        }

        public async Task RequestFlightPlan(string callsign)
        {
            await Clients.All.RequestFlightPlan(Context.ConnectionId, callsign);
        }

        public async Task ReturnFlightPlan(string connectionId, FlightPlanCompact flightPlan, List<string> atcConnectionIds)
        {
            await Clients.Clients(atcConnectionIds).ReturnFlightPlan(connectionId, flightPlan);
        }

        public async Task RequestFlightPlanDetails(string connectionId)
        {
            await Clients.Clients(connectionId).RequestFlightPlanDetails(Context.ConnectionId);
        }

        public async Task ReturnFlightPlanDetails(string connectionId, FlightPlanData flightPlan, string webConnectionId)
        {
            await Clients.Clients(webConnectionId).ReturnFlightPlanDetails(connectionId, flightPlan);
        }

        public async Task SendMessage(string from, string to, string message)
        {
            await Clients.All.SendMessage(from, to, message);
        }

        public async Task Join(string group)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, group);
        }

        public async Task Leave(string group)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        }
    }
}
