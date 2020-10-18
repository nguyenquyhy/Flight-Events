using System;
using System.Collections.Generic;

namespace FlightEvents.Client.Logics
{
    public class AirportListReceivedEventArgs : EventArgs
    {
        public AirportListReceivedEventArgs(IEnumerable<Airport> airports)
        {
            Airports = airports;
        }

        public IEnumerable<Airport> Airports { get; }
    }
}