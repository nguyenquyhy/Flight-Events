namespace FlightEvents
{
    public class AircraftStatusBrief
    {
        public AircraftStatusBrief()
        {

        }

        public AircraftStatusBrief(AircraftStatus status)
        {
            Latitude = status.Latitude;
            Longitude = status.Longitude;
            Altitude = status.Altitude;
            IsOnGround = status.IsOnGround;
        }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public bool IsOnGround { get; set; }
    }

    public class AircraftStatus
    {
        public string Callsign { get; set; }

        //public double SimTime { get; set; }
        public int SimRate { get; set; }
        public int? LocalTime { get; set; }
        public int? ZuluTime { get; set; }
        public long? AbsoluteTime { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public double AltitudeAboveGround { get; set; }

        public double Heading { get; set; }
        public double TrueHeading { get; set; }

        public double GroundSpeed { get; set; }
        public double IndicatedAirSpeed { get; set; }
        public double VerticalSpeed { get; set; }
        public double TouchdownNormalVelocity { get; set; }
        public double GForce { get; set; }

        public double FuelTotalQuantity { get; set; }

        public double Pitch { get; set; }
        public double Bank { get; set; }

        public bool IsOnGround { get; set; }
        public bool StallWarning { get; set; }
        public bool OverspeedWarning { get; set; }

        public bool IsAutopilotOn { get; set; }
        public string Transponder { get; set; }
        public bool ReceiveAllCom { get; set; }
        public bool TransmitCom1 { get; set; }
        public bool TransmitCom2 { get; set; }
        public int FrequencyCom1 { get; set; }
        public int FrequencyCom2 { get; set; }
        public TransponderMode TransponderMode { get; set; } = TransponderMode.ModeC;
    }

    public enum TransponderMode
    {
        Standby,
        ModeC,
        Ident
    }
}
