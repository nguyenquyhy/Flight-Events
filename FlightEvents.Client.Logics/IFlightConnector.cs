using System;

namespace FlightEvents.Client.Logics
{
    public interface IFlightConnector
    {
        event EventHandler<AircraftDataUpdatedEventArgs> AircraftDataUpdated;
        event EventHandler<AircraftStatusUpdatedEventArgs> AircraftStatusUpdated;
        event EventHandler<FlightPlanUpdatedEventArgs> FlightPlanUpdated;

        void RequestFlightPlan(string atcConnectionId);
    }
}
