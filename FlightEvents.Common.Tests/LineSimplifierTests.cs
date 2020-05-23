using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FlightEvents.Common.Tests
{
    [TestClass]
    public class LineSimplifierTests
    {
        [TestMethod]
        public async Task TestImprovement()
        {
            var lineSimplifier = new LineSimplifier();
            await TestAsync(lineSimplifier, "../../../route.json");
            await TestAsync(lineSimplifier, "../../../route2.json");
        }

        private static async Task TestAsync(LineSimplifier lineSimplifier, string path)
        {
            var route = JsonConvert.DeserializeObject<List<AircraftStatusBrief>>(await File.ReadAllTextAsync(Path.Combine(Directory.GetCurrentDirectory(), path)));

            for (var t = 0.0001; t < 0.001; t += 0.0001)
            {
                var simplifiedRoute = lineSimplifier.DouglasPeucker(route, t).ToList();

                Debug.WriteLine($"Tolerant: {t}. Original: {route.Count}. Simplified: {simplifiedRoute.Count}.");

                Assert.IsTrue(route.Count > simplifiedRoute.Count);
            }
        }
    }
}
