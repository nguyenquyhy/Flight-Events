using FlightEvents.Web.Logics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace FlightEvents.Web.Controllers
{
    public class DiscordController : Controller
    {
        private readonly DiscordOptions options;
        private readonly DiscordLogic discordLogic;

        public DiscordController(IOptionsMonitor<DiscordOptions> optionsAccessor, DiscordLogic discordLogic)
        {
            this.options = optionsAccessor.CurrentValue;
            this.discordLogic = discordLogic;
        }

        public ActionResult Connect()
        {
            var clientId = options.ClientId;
            var url = Url.Action(nameof(Auth), null, null, "https");
            return Redirect($"https://discordapp.com/api/oauth2/authorize?client_id={clientId}&redirect_uri={Uri.EscapeDataString(url)}&response_type=code&scope={Uri.EscapeDataString("identify guilds.join")}");
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

        [HttpDelete]
        [Route("Discord/Connection/{clientId}")]
        public async Task Delete(string clientId)
        {
            await discordLogic.DeleteConnectionAsync(clientId);
        }
    }

}