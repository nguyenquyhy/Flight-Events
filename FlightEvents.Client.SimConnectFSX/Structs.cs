using System.Runtime.InteropServices;

namespace FlightEvents.Client.SimConnectFSX
{
    enum GROUPID
    {
        FLAG = 2000000000,
    };

    enum DEFINITIONS
    {
        FlightStatus
    }

    internal enum DATA_REQUESTS
    {
        NONE,
        SUBSCRIBE_GENERIC,
        AIRCRAFT_DATA,
        FLIGHT_STATUS,
        ENVIRONMENT_DATA
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct FlightStatusStruct
    {
        public int SimRate;

        public double Latitude;
        public double Longitude;
        public double Altitude;
        public double AltitudeAboveGround;
        public double Pitch;
        public double Bank;
        public double TrueHeading;
        public double MagneticHeading;
        public double GroundAltitude;
        public double GroundSpeed;
        public double IndicatedAirSpeed;
        public double VerticalSpeed;

        public double FuelTotalQuantity;

        public double WindVelocity;
        public double WindDirection;

        public int IsOnGround;
        public int StallWarning;
        public int OverspeedWarning;

        public int IsAutopilotOn;

        public int Transponder;
    }
}
