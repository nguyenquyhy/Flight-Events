using System;
using System.Threading.Tasks;

namespace FlightEvents.Client.Logics
{
    public interface IFlightConnector
    {
        event EventHandler<AircraftDataUpdatedEventArgs> AircraftDataUpdated;
        event EventHandler<AircraftStatusUpdatedEventArgs> AircraftStatusUpdated;
        event EventHandler<FlightPlanUpdatedEventArgs> FlightPlanUpdated;
        event EventHandler AircraftPositionChanged;

        Task<FlightPlanData> RequestFlightPlanAsync();
        void Send(string message);
    }
}
