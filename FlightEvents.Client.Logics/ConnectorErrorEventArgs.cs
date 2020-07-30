using System;

namespace FlightEvents.Client.Logics
{
    public class ConnectorErrorEventArgs : EventArgs
    {
        public ConnectorErrorEventArgs(string simConnectError)
        {
            SimConnectError = simConnectError;
        }

        public string SimConnectError { get; }
    }
}
