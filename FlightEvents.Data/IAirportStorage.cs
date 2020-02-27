using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightEvents.Data
{
    public interface IAirportStorage
    {
        Task<List<Airport>> GetAirportsAsync(List<string> idents);
    }
}
