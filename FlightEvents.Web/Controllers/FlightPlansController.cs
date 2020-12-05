using FlightEvents.Data;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlightEvents.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FlightPlansController : ControllerBase
    {
        private readonly IATCFlightPlanStorage storage;

        public FlightPlansController(IATCFlightPlanStorage storage)
        {
            this.storage = storage;
        }

        [HttpGet]
        public async Task<IEnumerable<FlightPlanCompact>> Get()
        {
            var list = await storage.GetFlightPlansAsync();
            return list.Select(o => o.flightPlan);
        }

        [Route("{callsign}")]
        [HttpGet]
        public async Task<FlightPlanCompact> Get(string callsign)
        {
            var result = await storage.GetFlightPlanAsync(callsign);
            return result.flightPlan;
        }

        [HttpPost]
        public async Task<FlightPlanCompact> Post(FlightPlanCompact flightPlan)
        {
            await storage.SetFlightPlanAsync(flightPlan.Callsign, null, flightPlan);
            return flightPlan;
        }

        [Route("{callsign}")]
        [HttpDelete]
        public Task Delete(string callsign)
        {
            return storage.DeleteFlightPlanAsync(callsign);
        }
    }
}
