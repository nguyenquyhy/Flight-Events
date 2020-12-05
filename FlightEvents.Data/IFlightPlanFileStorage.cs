using System.Threading.Tasks;

namespace FlightEvents.Data
{
    public interface IFlightPlanFileStorage
    {
        Task<string> GetFlightPlanUrlAsync(string id);
        Task<FlightPlanData> GetFlightPlanAsync(string id);
    }
}
