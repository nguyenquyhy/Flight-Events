using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;

namespace FlightEvents.Common.Tests
{
    [TestClass]
    public class GpsHelperTests
    {
        [TestMethod]
        public void TestCultureConvert()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            var (latitude, longitude, altitude) = GpsHelper.ConvertString("N50° 1' 57.39\",E8° 32' 26.37\",+000338.14");

            Assert.AreEqual(338.14, altitude);

            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("de-DE");
            var (latitudeDe, longitudeDe, altitudeDe) = GpsHelper.ConvertString("N50° 1' 57.39\",E8° 32' 26.37\",+000338.14");

            Assert.AreEqual(338.14, altitudeDe);
            Assert.AreEqual(latitude, latitudeDe);
            Assert.AreEqual(longitude, longitudeDe);

            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("fr-FR");
            var (latitudeFr, longitudeFr, altitudeFr) = GpsHelper.ConvertString("N50° 1' 57.39\",E8° 32' 26.37\",+000338.14");

            Assert.AreEqual(338.14, altitudeFr);
            Assert.AreEqual(latitude, latitudeFr);
            Assert.AreEqual(longitude, longitudeFr);
        }

        [TestMethod]
        public void TestFormatConvert()
        {
            var (latitude, longitude, _) = GpsHelper.ConvertString("N50° 1' 57.39\",E8° 32' 26.37\",+000338.14");
            var (latitudeSt, longitudeSt, _) = GpsHelper.ConvertString("N50* 1' 57.39\",E8* 32' 26.37\",+000338.14");

            Assert.AreEqual(latitude, latitudeSt);
            Assert.AreEqual(longitude, longitudeSt);
        }

        [TestMethod]
        public void TestPerpendicular()
        {
            //var lat1 = 35.91261825928116;
            //var lon1 = -113.71721493789124;
            //var lat2 = 35.912017916060556;
            //var lon2 = -113.7172875911555;
            var lat1 = 35.905402778;
            var lon1 = -113.717561111;
            var lat2 = 35.880433333;
            var lon2 = -113.709511111;
            var distance = 0.05399568 * 5;
            var (deltaLat, deltaLon) = GpsHelper.CalculatePerpendicular(lat1, lon1, lat2, lon2, distance * 2);

            var lat3 = lat2 + deltaLat;
            var lon3 = lon2 + deltaLon;
            var lat4 = lat2 - deltaLat;
            var lon4 = lon2 - deltaLon;
        }
    }
}
