using FlightEvents.Web.Logics;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FlightEvents.Web.Controllers
{
    public class DiscordController : Controller
    {
        private readonly DiscordLogic discordLogic;

        public DiscordController(DiscordLogic discordLogic)
        {
            this.discordLogic = discordLogic;
        }

        public async Task<ActionResult> Auth(string code)
        {
            var confirm = await discordLogic.LoginAsync(code);
            return View(confirm);
        }

        [Route("Discord/Connection/{clientId}")]
        public async Task<ActionResult<DiscordConnection>> Connection(string clientId)
        {
            var connection = await discordLogic.GetConnectionAsync(clientId);
            if (connection == null) return NotFound();

            return connection;
        }

        [HttpPost]
        public async Task<ActionResult<DiscordConnection>> Confirm(string clientId, string code)
        {
            return await discordLogic.ConfirmAsync(clientId, code);
        }
    }

}