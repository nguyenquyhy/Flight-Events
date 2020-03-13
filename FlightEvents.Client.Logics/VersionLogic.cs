using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlightEvents.Client.Logics
{
    public class VersionLogic
    {
        private readonly string versionsUrl;

        public VersionLogic(string versionsUrl)
        {
            this.versionsUrl = versionsUrl;
        }

        public Version GetVersion() => System.Reflection.Assembly.GetEntryAssembly().GetName().Version;

        public async Task<Version> GetUpdatedVersionAsync()
        {
            using var httpClient = new HttpClient();
            using var stream = await httpClient.GetStreamAsync(versionsUrl);
            var data = await JsonSerializer.DeserializeAsync<ClientVersions>(stream,
                options: new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            var latest = Version.Parse(data.Versions.First().Id);
            return latest > GetVersion() ? latest : null;
        }
    }
    public class ClientVersions
    {
        public List<ClientVersion> Versions { get; set; }
    }

    public class ClientVersion
    {
        public string Id { get; set; }
        public string Changes { get; set; }
    }
}
