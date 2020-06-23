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
    }
}
