using DiscordRPC;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace FlightEvents.Client.Logics
{
    public class DiscordRichPresentLogic
    {
        private readonly ILogger<DiscordRichPresentLogic> logger;
        private readonly DiscordRpcClient discordRpcClient;
        private readonly HttpClient httpClient;

        private readonly Dictionary<string, AirportDataResult> cachedAirports = new Dictionary<string, AirportDataResult>();
        private readonly ThrottleExecutor updateExecutor = new ThrottleExecutor(TimeSpan.FromMilliseconds(1000));
        private readonly ThrottleExecutor geocodeExecutor = new ThrottleExecutor(TimeSpan.FromMilliseconds(60000));

        private AircraftStatus lastStatus = null;
        private Timestamps groundStateChanged = null;

        private bool isStarted = false;
        private bool isConnected = false;

        private string callsign;
        private string lastICAO = null;
        private string lastAirport = null;

        public DiscordRichPresentLogic(ILogger<DiscordRichPresentLogic> logger, DiscordRpcClient discordRpcClient, IFlightConnector flightConnector)
        {
            this.logger = logger;
            this.discordRpcClient = discordRpcClient;

            httpClient = new HttpClient();

            flightConnector.Connected += FlightConnector_Connected;
            flightConnector.Closed += FlightConnector_Closed;
            flightConnector.AircraftStatusUpdated += FlightConnector_AircraftStatusUpdated;
        }

        public void Initialize()
        {
            try
            {
                discordRpcClient.Initialize();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot initialize rich present!");
            }
        }

        public void Start(string callsign)
        {
            isStarted = true;
            this.callsign = callsign;
            if (isConnected)
            {
                SetPreparing();
            }
        }

        public void Stop()
        {
            isStarted = false;
            ClearPresent();
        }

        private void FlightConnector_Connected(object sender, EventArgs e)
        {
            isConnected = true;
            if (isStarted)
            {
                SetPreparing();
            }
        }

        private void FlightConnector_Closed(object sender, EventArgs e)
        {
            isConnected = false;
            ClearPresent();
        }

        private async void FlightConnector_AircraftStatusUpdated(object sender, AircraftStatusUpdatedEventArgs e)
        {
            // NOTE: do not need to check for isConnected because this event is not triggered if simconnect is not connected
            if (!isStarted) return;

            var status = e.AircraftStatus;
            
            DetectTakeOffLanding(status);

            lastStatus = status;

            await updateExecutor.ExecuteAsync(async () =>
            {
                if (Math.Abs(status.Latitude) < 0.02 && Math.Abs(status.Longitude) < 0.02)
                {
                    // Aircraft is not loaded
                    if (isStarted)
                    {
                        SetPreparing();
                    }
                }
                else
                {
                    try
                    {
                        string icao = lastICAO;
                        string airport = lastAirport;
                        await geocodeExecutor.ExecuteAsync(async () =>
                        {
                            try
                            {
                                var dataString = await httpClient.GetStringAsync($"http://iatageo.com/getCode/{e.AircraftStatus.Latitude.ToString(CultureInfo.InvariantCulture)}/{e.AircraftStatus.Longitude.ToString(CultureInfo.InvariantCulture)}");
                                var result = JsonConvert.DeserializeObject<IATAGeoResult>(dataString);
                                icao = result.ICAO;
                                airport = result.name;
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, $"Cannot get ICAO code of {status.Latitude} {status.Longitude}!");
                            }
                        });
                        lastICAO = icao;
                        lastAirport = airport;

                        string country = null;
                        if (!string.IsNullOrEmpty(icao))
                        {
                            if (cachedAirports.TryGetValue(icao, out var airportData))
                            {
                                country = airportData.country;
                            }
                            else
                            {
                                try
                                {
                                    var dataString = await httpClient.GetStringAsync($"https://www.airport-data.com/api/ap_info.json?icao={icao}");
                                    var result = JsonConvert.DeserializeObject<AirportDataResult>(dataString);
                                    cachedAirports.TryAdd(icao, result);
                                    country = result.country;
                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex, $"Cannot get airport information of {icao}!");
                                }
                            }
                        }

                        var tooltip = callsign;
                        var details = string.Empty;
                        if (!string.IsNullOrEmpty(airport))
                        {
                            tooltip += " near " + airport;
                        }
                        if (!string.IsNullOrEmpty(icao))
                        {
                            details += $" near {icao}";
                            tooltip += $" ({icao})";
                        }
                        if (!string.IsNullOrEmpty(country))
                        {
                            details += $", {country}";
                            tooltip += $" in {country}";
                        }

                        discordRpcClient.SetPresence(new RichPresence
                        {
                            Details = details.Trim(),
                            State = status.IsOnGround ? "on the ground" : $"alt {Math.Round(status.Altitude)} ft",
                            Assets = new Assets
                            {
                                LargeImageKey = "icon_large",
                                LargeImageText = tooltip.Trim()
                            },
                            Timestamps = groundStateChanged
                        });
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Cannot update rich present!");
                    }
                }
            });
        }

        private void DetectTakeOffLanding(AircraftStatus status)
        {
            if (lastStatus == null || status.IsOnGround != lastStatus.IsOnGround)
            {
                groundStateChanged = Timestamps.Now;
            }
        }

        private void SetPreparing()
        {
            try
            {
                discordRpcClient.SetPresence(new RichPresence()
                {
                    State = "Preparing...",
                    Assets = new Assets
                    {
                        LargeImageKey = "icon_large",
                        LargeImageText = "Flight Events"
                    }
                });
                isStarted = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot start rich present!");
            }
        }

        private void ClearPresent()
        {
            try
            {
                discordRpcClient.ClearPresence();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot stop rich present!");
            }
        }
    }

    public class ThrottleExecutor
    {
        private readonly TimeSpan interval;
        private DateTime lastExecution = DateTime.MinValue;

        public ThrottleExecutor(TimeSpan interval)
        {
            this.interval = interval;
        }

        public async Task ExecuteAsync(Func<Task> action)
        {
            if (DateTime.Now - lastExecution < interval) return;
            lastExecution = DateTime.Now;
            await action();
        }
    }
}
