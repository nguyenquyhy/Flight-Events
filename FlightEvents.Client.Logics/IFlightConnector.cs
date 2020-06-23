using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlightEvents.Client.Logics
{
    public interface IFlightConnector
    {
        event EventHandler<AircraftDataUpdatedEventArgs> AircraftDataUpdated;
        event EventHandler<AircraftStatusUpdatedEventArgs> AircraftStatusUpdated;
        event EventHandler<FlightPlanUpdatedEventArgs> FlightPlanUpdated;
        event EventHandler AircraftPositionChanged;

        Task<AircraftData> RequestAircraftDataAsync(CancellationToken cancellationToken = default);
        Task<FlightPlanData> RequestFlightPlanAsync(CancellationToken cancellationToken = default);
        void Send(string message);
    }
}
