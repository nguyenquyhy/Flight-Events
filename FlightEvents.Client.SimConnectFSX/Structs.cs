using System.Runtime.InteropServices;

namespace FlightEvents.Client.SimConnectFSX
{
    enum GROUPID
    {
        FLAG = 2000000000,
    };

    enum DEFINITIONS
    {
        AircraftData,
        FlightStatus,
        AircraftPosition
    }

    internal enum DATA_REQUESTS
    {
        NONE,
        SUBSCRIBE_GENERIC,
        AIRCRAFT_DATA,
        FLIGHT_STATUS,
        ENVIRONMENT_DATA,
        FLIGHT_PLAN
    }

    internal enum EVENTS
    {
        CONNECTED,
        MESSAGE_RECEIVED,
        POSITION_CHANGED
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct AircraftDataStruct
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Type;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Model;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Title;
        public double EstimatedCruiseSpeed;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct FlightStatusStruct
    {
        //[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        //public string EngineType;
        //public float SimTime;
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
        public double TrueAirSpeed;
        public double VerticalSpeed;
        public double TouchdownNormalVelocity;
        public double GForce;

        public double FuelTotalQuantity;
        public double FuelTotalQuantityWeight;
        public int IsUnlimitedFuel;

        public double BarometerPressure;
        public double TotalAirTemperature;
        public double WindVelocity;
        public double WindDirection;

        public int IsOnGround;
        public int StallWarning;
        public int OverspeedWarning;

        public int IsAutopilotOn;

        public int Transponder;
        public int TransponderState;
        public int ComReceiveAll;
        public int Com1Transmit;
        public int Com2Transmit;
        public int Com3Transmit;
        public int Com1;
        public int Com2;
        public int Com3;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct AircraftPositionStruct
    {
        public double Latitude;
        public double Longitude;
        public double Altitude;
    }
}
