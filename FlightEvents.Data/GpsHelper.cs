using System;
using System.Text.RegularExpressions;

namespace FlightEvents.Data
{
    public class GpsHelper
    {
        private static Regex re = new Regex(@"([N|S])([0-9]*[\.[0-9]+]*)°\s([0-9]*[\.[0-9]+]*)'\s([0-9]*[\.[0-9]+]*)"",([W|E])([0-9]*[\.[0-9]+]*)°\s([0-9]*[\.[0-9]+]*)'\s([0-9]*[\.[0-9]+]*)""");

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
    }
}
