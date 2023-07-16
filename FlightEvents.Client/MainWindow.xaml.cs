using FlightEvents.Client.ATC;
using FlightEvents.Client.Dialogs;
using FlightEvents.Client.Logics;
using FlightEvents.Client.ViewModels;
using FlightEvents.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FlightEvents.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int MinimumUpdatePeriod = 500;

        private readonly ConcurrentDictionary<string, Airport> airports = new ConcurrentDictionary<string, Airport>();

        private readonly Random random = new Random();

        private readonly MainViewModel viewModel;
        private readonly DiscordRichPresentLogic discordRichPresentLogic;
        private readonly AppSettings appSettings;
        private readonly LineSimplifier lineSimplifier;
        private readonly ATCServer atcServer;
        private readonly UserPreferencesLoader userPreferencesLoader;
        private readonly VersionLogic versionLogic;
        private readonly UdpBroadcastLogic udpBroadcastLogic;
        private readonly ILogger<MainWindow> logger;
        private readonly IFlightConnector flightConnector;

        private HubConnection hub;

        public MainWindow(ILogger<MainWindow> logger, IFlightConnector flightConnector, MainViewModel viewModel,
            IOptionsMonitor<AppSettings> appSettings,
            DiscordRichPresentLogic discordRichPresentLogic,
            ATCServer atcServer, UserPreferencesLoader userPreferencesLoader, VersionLogic versionLogic,
            UdpBroadcastLogic udpBroadcastLogic)
        {
            InitializeComponent();

            this.logger = logger;
            this.flightConnector = flightConnector;
            this.atcServer = atcServer;
            this.userPreferencesLoader = userPreferencesLoader;
            this.versionLogic = versionLogic;
            this.udpBroadcastLogic = udpBroadcastLogic;
            this.viewModel = viewModel;
            this.discordRichPresentLogic = discordRichPresentLogic;
            this.appSettings = appSettings.CurrentValue;
            this.lineSimplifier = new LineSimplifier();

            flightConnector.AircraftStatusUpdated += FlightConnector_AircraftStatusUpdated;
            flightConnector.AircraftPositionChanged += FlightConnector_AircraftPositionChanged;
            flightConnector.AirportListReceived += FlightConnector_AirportListReceived;
            flightConnector.Error += FlightConnector_Error;

            DataContext = viewModel;
        }

        #region Interaction

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TextURL.Text = this.appSettings.WebServerUrl;

            var currentVersion = versionLogic.GetVersion();
            UserPreferences pref;
            try
            {
                pref = await userPreferencesLoader.LoadAsync();
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogError(ex, "Cannot save preferences!");
                ShowSavingPreferenceErrorMessageAndShutDown();
                return;
            }
            catch (IOException ex)
            {
                logger.LogError(ex, "Cannot save preferences!");
                ShowSavingPreferenceErrorMessageAndShutDown();
                return;
            }

            viewModel.DisableDiscordRP = pref.DisableDiscordRP;
            viewModel.BroadcastUDP = pref.BroadcastUDP;
            viewModel.BroadcastIP = pref.BroadcastIP;
            viewModel.SlowMode = pref.SlowMode;
            viewModel.MinimizeToTaskbar = pref.MinimizeToTaskbar;
            viewModel.ShowLandingInfo = pref.ShowLandingInfo;

            atcServer.FlightPlanRequested += AtcServer_FlightPlanRequested;
            atcServer.Connected += AtcServer_Connected;
            atcServer.MessageSent += AtcServer_MessageSent;
            atcServer.AtcUpdated += AtcServer_AtcUpdated;
            atcServer.AtcLoggedOff += AtcServer_AtcLoggedOff;
            atcServer.AtcMessageSent += AtcServer_AtcMessageSent;

            if (string.IsNullOrWhiteSpace(viewModel.Callsign))
            {
                viewModel.Callsign = string.IsNullOrWhiteSpace(pref.LastCallsign) ? GenerateCallSign() : pref.LastCallsign;
            }

            try
            {
                this.Title = "Flight Events " + currentVersion;
                var version = await versionLogic.GetUpdatedVersionAsync();
                if (version != null)
                {
                    var result = MessageBox.Show(this, $"A new version {version.ToString()} is available.\nDo you want to download it?", "Flight Events Update", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            UseShellExecute = true,
                            FileName = "https://events-storage.flighttracker.tech/downloads/FlightEvents.Client.zip"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot check for update!");
            }

            try
            {
                await viewModel.InitializeAsync(pref.ClientId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cannot initialize View Model");
            }

            discordRichPresentLogic.Initialize();

            using var httpClient = new HttpClient();
            ClientVersion versionData;
            while (true)
            {
                try
                {
                    viewModel.HubConnectionState = ConnectionState.Connecting;

                    var dataString = await httpClient.GetStringAsync($"{this.appSettings.WebServerUrl}/api/ClientVersions/{currentVersion}");
                    versionData = JsonSerializer.Deserialize<ClientVersion>(dataString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    break;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Cannot connect to server! Retry in 10s...");
                    await Task.Delay(10000);
                }
            }

            var announcement = versionData?.Announcements?.FirstOrDefault()?.Content;
            if (!string.IsNullOrWhiteSpace(announcement))
            {
                MessageBox.Show(announcement, "Flight Events", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            await InitializeHubAsync(currentVersion, pref.ClientId, versionData?.Features?.UseMessagePack == true);

            if (viewModel.BroadcastUDP)
            {
                await StartUDPConnectionAsync();
            }
        }

        private async void ButtonStartTrack_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                flightConnector.Send("Connected to Flight Events!");
            }
            catch (COMException ex) when (ex.Message == "0xC000014B" || ex.Message == "0xC00000B0")
            {
                // broken pipe
            }
            catch (BadImageFormatException ex)
            {
                logger.LogError(ex, "Cannot initialize SimConnect!");
                ShowSimConnectErrorMessage();
                return;
            }

            route.Clear();

            viewModel.IsTracking = true;

            try
            {
                await userPreferencesLoader.UpdateAsync(o => o.LastCallsign = viewModel.Callsign);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogError(ex, "Cannot save preferences!");
                ShowSavingPreferenceErrorMessageAndShutDown();
                return;
            }

            ButtonStartTrack.Visibility = Visibility.Collapsed;
            ButtonStopTrack.Visibility = Visibility.Visible;

            if (!viewModel.DisableDiscordRP)
            {
                discordRichPresentLogic.Start(viewModel.Callsign);
            }
        }

        private void ButtonStopTrack_Click(object sender, RoutedEventArgs e)
        {
            route.Clear();

            viewModel.IsTracking = false;
            ButtonStopTrack.Visibility = Visibility.Collapsed;
            ButtonStartTrack.Visibility = Visibility.Visible;

            if (!viewModel.DisableDiscordRP)
            {
                discordRichPresentLogic.Stop();
            }
        }

        private void ButtonStartATC_Click(object sender, RoutedEventArgs e)
        {
            ButtonStartATC.IsEnabled = false;
            try
            {
                atcServer.Start(viewModel.VatsimMode);
                viewModel.AtcConnectionState = ConnectionState.Connecting;
            }
            catch (SocketException ex)
            {
                logger.LogWarning(ex, "Cannot start ATC server!");
                MessageBox.Show(this, "Cannot start ATC server! Please make sure no other FSD server is running.", "ATC Server", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ButtonStopATC_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AtcCallsign = null;

            hub.Remove("UpdateAircraft");
            hub.Remove("ReturnFlightPlan");

            atcServer.Stop();

            await hub.SendAsync("Leave", "ATC");
            await hub.SendAsync("UpdateATC", null);

            ButtonStopATC.Visibility = Visibility.Collapsed;
            ButtonStartATC.Visibility = Visibility.Visible;
            ButtonStartATC.IsEnabled = true;
        }

        private void TextURL_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var url = TextURL.Text;
                if (!string.IsNullOrEmpty(url) && viewModel.IsTracking)
                {
                    var queries = new List<string>();
                    if (!string.IsNullOrEmpty(viewModel.Callsign))
                    {
                        queries.Add("myCallsign=" + Uri.EscapeDataString(viewModel.Callsign));
                    }
                    if (!string.IsNullOrEmpty(viewModel.Group))
                    {
                        queries.Add("group=" + Uri.EscapeDataString(viewModel.Group));
                    }
                    if (queries.Count > 0)
                    {
                        url += "?" + string.Join('&', queries);
                    }
                }


                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch { }
        }

        #endregion

        #region SignalR

        private async Task InitializeHubAsync(Version currentVersion, string clientId, bool useMessagePack)
        {
            var builder = new HubConnectionBuilder()
                .WithUrl($"{this.appSettings.WebServerUrl}/FlightEventHub?clientType=Client&clientVersion={currentVersion}&clientId={clientId}")
                .WithAutomaticReconnect();
            if (useMessagePack)
            {
                builder.AddMessagePackProtocol();
            }

            hub = builder.Build();

            hub.Closed += Hub_Closed;
            hub.Reconnecting += Hub_Reconnecting;
            hub.Reconnected += Hub_Reconnected;

            hub.On<string>("RequestAircraftInfo", Hub_OnRequestAircraftInfo);
            hub.On<string, string>("RequestFlightPlan", Hub_OnRequestFlightPlan);
            hub.On<string>("RequestFlightPlanDetails", Hub_OnRequestFlightPlanDetails);
            hub.On<string>("RequestFlightRoute", Hub_OnRequestFlightRoute);
            hub.On<string, string, string>("SendMessage", Hub_OnMessageSent);
            hub.On<string, int>("ChangeUpdateRateByCallsign", Hub_OnChangeUpdateRateByCallsign);
            hub.On<string, AircraftStatus>("UpdateAircraft", Hub_OnAircraftUpdated);
            hub.On<string, AircraftPosition>("Teleport", Hub_OnTeleport);
            hub.On("NotifyEventsUpdated", Hub_OnNotifyEventsUpdated);

            while (true)
            {
                try
                {
                    viewModel.HubConnectionState = ConnectionState.Connecting;
                    await hub.StartAsync();
                    viewModel.HubConnectionState = ConnectionState.Connected;

                    ButtonStartATC.IsEnabled = true;

                    break;
                }
                catch (HttpRequestException ex)
                {
                    logger.LogWarning(ex, "Cannot connect to SignalR server! Retry in 5s...");
                    await Task.Delay(5000);
                }
                catch (HubException ex)
                {
                    logger.LogWarning(ex, "Cannot connect to SignalR server! Retry in 5s...");
                    await Task.Delay(5000);
                }
                catch (OperationCanceledException ex)
                {
                    logger.LogWarning(ex, "Cannot connect to SignalR server! Retry in 5s...");
                    await Task.Delay(5000);
                }
                catch (SocketException ex)
                {
                    logger.LogWarning(ex, "Cannot connect to SignalR server! Retry in 5s...");
                    await Task.Delay(5000);
                }
                catch (WebSocketException ex)
                {
                    logger.LogWarning(ex, "Cannot connect to SignalR server! Retry in 5s...");
                    await Task.Delay(5000);
                }
                catch (IOException ex)
                {
                    logger.LogWarning(ex, "Cannot connect to SignalR server! Retry in 5s...");
                    await Task.Delay(5000);
                }
            }
        }

        private async Task Hub_Reconnected(string arg)
        {
            viewModel.HubConnectionState = ConnectionState.Connected;

            if (!string.IsNullOrEmpty(viewModel.AtcCallsign))
            {
                await hub.SendAsync("Join", "ATC");
            }

            await viewModel.ReconnectedAsync();
        }

        private Task Hub_Reconnecting(Exception arg)
        {
            viewModel.HubConnectionState = ConnectionState.Connecting;
            return Task.CompletedTask;
        }

        private Task Hub_Closed(Exception arg)
        {
            viewModel.HubConnectionState = ConnectionState.Failed;
            return Task.CompletedTask;
        }

        private async void Hub_OnRequestAircraftInfo(string requesterConnectionId)
        {
            try
            {
                var aircraftData = await flightConnector.RequestAircraftDataAsync(new CancellationTokenSource(5000).Token);
                if (aircraftData != null)
                {
                    await hub.SendAsync("ReturnAircraftInfo", requesterConnectionId, aircraftData);
                }
            }
            catch (TaskCanceledException ex)
            {
                logger.LogError(ex, "Cannot get aircraft info!");
            }
            catch (COMException ex)
            {
                logger.LogError(ex, "Cannot get aircraft info!");
            }
        }

        /// <summary>
        /// Flight plan is requested from a broadcast by ATC client
        /// </summary>
        private async void Hub_OnRequestFlightPlan(string atcConnectionId, string callsign)
        {
            if (viewModel.Callsign == callsign)
            {
                try
                {
                    var aircraftData = await flightConnector.RequestAircraftDataAsync(new CancellationTokenSource(5000).Token);
                    var data = await flightConnector.RequestFlightPlanAsync(new CancellationTokenSource(15000).Token);
                    if (data != null)
                    {
                        var flightPlan = new FlightPlanCompact(data, viewModel.Callsign, aircraftData.Title, (int)aircraftData.EstimatedCruiseSpeed, viewModel.Remarks);
                        var pref = await userPreferencesLoader.LoadAsync();
                        await hub.SendAsync("ReturnFlightPlan", pref.ClientId, flightPlan, new string[] { atcConnectionId });
                    }
                }
                catch (TaskCanceledException ex)
                {
                    logger.LogError(ex, "Cannot get flight plan for ATC!");
                }
                catch (COMException ex)
                {
                    logger.LogError(ex, "Cannot get flight plan for ATC!");
                }
            }
        }

        /// <summary>
        /// Flight plan is requested from web client
        /// </summary>
        private async void Hub_OnRequestFlightPlanDetails(string webConnectionId)
        {
            try
            {
                var data = await flightConnector.RequestFlightPlanAsync(new CancellationTokenSource(15000).Token);
                await hub.SendAsync("ReturnFlightPlanDetails", hub.ConnectionId, data, webConnectionId);
            }
            catch (TaskCanceledException ex)
            {
                logger.LogError(ex, "Cannot get flight plan for map!");
            }
            catch (COMException ex)
            {
                logger.LogError(ex, "Cannot get flight plan for map!");
            }
        }

        /// <summary>
        /// Flight route is requested from web client
        /// </summary>
        private async void Hub_OnRequestFlightRoute(string webConnectionId)
        {
            await hub.SendAsync("StreamFlightRoute", ClientStreamData());
        }

        IEnumerable<AircraftStatusBrief> ClientStreamData()
        {
            var copy = lineSimplifier.DouglasPeucker(route.ToList(), 0.0001).ToList();
            copy.Reverse();
            foreach (var status in copy)
            {
                yield return status;
            }
        }

        private void Hub_OnMessageSent(string from, string toRaw, string message)
        {
            if (viewModel.IsTracking && viewModel.Callsign != from)
            {
                var tos = toRaw.Split("&");
                foreach (var to in tos)
                {
                    if (to.StartsWith("@"))
                    {
                        // @18700 @22900&@20000
                        var frequency = "1" + to.Substring(1);
                        if (frequency == viewModel.AircraftStatus.FrequencyCom1.ToString() || frequency == viewModel.AircraftStatus.FrequencyCom2.ToString())
                        {
                            flightConnector.Send($"{from} [{frequency}]: {message}");
                        }
                    }
                    else if (to == viewModel.Callsign)
                    {
                        flightConnector.Send($"{from}: {message}");
                    }
                }
            }
        }

        private void Hub_OnChangeUpdateRateByCallsign(string callsign, int hz)
        {
            if (viewModel.Callsign == callsign)
            {
                MinimumUpdatePeriod = 1000 / hz;
                hub.SendAsync("NotifyUpdateRateChanged", hz);
            }
        }

        private async void Hub_OnAircraftUpdated(string clientId, AircraftStatus status)
        {
            if (viewModel.IsTracking && viewModel.AircraftStatus != null &&
                GpsHelper.CalculateDistance(status.Latitude, status.Longitude, viewModel.AircraftStatus.Latitude, viewModel.AircraftStatus.Longitude) < 10000)
            {
                try
                {
                    var pref = await userPreferencesLoader.LoadAsync();
                    if (pref.ClientId != clientId)
                    {
                        await udpBroadcastLogic.SendTrafficAsync(status);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Cannot send network package!");
                }
            }
        }

        private void Hub_OnTeleport(string connectionId, AircraftPosition position)
        {
            flightConnector.Teleport(position.Latitude, position.Longitude, position.Altitude);
        }

        private async void Hub_OnNotifyEventsUpdated()
        {
            await viewModel.RefreshEventsAsync();
        }

        #endregion

        #region SimConnect

        DateTime lastStatusSent = DateTime.Now;
        AircraftStatus lastStatus = null;
        private bool landed = false;

        private readonly List<AircraftStatusBrief> route = new List<AircraftStatusBrief>();

        private async void FlightConnector_AircraftStatusUpdated(object sender, AircraftStatusUpdatedEventArgs e)
        {
            if (viewModel.IsTracking)
            {
                if (lastStatus?.IsOnGround == false && e.AircraftStatus.IsOnGround)
                {
                    landed = true;
                }
                if (landed && (e.AircraftStatus.TouchdownNormalVelocity > 0 || !e.AircraftStatus.IsOnGround))
                {
                    landed = false;
                    logger.LogInformation("Landing rate: {landingRate}. G-Force: {gforce}.", e.AircraftStatus.TouchdownNormalVelocity, e.AircraftStatus.GForce);

                    lastStatusSent = DateTime.MinValue;
                    if (viewModel.ShowLandingInfo)
                    {
                        flightConnector.Send($"Landing rate: {e.AircraftStatus.TouchdownNormalVelocity:0}fpm. G-Force: {e.AircraftStatus.GForce:0.00}g.");
                    }
                }

                e.AircraftStatus.Callsign = viewModel.Callsign;
                e.AircraftStatus.Group = viewModel.Group;
                e.AircraftStatus.TransponderMode = viewModel.TransponderIdent ? TransponderMode.Ident : TransponderMode.ModeC;

                if (DateTime.Now - lastStatusSent > TimeSpan.FromMilliseconds(MinimumUpdatePeriod))
                {
                    if (hub?.ConnectionId != null)
                    {
                        route.Add(new AircraftStatusBrief(e.AircraftStatus));

                        lastStatusSent = DateTime.Now;
                        await hub.SendAsync("UpdateAircraft", e.AircraftStatus);
                        lastStatusSent = DateTime.Now;

                        if (viewModel.TransponderIdent) viewModel.TransponderIdent = false;
                    }

                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    var minDistance = 0d;
                    Airport minAirport = null;
                    foreach (var airport in airports.Values)
                    {
                        var distance = GpsHelper.CalculateDistance(e.AircraftStatus.Latitude, e.AircraftStatus.Longitude, airport.Latitude, airport.Longitude);
                        if (minAirport == null || minDistance > distance)
                        {
                            minDistance = distance;
                            minAirport = airport;
                        }
                    }
                    if (minAirport != null)
                    {
                        viewModel.NearestAirport = minAirport;
                    }
                    //Debug.WriteLine($"Found closest airport in {stopwatch.ElapsedMilliseconds}ms");
                    stopwatch.Stop();
                }

                viewModel.AircraftStatus = e.AircraftStatus;

                try
                {
                    if (viewModel.BroadcastUDP)
                    {
                        await udpBroadcastLogic.SendGpsAsync(e.AircraftStatus);
                        await udpBroadcastLogic.SendAttitudeAsync(e.AircraftStatus);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Cannot send network package!");
                }
            }

            lastStatus = e.AircraftStatus;
        }

        private void FlightConnector_AircraftPositionChanged(object sender, EventArgs e)
        {
            route.Clear();
        }

        private void FlightConnector_AirportListReceived(object sender, AirportListReceivedEventArgs e)
        {
            foreach (var airport in e.Airports)
            {
                if (airports.TryAdd(airport.Ident, airport))
                {
                    Debug.WriteLine($"{airport.Ident} {airport.Latitude} {airport.Longitude}");
                }
            }
        }

        private void FlightConnector_Error(object sender, ConnectorErrorEventArgs e)
        {
            MessageBox.Show("Error connecting to simulator: " + e.SimConnectError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion

        #region Discord

        private async void ButtonDiscord_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ButtonDiscord.IsEnabled = false;
                await Task.Delay(500);
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"{appSettings.WebServerUrl}/Discord/Connect",
                    UseShellExecute = true
                });
            }
            catch { }
            finally
            {
                ButtonDiscord.IsEnabled = true;
            }
        }

        private async void ButtonDiscordConfirm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ButtonDiscordConfirm.IsEnabled = false;

                var userPref = await userPreferencesLoader.LoadAsync();

                using var httpClient = new HttpClient();
                var response = await httpClient.PostAsync($"{appSettings.WebServerUrl}/Discord/Confirm?clientId={userPref.ClientId}&code={TextDiscordConfirm.Text}", null);

                if (response.IsSuccessStatusCode)
                {
                    using var stream = await response.Content.ReadAsStreamAsync();
                    viewModel.DiscordConnection = await JsonSerializer.DeserializeAsync<DiscordConnection>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    MessageBox.Show(this, "You have connected this client to your Discord account.", "Flight Events", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(this, "Cannot connect this client to your Discord account.\nPlease try again.", "Flight Events", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            finally
            {
                ButtonDiscordConfirm.IsEnabled = true;
            }
        }

        private async void ButtonDiscordDisconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ButtonDiscordDisconnect.IsEnabled = false;

                var result = MessageBox.Show(this, "Do you want to disconnect this client from your Discord account.", "Flight Events", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    var userPref = await userPreferencesLoader.LoadAsync();
                    using (var httpClient = new HttpClient())
                    {
                        await httpClient.DeleteAsync($"{appSettings.WebServerUrl}/Discord/Connection/{userPref.ClientId}");
                    }
                    viewModel.DiscordConnection = null;
                }
            }
            finally
            {
                ButtonDiscordDisconnect.IsEnabled = true;
            }
        }

        #endregion

        #region ATC

        private async void AtcServer_Connected(object sender, ConnectedEventArgs e)
        {
            viewModel.AtcConnectionState = ConnectionState.Connected;
            viewModel.AtcCallsign = e.Callsign;

            // Register ATC specific events
            hub.On<string, AircraftStatus>("UpdateAircraft", async (connectionId, aircraftStatus) =>
            {
                await atcServer.SendPositionAsync(aircraftStatus.Callsign, aircraftStatus.Transponder,
                    aircraftStatus.Latitude, aircraftStatus.Longitude, aircraftStatus.Altitude, aircraftStatus.GroundSpeed,
                    aircraftStatus.Pitch, aircraftStatus.Bank, aircraftStatus.TrueHeading,
                    aircraftStatus.TransponderMode switch
                    {
                        TransponderMode.Standby => AtcTransponderMode.Standby,
                        TransponderMode.ModeC => AtcTransponderMode.ModeC,
                        TransponderMode.Ident => AtcTransponderMode.Ident,
                        _ => AtcTransponderMode.Standby
                    });
            });
            hub.On<string, FlightPlanCompact>("ReturnFlightPlan", async (clientId, flightPlan) =>
            {
                await atcServer.SendFlightPlanAsync(
                    flightPlan.Callsign,
                    flightPlan.Type,
                    flightPlan.AircraftType,
                    flightPlan.Callsign,
                    flightPlan.AircraftType,
                    flightPlan.Departure,
                    flightPlan.Destination,
                    flightPlan.Alternate,
                    flightPlan.Route,
                    flightPlan.CruisingSpeed,
                    flightPlan.CruisingAltitude,
                    flightPlan.EstimatedEnroute,
                    flightPlan.Remarks);
            });
            hub.On<string, string>("SendATC", async (to, message) =>
            {
                if (to == "*" || to == "@94835" || viewModel.AtcCallsign == to)
                {
                    await atcServer.SendAsync(message);
                }
            });
            await hub.SendAsync("Join", "ATC");

            // Update ATC Location on map
            await hub.SendAsync("LoginATC", new ATCInfo
            {
                Callsign = e.Callsign,
                Latitude = e.Latitude,
                Longitude = e.Longitude,
                RealName = e.RealName,
                Certificate = e.Certificate,
                Rating = e.Rating
            });

            Dispatcher.Invoke(() =>
            {
                ButtonStartATC.Visibility = Visibility.Collapsed;
                ButtonStopATC.Visibility = Visibility.Visible;
            });
        }

        private async void AtcServer_AtcLoggedOff(object sender, AtcLoggedOffEventArgs e)
        {
            // De-Register ATC specific events
            hub.Remove("UpdateAircraft");
            hub.Remove("ReturnFlightPlan");
            hub.Remove("SendATC");

            await hub.SendAsync("UpdateATC", null);

            Dispatcher.Invoke(() =>
            {
                viewModel.AtcCallsign = null;
                viewModel.AtcConnectionState = ConnectionState.Connecting;
                ButtonStartATC.Visibility = Visibility.Visible;
                ButtonStopATC.Visibility = Visibility.Collapsed;
            });
        }

        private async void AtcServer_FlightPlanRequested(object sender, FlightPlanRequestedEventArgs e)
        {
            await hub.SendAsync("RequestFlightPlan", e.Callsign);
        }

        private void AtcServer_IdentSent(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                viewModel.TransponderIdent = false;
            });
        }

        private async void AtcServer_MessageSent(object sender, MessageSentEventArgs e)
        {
            await hub.SendAsync("SendMessage", viewModel.AtcCallsign, e.To, e.Message);
        }

        private async void AtcServer_AtcUpdated(object sender, AtcUpdatedEventArgs e)
        {
            await hub.SendAsync("UpdateATC", new ATCStatus
            {
                Callsign = e.Callsign,
                Latitude = e.Latitude,
                Longitude = e.Longitude,
                Altitude = e.Altitude,
                FrequencyCom = e.Frequency
            });
        }

        private async void AtcServer_AtcMessageSent(object sender, AtcMessageSentEventArgs e)
        {
            await hub.SendAsync("SendATC", e.To, e.Message);
        }

        #endregion

        #region Teleport

        private async void ButtonTeleport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TeleportDialog
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var token = dialog.TextToken.Text;
                    await hub.SendAsync("AcceptTeleport", token);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Cannot accept teleport token!");
                    MessageBox.Show("Cannot accept teleport token!", "Flight Events", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Mics

        private string GenerateCallSign()
        {
            var builder = new StringBuilder();
            builder.Append(((char)('A' + random.Next(26))).ToString());
            builder.Append(((char)('A' + random.Next(26))).ToString());
            builder.Append("-");
            builder.Append(((char)('A' + random.Next(26))).ToString());
            builder.Append(((char)('A' + random.Next(26))).ToString());
            builder.Append(((char)('A' + random.Next(26))).ToString());
            return builder.ToString();
        }

        private async Task StartUDPConnectionAsync()
        {
            await udpBroadcastLogic.StartAsync(viewModel.BroadcastIP);
            if (hub?.ConnectionId != null)
            {
                await hub.SendAsync("Join", "ClientMap");
            }
        }

        private async Task StopUDPConnectionAsync()
        {
            await hub.SendAsync("Leave", "ClientMap");
            if (hub?.ConnectionId != null)
            {
                await udpBroadcastLogic.StopAsync();
            }
        }

        #endregion

        #region Minimize to System Tray

        private bool notified = false;

        private async void Window_StateChanged(object sender, EventArgs e)
        {
            var minimizeToTaskbar = await userPreferencesLoader.GetSettingsAsync(o => o.MinimizeToTaskbar);

            if (!minimizeToTaskbar && WindowState == WindowState.Minimized)
            {
                Hide();
                myNotifyIcon.Visibility = Visibility.Visible;
                if (!notified)
                {
                    notified = true;
                    myNotifyIcon.ShowBalloonTip("Minimized to system tray", "Click icon to restore the window.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                    await Task.Delay(3000);
                    myNotifyIcon.HideBalloonTip();
                }
            }
        }

        private void myNotifyIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            RestoreWindow();
        }

        public void RestoreWindow()
        {
            myNotifyIcon.Visibility = Visibility.Collapsed;
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        #endregion

        #region Settings

        private async void DisableDiscordRP_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                await userPreferencesLoader.UpdateAsync(pref => pref.DisableDiscordRP = true);

                if (viewModel.IsTracking)
                {
                    discordRichPresentLogic.Stop();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot check Disable Discord RP!");
                viewModel.DisableDiscordRP = false;
            }
        }

        private async void DisableDiscordRP_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                await userPreferencesLoader.UpdateAsync(pref => pref.DisableDiscordRP = false);

                if (viewModel.IsTracking)
                {
                    discordRichPresentLogic.Start(viewModel.Callsign);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot uncheck Disable Discord RP!");
                viewModel.DisableDiscordRP = true;
            }
        }

        private async void BroadcastUDP_Checked(object sender, RoutedEventArgs e)
        {
            // Prevent loop
            if (!BroadcastUDP.IsEnabled) return;

            var initializating = hub == null;

            try
            {
                BroadcastUDP.IsEnabled = false;
                await userPreferencesLoader.UpdateAsync(pref => pref.BroadcastUDP = true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot update BroadcastUDP!");
                viewModel.BroadcastUDP = false;
                return;
            }
            finally
            {
                BroadcastUDP.IsEnabled = true;
            }

            // Side-effect
            if (!initializating)
            {
                try
                {
                    await StartUDPConnectionAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Cannot start UDP connection!");
                    MessageBox.Show("Cannot start broadcasting! Please restart Flight Events to try again.");
                }
            }
        }

        private async void BroadcastUDP_Unchecked(object sender, RoutedEventArgs e)
        {
            // Prevent loop
            if (!BroadcastUDP.IsEnabled) return;

            try
            {
                BroadcastUDP.IsEnabled = false;
                await userPreferencesLoader.UpdateAsync(pref => pref.BroadcastUDP = false);

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot update BroadcastUDP!");
                viewModel.BroadcastUDP = true;
                return;
            }
            finally
            {
                BroadcastUDP.IsEnabled = true;
            }

            // Side-effect
            try
            {
                await StopUDPConnectionAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot stop UDP connection!");
                MessageBox.Show("Cannot stop broadcasting! Please restart Flight Events.");
            }
        }

        private async void BroadcastIP_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                await userPreferencesLoader.UpdateAsync(pref => pref.BroadcastIP = viewModel.BroadcastIP);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot update Broadcast IP!");
            }
        }

        private async void SlowMode_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                await userPreferencesLoader.UpdateAsync(pref => pref.SlowMode = true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot check SlowMode!");
                viewModel.SlowMode = false;
            }
        }

        private async void SlowMode_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                await userPreferencesLoader.UpdateAsync(pref => pref.SlowMode = false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot uncheck SlowMode!");
                viewModel.SlowMode = true;
            }
        }

        private async void MinimizeToTaskbar_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                await userPreferencesLoader.UpdateAsync(pref => pref.MinimizeToTaskbar = true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot check SlowMode!");
                viewModel.MinimizeToTaskbar = false;
            }
        }

        private async void MinimizeToTaskbar_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                await userPreferencesLoader.UpdateAsync(pref => pref.MinimizeToTaskbar = false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot uncheck SlowMode!");
                viewModel.MinimizeToTaskbar = true;
            }
        }
        private async void ShowLandingInfo_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                await userPreferencesLoader.UpdateAsync(pref => pref.ShowLandingInfo = true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot check Show Landing Info!");
                viewModel.ShowLandingInfo = false;
            }
        }

        private async void ShowLandingInfo_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                await userPreferencesLoader.UpdateAsync(pref => pref.ShowLandingInfo = false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot uncheck Show Landing Info!");
                viewModel.ShowLandingInfo = true;
            }
        }

        #endregion

        #region Events

        private void ButtonReadMore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var vm = (sender as FrameworkElement).DataContext as FlightEventViewModel;
                if (!string.IsNullOrEmpty(vm?.Model.Url))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = vm.Model.Url,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot open Read More link");
            }
        }

        private void ButtonChecklist_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var vm = (sender as FrameworkElement).DataContext as FlightEventViewModel;
                viewModel.ChecklistEvent = new ChecklistViewModel(vm.Model, viewModel.DiscordConnection != null);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot open checklist");
            }
        }

        private void ButtonCloseChecklist_Click(object sender, RoutedEventArgs e)
        {
            viewModel.ChecklistEvent = null;
        }

        private void ButtonChecklistItemHint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var itemVM = (sender as FrameworkElement).DataContext as ChecklistItemViewModel;
                if (!string.IsNullOrEmpty(itemVM.Hint))
                {
                    MessageBox.Show(itemVM.Hint, "Flight Events", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch { }
        }

        private void ButtonChecklistItemLink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var link = (sender as FrameworkElement).DataContext as FlightEventChecklistItemLink;
                if (!string.IsNullOrEmpty(link?.Url) && Uri.IsWellFormedUriString(link.Url, UriKind.Absolute))
                {
                    Process.Start(new ProcessStartInfo(link.Url) { UseShellExecute = true });
                }
            }
            catch { }
        }

        #endregion

        public void ShowSimConnectErrorMessage()
        {
            MessageBox.Show(this,
                @"SimConnect is not found. This component is needed to connect to flight simulator.

Please make sure you have installed Microsoft Flight Simulator and restart the client.",
                "Needed component is missing",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        public void ShowSavingPreferenceErrorMessageAndShutDown()
        {
            MessageBox.Show("Please extract Flight Events before running.\n\nIf you have already done so, please try to extract to another folder that does not require Administrator right.", "Cannot Save Preference", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown();
        }
    }
}
