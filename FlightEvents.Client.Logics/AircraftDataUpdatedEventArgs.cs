using System;

namespace FlightEvents.Client.Logics
{
    public class AircraftDataUpdatedEventArgs : EventArgs
    {
        public AircraftDataUpdatedEventArgs(AircraftData aircraftData)
        {
            AircraftData = aircraftData;
        }

        public AircraftData AircraftData { get; }
    }
}
