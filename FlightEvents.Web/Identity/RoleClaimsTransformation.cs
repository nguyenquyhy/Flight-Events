using FlightEvents.Data;
using Microsoft.AspNetCore.Authentication;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FlightEvents.Web.Identity
{
    public class RoleClaimsTransformation : IClaimsTransformation
    {
        private readonly IUserStorage userStorage;

        public RoleClaimsTransformation(IUserStorage userStorage)
        {
            this.userStorage = userStorage;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            // Clone current identity
            var clone = principal.Clone();
            var newIdentity = (ClaimsIdentity)clone.Identity;

            // Support AD and local accounts
            //var nameId = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == ClaimTypes.Name);
            var nameId = principal.Claims.FirstOrDefault(o => o.Type == "preferred_username");

            if (nameId == null)
            {
                return principal;
            }

            // Get user from database
            var user = await userStorage.GetAsync(nameId.Value);
            if (user == null)
            {
                return principal;
            }

            // Add role claims to cloned identity
            foreach (var role in user.Roles)
            {
                var claim = new Claim(newIdentity.RoleClaimType, role);
                newIdentity.AddClaim(claim);
            }

            return clone;
        }
    }
}
