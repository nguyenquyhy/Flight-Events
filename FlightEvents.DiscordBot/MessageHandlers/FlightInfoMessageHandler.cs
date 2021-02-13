using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlightEvents.DiscordBot.MessageHandlers
{
    public class FlightInfoMessageHandler : IMessageHandler
    {
        private readonly Regex command = new Regex("!finfo (.*)", RegexOptions.IgnoreCase);

        private readonly DiscordOptions discordOptions;
        private readonly ILogger<FlightInfoMessageHandler> logger;
        private readonly HubConnection hub;
        private readonly HttpClient httpClient;

        public FlightInfoMessageHandler(
            ILogger<FlightInfoMessageHandler> logger,
            IOptionsMonitor<DiscordOptions> discordOptionsAccessor,
            HubConnection hub)
        {
            this.discordOptions = discordOptionsAccessor.CurrentValue;
            this.logger = logger;
            this.hub = hub;
            this.httpClient = new HttpClient();

            hub.On<ulong, string, AircraftStatus>("UpdateAircraftToDiscord", (discordClientId, clientId, status) =>
            {
                if (sources.TryRemove(discordClientId, out var tcs))
                {
                    tcs.SetResult((clientId, status));
                }
            });
        }

        public async Task<bool> ProcessAsync(SocketMessage message)
        {
            if (message.Channel is SocketTextChannel channel)
            {
                var guild = channel.Guild;
                var options = discordOptions.Servers.FirstOrDefault(o => o.ServerId == guild.Id);
                if (options != null)
                {
                    var match = command.Match(message.Content);

                    if (match.Success)
                    {
                        var nameToFind = match.Groups[1].Value.Trim();

                        var guildUser = guild.GetUser(message.Author.Id);
                        if (options.FlightInfoRoleId == null || guildUser.Roles.Any(o => o.Id == options.FlightInfoRoleId.Value))
                        {
                            var foundUsers = guild.Users.Where(o => !o.IsBot && (
                                o.Username.Equals(nameToFind, StringComparison.OrdinalIgnoreCase) ||
                                (o.Nickname != null && (
                                    o.Nickname.Equals(nameToFind, StringComparison.OrdinalIgnoreCase) ||
                                    o.Nickname.StartsWith(nameToFind + " [", StringComparison.OrdinalIgnoreCase)
                                ))
                            )).ToList();

                            if (foundUsers.Count == 1)
                            {
                                var foundName = foundUsers[0].Nickname ?? foundUsers[0].Username;

                                logger.LogInformation("Requesting flight information of {id} {name}", foundUsers[0].Id, foundName);
                                var (clientId, status) = await RequestStatusAsync(foundUsers[0].Id);

                                if (status != null)
                                {
                                    string icao = null;
                                    string airport = null;
                                    try
                                    {
                                        var dataString = await httpClient.GetStringAsync($"http://iatageo.com/getCode/{status.Latitude}/{status.Longitude}");
                                        var result = JsonConvert.DeserializeObject<IATAGeoResult>(dataString);
                                        icao = result.ICAO;
                                        airport = result.name;
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.LogError(ex, "Cannot get ICAO code of {latitude} {longitude}!", status.Latitude, status.Longitude);
                                    }

                                    string country = null;
                                    if (!string.IsNullOrEmpty(icao))
                                    {
                                        try
                                        {
                                            var dataString = await httpClient.GetStringAsync($"https://www.airport-data.com/api/ap_info.json?icao={icao}");
                                            var result = JsonConvert.DeserializeObject<AirportDataResult>(dataString);
                                            country = result.country;
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.LogError(ex, "Cannot get airport information of {icao}!", icao);
                                        }
                                    }

                                    var location = $"{Math.Round(status.Latitude, 4)} {Math.Round(status.Longitude, 4)}";

                                    var response = foundName;
                                    response += status.IsOnGround ?
                                        " is on the ground" : " is flying";
                                    if (!string.IsNullOrEmpty(airport))
                                    {
                                        response += " near " + airport;
                                    }
                                    if (!string.IsNullOrEmpty(icao))
                                    {
                                        response += $" ({icao})";
                                        location = icao + " | " + location;
                                    }
                                    if (!string.IsNullOrEmpty(country))
                                    {
                                        response += $" in {country}";
                                        location += " | " + country;
                                    }

                                    await channel.SendMessageAsync(response, embed:
                                        new EmbedBuilder()
                                            .WithTitle(status.Callsign)
                                            .WithDescription($"- Altitude: {Math.Round(status.Altitude)} ft\n- Heading: {Math.Round(status.TrueHeading)}°\n- Airspeed: {Math.Round(status.IndicatedAirSpeed)} kt\n- Ground Speed: {Math.Round(status.GroundSpeed)} ft")
                                            .WithFooter(location)
                                            .Build());
                                }
                                else
                                {
                                    await channel.SendMessageAsync($"{foundName} is not connected to Flight Events or Flight Events client is not connected to Discord.");

                                }

                                return true;
                            }
                            else
                            {
                            }
                        }
                    }
                }
            }
            return false;
        }

        private readonly ConcurrentDictionary<ulong, TaskCompletionSource<(string, AircraftStatus)>> sources =
            new ConcurrentDictionary<ulong, TaskCompletionSource<(string, AircraftStatus)>>();

        private async Task<(string, AircraftStatus)> RequestStatusAsync(ulong discordUserId)
        {
            if (sources.TryGetValue(discordUserId, out var tcs))
            {
                return await tcs.Task;
            }
            else
            {
                tcs = new TaskCompletionSource<(string, AircraftStatus)>();
                sources.TryAdd(discordUserId, tcs);

                await hub.SendAsync("RequestStatusFromDiscord", discordUserId);

                return await tcs.Task;
            }
        }
    }

}
