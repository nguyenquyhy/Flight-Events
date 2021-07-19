using System;
using FlightEvents;
namespace UltraATC.FSDServer
{

    public class ConnectedEventArgs : EventArgs
    {
        public ConnectedEventArgs(string callsign, string realName, string certificate, string rating, double? latitude, double? longitude)
        {
            Callsign = callsign;
            RealName = realName;
            Certificate = certificate;
            Rating = rating;
            Latitude = latitude;
            Longitude = longitude;
        }

        public string Callsign { get; }
        public string RealName { get; }
        public string Certificate { get; }
        public string Rating { get; }
        public double? Latitude { get; }
        public double? Longitude { get; }
    }

    public class FlightPlanRequestedEventArgs : EventArgs
    {
        public FlightPlanRequestedEventArgs(string callsign)
        {
            Callsign = callsign;
        }

        public string Callsign { get; }
    }

    public class MessageSentEventArgs : EventArgs
    {
        public MessageSentEventArgs(string to, string message)
        {
            To = to;
            Message = message;
        }

        public string To { get; }
        public string Message { get; }
    }

    public class AtcUpdatedEventArgs : EventArgs
    {
        public AtcUpdatedEventArgs(string callsign, int frequency, int altitude, double latitude, double longitude)
        {
            Callsign = callsign;
            Frequency = frequency;
            Altitude = altitude;
            Latitude = latitude;
            Longitude = longitude;
        }

        public string Callsign { get; }
        public int Frequency { get; }
        public int Altitude { get; }
        public double Latitude { get; }
        public double Longitude { get; }
    }

    public class AtcLoggedOffEventArgs : EventArgs
    {
        public AtcLoggedOffEventArgs(string callsign)
        {
            Callsign = callsign;
        }

        public string Callsign { get; }
    }

    public class AtcMessageSentEventArgs : EventArgs
    {
        public AtcMessageSentEventArgs(string to, string message)
        {
            To = to;
            Message = message;
        }

        public string To { get; }
        public string Message { get; }
    }

    public class AircraftUpdatedEventArgs : EventArgs
    {
        public AircraftUpdatedEventArgs(AircraftStatus aircraftStatus)
        {
            AircraftStatus = aircraftStatus;

            
        }

        public AircraftStatus AircraftStatus;
    }

    public class FlightPlanUpdatedEventArgs : EventArgs
    {
        public FlightPlanUpdatedEventArgs(FlightPlanCompact flightPlan)
        {
            FlightPlan = flightPlan;
        }

        public FlightPlanCompact FlightPlan;
    }

    public enum AtcTransponderMode
    {
        Standby,
        ModeC,
        Ident
    }
}
