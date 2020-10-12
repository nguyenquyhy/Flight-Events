using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FlightEvents
{
    public class GpsHelper
    {
        private static readonly Regex re = new Regex(@"^([N|S])([0-9]*[\.[0-9]+]*)[*|°]\s([0-9]*[\.[0-9]+]*)'\s([0-9]*[\.[0-9]+]*)"",([W|E])([0-9]*[\.[0-9]+]*)[*|°]\s([0-9]*[\.[0-9]+]*)'\s([0-9]*[\.[0-9]+]*)"",?([+-][0-9]*[\.[0-9]+]*)?$");

        //var earthRadiusKm = 6371d;
        private const double earthRadiusNm = 3440d;

        public static (double latitude, double longitude, double? altitude) ConvertString(string data)
        {
            double lt, ln;
            lt = 0.0;
            ln = 0.0;
            double? alt = null;

            var m = re.Match(data);

            if (m.Success)
            {
                lt = Convert.ToDouble(m.Groups[2].Value, CultureInfo.InvariantCulture)
                    + (Convert.ToDouble(m.Groups[3].Value, CultureInfo.InvariantCulture) / 60.0)
                    + (Convert.ToDouble(m.Groups[4].Value, CultureInfo.InvariantCulture) / 3600.0);
                if (m.Groups[1].Value.Contains("S"))
                {
                    lt *= -1.0;
                }
                ln = Convert.ToDouble(m.Groups[6].Value, CultureInfo.InvariantCulture)
                    + (Convert.ToDouble(m.Groups[7].Value, CultureInfo.InvariantCulture) / 60.0)
                    + (Convert.ToDouble(m.Groups[8].Value, CultureInfo.InvariantCulture) / 3600.0);
                if (m.Groups[5].Value.Contains("W"))
                {
                    ln *= -1.0;
                }

                if (m.Groups.Count >= 10)
                {
                    alt = Convert.ToDouble(m.Groups[9].Value, CultureInfo.InvariantCulture);
                }
            }

            return (lt, ln, alt);
        }

        public static double CalculateDistance(double latitude1, double longitude1,
            double latitude2, double longitude2)
        {
            var dLat = DegreesToRadians(latitude2 - latitude1);
            var dLon = DegreesToRadians(longitude2 - longitude1);

            latitude1 = DegreesToRadians(latitude1);
            latitude2 = DegreesToRadians(latitude2);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(latitude1) * Math.Cos(latitude2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadiusNm * c;
        }

        /// <summary>
        /// Calculate the delta for a new segment with length of targetDistanceInNm
        /// and perpendicular to existing segment at point 2
        /// </summary>
        public static (double deltaLatitude, double deltaLongitude) CalculatePerpendicular(double latitude1, double longitude1,
            double latitude2, double longitude2, double targetDistanceInNm)
        {
            var distance = CalculateDistance(latitude1, longitude1, latitude2, longitude2);

            var dLat = DegreesToRadians(latitude2 - latitude1);
            var dLon = DegreesToRadians(longitude2 - longitude1);

            var sign = Math.Sign(dLat) * Math.Sign(dLon);

            var distanceLat = Math.Abs(Math.Sin(dLat) * earthRadiusNm);
            var distanceLon = CalculateDistance(latitude2, longitude1, latitude2, longitude2);

            var latitudeInRad2 = DegreesToRadians(latitude2);

            var newDistanceLat = distanceLon * targetDistanceInNm / distance;
            var newDistanceLon = distanceLat * targetDistanceInNm / distance;

            var newDLat = Math.Asin(newDistanceLat / earthRadiusNm);

            var a = Math.Cos(latitudeInRad2) * Math.Cos(latitudeInRad2) + 1;
            var t = Math.Tan(newDistanceLon / 2 / earthRadiusNm);
            var b = 1 / (t * t) + 1;
            var newDLon = Math.Asin(Math.Sqrt(1 / Math.Abs(a * b))) * 2;

            return (RadiansToDegrees(newDLat), RadiansToDegrees(newDLon) * -sign);
        }

        private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
        private static double RadiansToDegrees(double radians) => radians / Math.PI * 180;

        /// <summary>
        /// Based on https://stackoverflow.com/questions/8981943/lat-long-to-x-y-z-position-in-js-not-working
        /// Assuming the earth is a perfect sphere
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="altitude"></param>
        /// <returns></returns>
        public static (double x, double y, double z) ToXyz(double latitude, double longitude, double altitude)
        {
            var cosLat = Math.Cos(DegreesToRadians(latitude));
            var sinLat = Math.Sin(DegreesToRadians(latitude));
            var cosLon = Math.Cos(DegreesToRadians(longitude));
            var sinLon = Math.Sin(DegreesToRadians(longitude));
            var rad = 500.0;
            return (rad * cosLat * cosLon, rad * cosLat * sinLon, rad * sinLat);
        }

        /// <summary>
        /// Test whether two line segments intersect. If so, calculate the intersection point.
        /// <see cref="http://stackoverflow.com/a/14143738/292237"/>
        /// </summary>
        /// <param name="p">Vector to the start point of p.</param>
        /// <param name="p2">Vector to the end point of p.</param>
        /// <param name="q">Vector to the start point of q.</param>
        /// <param name="q2">Vector to the end point of q.</param>
        /// <param name="intersection">The point of intersection, if any.</param>
        /// <param name="considerOverlapAsIntersect">Do we consider overlapping lines as intersecting?
        /// </param>
        /// <returns>True if an intersection point was found.</returns>
        public static bool LineSegmentsIntersect(Vector p, Vector p2, Vector q, Vector q2,
            out Vector intersection, bool considerCollinearOverlapAsIntersect = false)
        {
            intersection = new Vector();

            var r = p2 - p;
            var s = q2 - q;
            var rxs = r.Cross(s);
            var qpxr = (q - p).Cross(r);

            // If r x s = 0 and (q - p) x r = 0, then the two lines are collinear.
            if (rxs.IsZero() && qpxr.IsZero())
            {
                // 1. If either  0 <= (q - p) * r <= r * r or 0 <= (p - q) * s <= * s
                // then the two lines are overlapping,
                if (considerCollinearOverlapAsIntersect)
                    if ((0 <= (q - p) * r && (q - p) * r <= r * r) || (0 <= (p - q) * s && (p - q) * s <= s * s))
                        return true;

                // 2. If neither 0 <= (q - p) * r = r * r nor 0 <= (p - q) * s <= s * s
                // then the two lines are collinear but disjoint.
                // No need to implement this expression, as it follows from the expression above.
                return false;
            }

            // 3. If r x s = 0 and (q - p) x r != 0, then the two lines are parallel and non-intersecting.
            if (rxs.IsZero() && !qpxr.IsZero())
                return false;

            // t = (q - p) x s / (r x s)
            var t = (q - p).Cross(s) / rxs;

            // u = (q - p) x r / (r x s)

            var u = (q - p).Cross(r) / rxs;

            // 4. If r x s != 0 and 0 <= t <= 1 and 0 <= u <= 1
            // the two line segments meet at the point p + t r = q + u s.
            if (!rxs.IsZero() && (0 <= t && t <= 1) && (0 <= u && u <= 1))
            {
                // We can calculate the intersection point using either t or u.
                intersection = p + t * r;

                // An intersection was found.
                return true;
            }

            // 5. Otherwise, the two line segments are not parallel but do not intersect.
            return false;
        }
    }

    public class Vector
    {
        public double X;
        public double Y;

        // Constructors.
        public Vector(double x, double y) { X = x; Y = y; }
        public Vector() : this(double.NaN, double.NaN) { }

        public static Vector operator -(Vector v, Vector w)
        {
            return new Vector(v.X - w.X, v.Y - w.Y);
        }

        public static Vector operator +(Vector v, Vector w)
        {
            return new Vector(v.X + w.X, v.Y + w.Y);
        }

        public static double operator *(Vector v, Vector w)
        {
            return v.X * w.X + v.Y * w.Y;
        }

        public static Vector operator *(Vector v, double mult)
        {
            return new Vector(v.X * mult, v.Y * mult);
        }

        public static Vector operator *(double mult, Vector v)
        {
            return new Vector(v.X * mult, v.Y * mult);
        }

        public double Cross(Vector v)
        {
            return X * v.Y - Y * v.X;
        }

        public override bool Equals(object obj)
        {
            var v = (Vector)obj;
            return (X - v.X).IsZero() && (Y - v.Y).IsZero();
        }
    }

    public static class DoubleExtensions
    {
        private const double Epsilon = 1e-10;

        public static bool IsZero(this double d)
        {
            return Math.Abs(d) < Epsilon;
        }
    }
}
