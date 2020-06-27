using HotChocolate.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace FlightEvents.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        [HttpGet("Me")]
        public IActionResult GetMe()
        {
            return Ok(User.Claims.ToDictionary(o => o.Type, o => o.Value));
        }
    }
}
