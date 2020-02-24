using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace FlightEvents.Web.Hubs
{
    public class FlightEventHub : Hub
    {
        public async Task UpdateAircraft(string connectionId, AircraftStatus status)
        {
            await Clients.Others.SendAsync("UpdateAircraft", connectionId, status);
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            return base.OnDisconnectedAsync(exception);
        }
    }
}
