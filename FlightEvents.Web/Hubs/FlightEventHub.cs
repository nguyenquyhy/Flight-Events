using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightEvents.Web.Hubs
{
    public class FlightEventHub : Hub
    {
        public async Task UpdateAircraft(string connectionId, AircraftStatus status)
        {
            await Clients.Groups("Map", "ATC").SendAsync("UpdateAircraft", connectionId, status);
        }

        public async Task UpdateFlightPlan(string connectionId, FlightPlanCompact flightPlan)
        {
            await Clients.Groups("ATC").SendAsync("UpdateFlightPlan", connectionId, flightPlan);
        }

        public async Task RequestFlightPlan(string callsign)
        {
            await Clients.All.SendAsync("RequestFlightPlan", Context.ConnectionId, callsign);
        }

        public async Task ReturnFlightPlan(string connectionId, FlightPlanCompact flightPlan, List<string> atcConnectionIds)
        {
            await Clients.Clients(atcConnectionIds).SendAsync("ReturnFlightPlan", connectionId, flightPlan);
        }

        public async Task RequestFlightPlanDetails(string connectionId)
        {
            await Clients.Clients(connectionId).SendAsync("RequestFlightPlanDetails", Context.ConnectionId);
        }

        public async Task ReturnFlightPlanDetails(string connectionId, FlightPlanData flightPlan, string webConnectionId)
        {
            await Clients.Clients(webConnectionId).SendAsync("ReturnFlightPlanDetails", connectionId, flightPlan);
        }

        public async Task SendMessage(string from, string to, string message)
        {
            await Clients.All.SendAsync("SendMessage", from, to, message);
        }

        public async Task Join(string group)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, group);
        }

        public async Task Leave(string group)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            return base.OnDisconnectedAsync(exception);
        }
    }
}
