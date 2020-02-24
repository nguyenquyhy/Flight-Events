using System;

namespace FlightEvents.Client.Logics
{
    public interface IFlightConnector
    {
        event EventHandler<AircraftStatusUpdatedEventArgs> AircraftStatusUpdated;
    }
}
