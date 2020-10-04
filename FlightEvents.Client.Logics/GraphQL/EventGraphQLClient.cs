using FlightEvents.Data;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FlightEvents.Client.Logics
{
    public class EventGraphQLClient : IEventGraphQLClient
    {
        private readonly string webServerUrl;
        private readonly HttpClient httpClient;

        public EventGraphQLClient(IOptionsMonitor<AppSettings> appSettings)
        {
            webServerUrl = appSettings.CurrentValue.WebServerUrl;
            httpClient = new HttpClient();
        }

        public async Task<IEnumerable<FlightEvent>> GetFlightEventsAsync()
        {
            var response = await httpClient.PostAsync(webServerUrl + "/GraphQL/", new StringContent(JsonSerializer.Serialize(
                new
                {
                    query = @"{
    flightEvents(upcoming: true) {
        id
        type
        name
        startDateTime
        endDateTime
        url
        checklistItems {
            type
            subType
            title
            discordServerId
            discordChannelId
            links {
                type
                url
            }
        }
    }
}"
                }), Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();

            using var dataStream = await response.Content.ReadAsStreamAsync();
            var data = await JsonSerializer.DeserializeAsync<GetFlightEventsResponse>(dataStream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters =
                {
                    new JsonStringEnumConverter(new UpperCaseJsonNamingPolicy())
                }
            });

            return data.Data.FlightEvents;
        }

        private class GetFlightEventsResponse
        {
            public GetFlightEventsResponseData Data { get; set; }
        }

        private class GetFlightEventsResponseData
        {
            public IEnumerable<FlightEvent> FlightEvents { get; set; }
        }
    }

    public class UpperCaseJsonNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return name.ToUpperInvariant();
        }
    }
}
