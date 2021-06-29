using FlightEvents.Web.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace FlightEvents.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController
    {
        private readonly IHubContext<FlightEventHub, IFlightEventHub> hubContext;

        public AdminController(IHubContext<FlightEventHub, IFlightEventHub> hubContext)
        {
            this.hubContext = hubContext;
        }

        [HttpPost]
        public async Task TriggerUpdateAsync()
        {
            await hubContext.Clients.All.NotifyEventsUpdated();
        }
    }
}
