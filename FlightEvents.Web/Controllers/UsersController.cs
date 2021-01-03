using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FlightEvents.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        [HttpGet("Me")]
        public ActionResult<UserProfile> GetMe()
        {
            var profile = new UserProfile
            {
                Id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                Name = User.FindFirst("name")?.Value,
                Username = User.FindFirst("preferred_username")?.Value,
                Role = User.FindFirst(ClaimTypes.Role)?.Value
            };
            return profile;
        }
    }

    public class UserProfile
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
    }
}
