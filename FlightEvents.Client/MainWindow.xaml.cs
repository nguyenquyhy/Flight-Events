using FlightEvents.Client.ATC;
using FlightEvents.Client.Logics;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
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
        private const int MinimumUpdatePeriod = 500;
        private readonly Random random = new Random();

        private readonly MainViewModel viewModel;
        private readonly IOptions<AppSettings> appSettings;
        private readonly ATCServer atcServer;
        private readonly UserPreferencesLoader userPreferencesLoader;
        private readonly VersionLogic versionLogic;
        private readonly HubConnection hub;
        private readonly ILogger<MainWindow> logger;
        private readonly IFlightConnector flightConnector;

        private AircraftData aircraftData;

        public MainWindow(ILogger<MainWindow> logger, IFlightConnector flightConnector, MainViewModel viewModel, IOptions<AppSettings> appSettings,
            ATCServer atcServer, UserPreferencesLoader userPreferencesLoader, VersionLogic versionLogic)
        {
            InitializeComponent();
            this.logger = logger;
            this.flightConnector = flightConnector;
            this.atcServer = atcServer;
            this.userPreferencesLoader = userPreferencesLoader;
            this.versionLogic = versionLogic;
            this.viewModel = viewModel;
            this.appSettings = appSettings;
            flightConnector.AircraftDataUpdated += FlightConnector_AircraftDataUpdated;
            flightConnector.AircraftStatusUpdated += FlightConnector_AircraftStatusUpdated;

            DataContext = viewModel;

            hub = new HubConnectionBuilder()
                .WithUrl(appSettings.Value.WebServerUrl + "/FlightEventHub")
                .WithAutomaticReconnect()
                .AddMessagePackProtocol()
                .Build();

            hub.Closed += Hub_Closed;
            hub.Reconnecting += Hub_Reconnecting;
            hub.Reconnected += Hub_Reconnected;

            hub.On<string, string>("RequestFlightPlan", Hub_OnRequestFlightPlan);
            hub.On<string>("RequestFlightPlanDetails", Hub_OnRequestFlightPlanDetails);
            hub.On<string, string, string>("SendMessage", Hub_OnMessageSent);

            TextURL.Text = appSettings.Value.WebServerUrl;

            atcServer.FlightPlanRequested += AtcServer_FlightPlanRequested;
            atcServer.Connected += AtcServer_Connected;
            atcServer.MessageSent += AtcServer_MessageSent;
            atcServer.AtcLoggedIn += AtcServer_AtcLoggedIn;
        }

        #region Interaction

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(viewModel.Callsign))
            {
                var pref = await userPreferencesLoader.LoadAsync();
                viewModel.Callsign = string.IsNullOrWhiteSpace(pref.LastCallsign) ? GenerateCallSign() : pref.LastCallsign;
            }

            try
            {
                this.Title = "Flight Events " + versionLogic.GetVersion();
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
            }
        }

        private async void ButtonStartTrack_Click(object sender, RoutedEventArgs e)
        {
            viewModel.IsTracking = true;

            await userPreferencesLoader.UpdateAsync(o => o.LastCallsign = viewModel.Callsign);

            ButtonStartTrack.Visibility = Visibility.Collapsed;
            ButtonStopTrack.Visibility = Visibility.Visible;

            flightConnector.Send("Connected to Flight Events!");
        }

        private void ButtonStopTrack_Click(object sender, RoutedEventArgs e)
        {
            viewModel.IsTracking = false;
            ButtonStopTrack.Visibility = Visibility.Collapsed;
            ButtonStartTrack.Visibility = Visibility.Visible;
        }

        private void ButtonStartATC_Click(object sender, RoutedEventArgs e)
        {
            ButtonStartATC.IsEnabled = false;
            atcServer.Start();
        }

        private async void ButtonStopATC_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AtcCallsign = null;

            hub.Remove("UpdateAircraft");
            hub.Remove("UpdateFlightPlan");
            hub.Remove("ReturnFlightPlan");

            atcServer.Stop();

            await hub.SendAsync("Leave", "ATC");

            ButtonStopATC.Visibility = Visibility.Collapsed;
            ButtonStartATC.Visibility = Visibility.Visible;
            ButtonStartATC.IsEnabled = true;
        }

        private void TextURL_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = TextURL.Text, UseShellExecute = true });
            }
            catch { }
        }

        #endregion

        #region SignalR

        private async Task Hub_Reconnected(string arg)
        {
            viewModel.HubConnectionState = ConnectionState.Connected;

            if (!string.IsNullOrEmpty(viewModel.AtcCallsign))
            {
                await hub.SendAsync("Join", "ATC");
            }
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

        /// <summary>
        /// Flight plan is requested from a broadcast by ATC client
        /// </summary>
        private async void Hub_OnRequestFlightPlan(string atcConnectionId, string callsign)
        {
            if (viewModel.Callsign == callsign)
            {
                var data = await flightConnector.RequestFlightPlanAsync();
                if (data != null)
                {
                    var flightPlan = new FlightPlanCompact(data, viewModel.Callsign, aircraftData.Model, (int)aircraftData.EstimatedCruiseSpeed);
                    await hub.SendAsync("ReturnFlightPlan", hub.ConnectionId, flightPlan, new string[] { atcConnectionId });
                }
            }
        }

        /// <summary>
        /// Flight plan is requested from web client
        /// </summary>
        private async void Hub_OnRequestFlightPlanDetails(string webConnectionId)
        {
            var data = await flightConnector.RequestFlightPlanAsync();
            await hub.SendAsync("ReturnFlightPlanDetails", hub.ConnectionId, data, webConnectionId);
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
                        if (frequency == viewModel.AircraftStatus.FreqencyCom1.ToString() || frequency == viewModel.AircraftStatus.FreqencyCom2.ToString())
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

        #endregion

        #region SimConnect

        private void FlightConnector_AircraftDataUpdated(object sender, AircraftDataUpdatedEventArgs e)
        {
            aircraftData = e.AircraftData;
        }

        DateTime lastStatusSent = DateTime.Now;
        int? lastFreqencyCom1 = null;

        private async void FlightConnector_AircraftStatusUpdated(object sender, AircraftStatusUpdatedEventArgs e)
        {
            if (viewModel.IsTracking)
            {
                e.AircraftStatus.Callsign = viewModel.Callsign;
                e.AircraftStatus.TransponderMode = viewModel.TransponderIdent ? TransponderMode.Ident : TransponderMode.ModeC;

                if (hub?.ConnectionId != null && DateTime.Now - lastStatusSent > TimeSpan.FromMilliseconds(MinimumUpdatePeriod))
                {
                    lastStatusSent = DateTime.Now;
                    await hub.SendAsync("UpdateAircraft", hub.ConnectionId, e.AircraftStatus);
                    lastStatusSent = DateTime.Now;

                    if (viewModel.TransponderIdent) viewModel.TransponderIdent = false;

                    if (lastFreqencyCom1 != e.AircraftStatus.FreqencyCom1 && viewModel.AtcCallsign == null)
                    {
                        var clientId = (await userPreferencesLoader.LoadAsync()).ClientId;
                        if (!string.IsNullOrEmpty(clientId))
                        {
                            await hub.SendAsync("ChangeFrequency", clientId, e.AircraftStatus.FreqencyCom1);
                            lastFreqencyCom1 = e.AircraftStatus.FreqencyCom1;
                        }
                    }
                }

                viewModel.AircraftStatus = e.AircraftStatus;
            }
        }

        #endregion

        #region Discord

        private void ButtonDiscord_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"https://discordapp.com/api/oauth2/authorize?client_id={appSettings.Value.BotClientId}&redirect_uri={Uri.EscapeDataString($"{appSettings.Value.WebServerUrl}/discord/auth")}&response_type=code&scope={Uri.EscapeDataString("identify guilds.join")}",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private async void ButtonDiscordConfirm_Click(object sender, RoutedEventArgs e)
        {
            var userPref = await userPreferencesLoader.LoadAsync();
            if (string.IsNullOrEmpty(userPref.ClientId))
            {
                userPref = await userPreferencesLoader.UpdateAsync(userPref => userPref.ClientId = Guid.NewGuid().ToString("N"));
            }

            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync($"{appSettings.Value.WebServerUrl}/discord/confirm?clientId={userPref.ClientId}&code={TextDiscordConfirm.Text}", null);

            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show(this, "You have connected this client to your Discord account.", "Flight Events", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(this, "Cannot connect this client to your Discord account.\nPlease try again.", "Flight Events", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region ATC

        private async void AtcServer_Connected(object sender, ConnectedEventArgs e)
        {
            viewModel.AtcCallsign = e.Callsign;

            // ATC specific events
            hub.On<string, AircraftStatus>("UpdateAircraft", async (connectionId, aircraftStatus) =>
            {
                await atcServer.SendPositionAsync(aircraftStatus.Callsign, aircraftStatus.Transponder,
                    aircraftStatus.Latitude, aircraftStatus.Longitude, aircraftStatus.Altitude, aircraftStatus.GroundSpeed,
                    aircraftStatus.TransponderMode switch
                    {
                        TransponderMode.Standby => AtcTransponderMode.Standby,
                        TransponderMode.ModeC => AtcTransponderMode.ModeC,
                        TransponderMode.Ident => AtcTransponderMode.Ident,
                        _ => AtcTransponderMode.Standby
                    });
            });
            hub.On<string, FlightPlanCompact>("UpdateFlightPlan", async (connectionId, flightPlan) =>
            {
                await atcServer.SendFlightPlanAsync(
                    flightPlan.Callsign,
                    flightPlan.Type == "IFR",
                    flightPlan.AircraftType,
                    flightPlan.Callsign,
                    flightPlan.AircraftType,
                    flightPlan.Departure,
                    flightPlan.Destination,
                    flightPlan.Route,
                    flightPlan.CruisingSpeed,
                    flightPlan.CruisingAltitude,
                    flightPlan.EstimatedEnroute);
            });
            hub.On<string, FlightPlanCompact>("ReturnFlightPlan", async (connectionId, flightPlan) =>
            {
                await atcServer.SendFlightPlanAsync(
                    flightPlan.Callsign,
                    flightPlan.Type == "IFR",
                    flightPlan.AircraftType,
                    flightPlan.Callsign,
                    flightPlan.AircraftType,
                    flightPlan.Departure,
                    flightPlan.Destination,
                    flightPlan.Route,
                    flightPlan.CruisingSpeed,
                    flightPlan.CruisingAltitude,
                    flightPlan.EstimatedEnroute);
            });
            await hub.SendAsync("Join", "ATC");

            Dispatcher.Invoke(() =>
            {
                ButtonStartATC.Visibility = Visibility.Collapsed;
                ButtonStopATC.Visibility = Visibility.Visible;
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

        private async void AtcServer_AtcLoggedIn(object sender, AtcLoggedInEventArgs e)
        {
            var clientId = (await userPreferencesLoader.LoadAsync()).ClientId;

            if (!string.IsNullOrEmpty(clientId))
            {
                await hub.SendAsync("ChangeFrequency", clientId, e.Frequency);
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

        #endregion

        #region Minimize to System Tray

        private bool notified = false;

        private async void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Visibility = Visibility.Collapsed;
                myNotifyIcon.Visibility = Visibility.Visible;
                WindowState = WindowState.Normal;
                if (!notified)
                {
                    notified = true;
                    myNotifyIcon.ShowBalloonTip("Minimized to system tray", "Double click to restore the window.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                    await Task.Delay(3000);
                    myNotifyIcon.HideBalloonTip();
                }
            }
        }

        private void myNotifyIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            myNotifyIcon.Visibility = Visibility.Collapsed;
            Visibility = Visibility.Visible;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        #endregion
    }
}
