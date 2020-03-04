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
        private readonly ATCServer atcServer;
        private readonly UserPreferencesLoader userPreferencesLoader;
        private readonly HubConnection hub;
        private readonly ILogger<MainWindow> logger;
        private readonly IFlightConnector flightConnector;

        private AircraftData aircraftData;

        public MainWindow(ILogger<MainWindow> logger, IFlightConnector flightConnector, MainViewModel viewModel, IOptions<AppSettings> appSettings, ATCServer atcServer, UserPreferencesLoader userPreferencesLoader)
        {
            InitializeComponent();
            this.logger = logger;
            this.flightConnector = flightConnector;
            this.atcServer = atcServer;
            this.userPreferencesLoader = userPreferencesLoader;
            this.viewModel = viewModel;

            flightConnector.AircraftDataUpdated += FlightConnector_AircraftDataUpdated;
            flightConnector.AircraftStatusUpdated += FlightConnector_AircraftStatusUpdated;
            flightConnector.FlightPlanUpdated += FlightConnector_FlightPlanUpdated;

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

            TextURL.Text = appSettings.Value.WebServerUrl;

            atcServer.FlightPlanRequested += AtcServer_FlightPlanRequested;
            atcServer.Connected += AtcServer_Connected;
            atcServer.IdentSent += AtcServer_IdentSent;
        }

        #region Interaction

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(viewModel.Callsign))
            {
                var pref = await userPreferencesLoader.LoadAsync();
                viewModel.Callsign = string.IsNullOrWhiteSpace(pref.LastCallsign) ? GenerateCallSign() : pref.LastCallsign;
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
            TextCallsign.IsEnabled = false;

            await userPreferencesLoader.UpdateAsync(o => o.LastCallsign = viewModel.Callsign);

            ButtonStartTrack.Visibility = Visibility.Collapsed;
            ButtonStopTrack.Visibility = Visibility.Visible;
        }

        private void ButtonStopTrack_Click(object sender, RoutedEventArgs e)
        {
            TextCallsign.IsEnabled = true;
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

        private Task Hub_Reconnected(string arg)
        {
            viewModel.HubConnectionState = ConnectionState.Connected;
            return Task.CompletedTask;
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

        private void Hub_OnRequestFlightPlan(string atcConnectionId, string callsign)
        {
            if (viewModel.Callsign == callsign)
            {
                flightConnector.RequestFlightPlan(atcConnectionId);
            }
        }

        #endregion

        #region SimConnect

        private void FlightConnector_AircraftDataUpdated(object sender, AircraftDataUpdatedEventArgs e)
        {
            aircraftData = e.AircraftData;
        }

        DateTime lastStatusSent = DateTime.Now;

        private async void FlightConnector_AircraftStatusUpdated(object sender, AircraftStatusUpdatedEventArgs e)
        {
            // TODO: change this to proper viewmodel
            if (!TextCallsign.IsEnabled)
            {
                e.AircraftStatus.Callsign = viewModel.Callsign;

                if (hub?.ConnectionId != null && DateTime.Now - lastStatusSent > TimeSpan.FromMilliseconds(MinimumUpdatePeriod))
                {
                    lastStatusSent = DateTime.Now;
                    await hub.SendAsync("UpdateAircraft", hub.ConnectionId, e.AircraftStatus);
                    lastStatusSent = DateTime.Now;
                }

                viewModel.AircraftStatus = e.AircraftStatus;
            }
        }

        private async void FlightConnector_FlightPlanUpdated(object sender, FlightPlanUpdatedEventArgs e)
        {
            var flightPlan = new FlightPlanCompact(e.FlightPlan, viewModel.Callsign, aircraftData.Model, (int)aircraftData.EstimatedCruiseSpeed);
            await hub.SendAsync("ReturnFlightPlan", hub.ConnectionId, flightPlan, e.AtcConnectionIds);
        }

        #endregion

        #region ATC

        private async void AtcServer_Connected(object sender, ConnectedEventArgs e)
        {
            viewModel.AtcCallsign = e.Callsign;

            hub.On<string, AircraftStatus>("UpdateAircraft", async (connectionId, aircraftStatus) =>
            {
                await atcServer.SendPositionAsync(aircraftStatus.Callsign, aircraftStatus.Transponder,
                    aircraftStatus.Latitude, aircraftStatus.Longitude, aircraftStatus.Altitude, aircraftStatus.GroundSpeed, viewModel.TransponderIdent ? TransponderMode.Ident : TransponderMode.ModeC);
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
