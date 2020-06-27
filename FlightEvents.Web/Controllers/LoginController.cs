using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FlightEvents.Web.Controllers
{
    public class LoginController : ControllerBase
    {
        [Route("Login")]
        public IActionResult Microsoft([FromQuery] string ReturnUrl)
        {
            if (User.Identity.IsAuthenticated)
                return Redirect("~/");

            return Challenge(new AuthenticationProperties
            {
                RedirectUri = ReturnUrl
            });
        }

        [Route("Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            return Redirect("~/");
        }
    }
}
