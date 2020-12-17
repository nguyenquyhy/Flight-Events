using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightEvents.Data
{
    public class User
    {
        public string Username { get; set; }
        public string Name { get; set; }
        public IEnumerable<string> Roles { get; set; }
    }

    public interface IUserStorage
    {
        Task<User> GetAsync(string username);
    }
}
