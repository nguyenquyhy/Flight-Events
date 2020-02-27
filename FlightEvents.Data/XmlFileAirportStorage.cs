using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FlightEvents.Data
{
    public class XmlFileAirportStorage : IAirportStorage
    {
        private List<Airport> airports = null;

        public Task<List<Airport>> GetAirportsAsync(List<string> idents)
        {
            if (airports == null)
            {
                using var stream = File.OpenRead("Airports.xml");
                var serializer = new XmlSerializer(typeof(Airports));
                var data = serializer.Deserialize(stream) as Airports;
                airports = data.Airport.ToList();
            }
            return Task.FromResult(airports.Where(o => idents.Contains(o.Ident)).ToList());
        }
    }
}
