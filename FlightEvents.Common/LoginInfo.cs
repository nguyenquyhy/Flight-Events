namespace FlightEvents
{
    public class LoginInfo
    {
        // TODO: change to enum with string converter
        public const string App = "App";
        public const string Web = "Web";

        public string ClientType { get; set; }
        public string AtcCallsign { get; set; }
        public string AircraftGroup { get; set; }
        public bool UseTraffic { get; set; }
    }
}
