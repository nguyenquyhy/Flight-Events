using FlightEvents.Web.Logics;
using Microsoft.AspNetCore.Mvc;
using System;
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

        [HttpPost]
        public async Task Confirm(string clientId, string code)
        {
            await discordLogic.ConfirmAsync(clientId, code);
        }
    }

}