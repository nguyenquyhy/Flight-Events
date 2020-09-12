using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightEvents.Data
{
    public interface IFlightEventStorage
    {
        Task<IEnumerable<FlightEvent>> GetAllAsync();
        Task<FlightEvent> GetAsync(Guid id);
        Task<FlightEvent> GetByCodeAsync(string code);
        Task<FlightEvent> GetByStopwatchCodeAsync(string code);
        Task<FlightEvent> AddAsync(FlightEvent flightEvent);
        Task<FlightEvent> UpdateAsync(FlightEvent flightEvent);
        Task<bool> DeleteAsync(Guid id);
    }
}
