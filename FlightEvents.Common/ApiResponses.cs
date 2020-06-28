namespace FlightEvents
{
    public class IATAGeoResult
    {
        public string name { get; set; }
        public string code { get; set; }
        public string IATA { get; set; }
        public string ICAO { get; set; }
        public string distance_meters { get; set; }
    }

    public class AirportDataResult
    {
        public string icao { get; set; }
        public string iata { get; set; }
        public string name { get; set; }
        public string location { get; set; }
        public string country { get; set; }
        public string country_code { get; set; }
        public string longitude { get; set; }
        public string latitude { get; set; }
        public string link { get; set; }
        public int status { get; set; }
    }
}
