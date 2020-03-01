using FlightEvents.Common;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace FlightEvents.Web.Hubs
{
    public class FlightEventHub : Hub
    {
        public async Task UpdateAircraft(string connectionId, AircraftStatus status)
        {
            await Clients.Groups("Map", "ATC").SendAsync("UpdateAircraft", connectionId, status);
        }

        public async Task UpdateFlightPlan(string connectionId, FlightPlanData flightPlan)
        {
            await Clients.Groups("ATC").SendAsync("UpdateFlightPlan", connectionId, flightPlan);
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
