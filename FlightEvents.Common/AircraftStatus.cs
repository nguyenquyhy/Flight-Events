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
        public AircraftStatus()
        {

        }

        public AircraftStatus(AircraftStatus data)
        {
            ClientVersion = data.ClientVersion;

            Callsign = data.Callsign;
            SimRate = data.SimRate;
            LocalTime = data.LocalTime;
            ZuluTime = data.ZuluTime;
            AbsoluteTime = data.AbsoluteTime;

            Latitude = data.Latitude;
            Longitude = data.Longitude;
            Altitude = data.Altitude;
            AltitudeAboveGround = data.AltitudeAboveGround;

            Pitch = data.Pitch;
            Bank = data.Bank;
            Heading = data.Heading;
            TrueHeading = data.TrueHeading;

            GroundSpeed = data.GroundSpeed;
            IndicatedAirSpeed = data.IndicatedAirSpeed;
            TrueAirSpeed = data.TrueAirSpeed;
            VerticalSpeed = data.VerticalSpeed;
            TouchdownNormalVelocity = data.TouchdownNormalVelocity;
            GForce = data.GForce;

            FuelTotalQuantity = data.FuelTotalQuantity;
            FuelTotalQuantityWeight = data.FuelTotalQuantityWeight;
            IsUnlimitedFuel = data.IsUnlimitedFuel;

            BarometerPressure = data.BarometerPressure;
            TotalAirTemperature = data.TotalAirTemperature;
            WindVelocity = data.WindVelocity;
            WindDirection = data.WindDirection;

            IsOnGround = data.IsOnGround;
            StallWarning = data.StallWarning;
            OverspeedWarning = data.OverspeedWarning;

            IsAutopilotOn = data.IsAutopilotOn;
            Transponder = data.Transponder;
            TransponderState = data.TransponderState;
            ReceiveAllCom = data.ReceiveAllCom;
            TransmitCom1 = data.TransmitCom1;
            TransmitCom2 = data.TransmitCom2;
            TransmitCom3 = data.TransmitCom3;
            FrequencyCom1 = data.FrequencyCom1;
            FrequencyCom2 = data.FrequencyCom2;
            FrequencyCom3 = data.FrequencyCom3;
        }

        public string ClientVersion { get; set; }
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

        public double Pitch { get; set; }
        public double Bank { get; set; }
        public double Heading { get; set; }
        public double TrueHeading { get; set; }

        public double GroundSpeed { get; set; }
        public double IndicatedAirSpeed { get; set; }
        public double TrueAirSpeed { get; set; }
        public double VerticalSpeed { get; set; }
        public double TouchdownNormalVelocity { get; set; }
        public double GForce { get; set; }

        public double FuelTotalQuantity { get; set; }
        public double FuelTotalQuantityWeight { get; set; }
        public int IsUnlimitedFuel { get; set; }

        public double BarometerPressure { get; set; }
        public double TotalAirTemperature { get; set; }
        public double WindVelocity { get; set; }
        public double WindDirection { get; set; }

        public bool IsOnGround { get; set; }
        public bool StallWarning { get; set; }
        public bool OverspeedWarning { get; set; }

        public bool IsAutopilotOn { get; set; }
        public string Transponder { get; set; }
        public int TransponderState { get; set; }
        public bool ReceiveAllCom { get; set; }
        public bool TransmitCom1 { get; set; }
        public bool TransmitCom2 { get; set; }
        public bool TransmitCom3 { get; set; }
        public int FrequencyCom1 { get; set; }
        public int FrequencyCom2 { get; set; }
        public int FrequencyCom3 { get; set; }
        public TransponderMode TransponderMode { get; set; } = TransponderMode.ModeC;
    }

    public enum TransponderMode
    {
        Standby,
        ModeC,
        Ident
    }
}
