using System;
using System.Text.RegularExpressions;

namespace FlightEvents
{
    public class GpsHelper
    {
        private static Regex re = new Regex(@"([N|S])([0-9]*[\.[0-9]+]*)[*|°]\s([0-9]*[\.[0-9]+]*)'\s([0-9]*[\.[0-9]+]*)"",([W|E])([0-9]*[\.[0-9]+]*)[*|°]\s([0-9]*[\.[0-9]+]*)'\s([0-9]*[\.[0-9]+]*)""");

        public static (double latitude, double longitude) ConvertString(string data)
        {
            double lt, ln;
            lt = 0.0;
            ln = 0.0;

            var m = re.Match(data);

            if (m.Success)
            {
                lt = Convert.ToDouble(m.Groups[2].Value) + (Convert.ToDouble(m.Groups[3].Value) / 60.0) + (Convert.ToDouble(m.Groups[4].Value) / 3600.0);
                if (m.Groups[1].Value.Contains("S"))
                {
                    lt *= -1.0;
                }
                ln = Convert.ToDouble(m.Groups[6].Value) + (Convert.ToDouble(m.Groups[7].Value) / 60.0) + (Convert.ToDouble(m.Groups[8].Value) / 3600.0);
                if (m.Groups[5].Value.Contains("W"))
                {
                    ln *= -1.0;
                }
            }

            return (lt, ln);
        }

        public static double CalculateDistance(double latitude1, double longitude1,
            double latitude2, double longitude2)
        {
            //var earthRadiusKm = 6371d;
            var earthRadiusKt = 3440d;

            var dLat = DegreesToRadians(latitude2 - latitude1);
            var dLon = DegreesToRadians(longitude2 - longitude1);

            latitude1 = DegreesToRadians(latitude1);
            latitude2 = DegreesToRadians(latitude2);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(latitude1) * Math.Cos(latitude2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadiusKt * c;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}
