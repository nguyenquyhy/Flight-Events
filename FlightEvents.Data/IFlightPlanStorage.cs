using FlightEvents.Common;
using System.Threading.Tasks;

namespace FlightEvents.Data
{
    public interface IFlightPlanStorage
    {
        Task<FlightPlanData> GetFlightPlanAsync(string id);
    }
}
