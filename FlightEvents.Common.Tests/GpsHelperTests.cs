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
            var (latitude, longitude) = GpsHelper.ConvertString("N50° 1' 57.39\",E8° 32' 26.37\",+000338.14");
            
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("de-DE");
            var (latitudeDe, longitudeDe) = GpsHelper.ConvertString("N50° 1' 57.39\",E8° 32' 26.37\",+000338.14");

            Assert.AreEqual(latitude, latitudeDe);
            Assert.AreEqual(longitude, longitudeDe);

            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("fr-FR");
            var (latitudeFr, longitudeFr) = GpsHelper.ConvertString("N50° 1' 57.39\",E8° 32' 26.37\",+000338.14");

            Assert.AreEqual(latitude, latitudeFr);
            Assert.AreEqual(longitude, longitudeFr);
        }

        [TestMethod]
        public void TestFormatConvert()
        {
            var (latitude, longitude) = GpsHelper.ConvertString("N50° 1' 57.39\",E8° 32' 26.37\",+000338.14");
            var (latitudeSt, longitudeSt) = GpsHelper.ConvertString("N50* 1' 57.39\",E8* 32' 26.37\",+000338.14");

            Assert.AreEqual(latitude, latitudeSt);
            Assert.AreEqual(longitude, longitudeSt);
        }
    }
}
