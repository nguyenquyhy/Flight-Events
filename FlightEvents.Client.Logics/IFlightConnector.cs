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
        event EventHandler<AirportListReceivedEventArgs> AirportListReceived;
        event EventHandler AircraftPositionChanged;
        event EventHandler Closed;
        event EventHandler Connected;
        event EventHandler<ConnectorErrorEventArgs> Error;

        Task<AircraftData> RequestAircraftDataAsync(CancellationToken cancellationToken = default);
        Task<FlightPlanData> RequestFlightPlanAsync(CancellationToken cancellationToken = default);
        void Send(string message);
        void Teleport(double latitude, double longitude, double altitude);
    }
}
