using FlightEvents.Client.Logics;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
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
        private readonly MainViewModel viewModel;

        private readonly HubConnection hub;

        public MainWindow(IFlightConnector flightConnector, MainViewModel viewModel, IOptions<AppSettings> appSettings)
        {
            InitializeComponent();
            flightConnector.AircraftStatusUpdated += FlightConnector_AircraftStatusUpdated;

            DataContext = viewModel;
            this.viewModel = viewModel;

            hub = new HubConnectionBuilder()
                .WithUrl(appSettings.Value.WebServerUrl + "/FlightEventHub")
                .WithAutomaticReconnect()
                .Build();

            hub.Closed += Hub_Closed;
            hub.Reconnecting += Hub_Reconnecting;
            hub.Reconnected += Hub_Reconnected;

            TextURL.Text = appSettings.Value.WebServerUrl;
        }

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

        DateTime last = DateTime.Now;

        private async void FlightConnector_AircraftStatusUpdated(object sender, AircraftStatusUpdatedEventArgs e)
        {
            e.AircraftStatus.Callsign = viewModel.Callsign;

            if (hub?.ConnectionId != null && DateTime.Now - last > TimeSpan.FromSeconds(2))
            {
                last = DateTime.Now;
                await hub.SendAsync("UpdateAircraft", hub.ConnectionId, e.AircraftStatus);
                last = DateTime.Now;
            }

            viewModel.AircraftStatus = null;
            viewModel.AircraftStatus = e.AircraftStatus;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel.HubConnectionState = ConnectionState.Connecting;
            await hub.StartAsync();
            viewModel.HubConnectionState = ConnectionState.Connected;

            if (string.IsNullOrWhiteSpace(viewModel.Callsign)) viewModel.Callsign = GenerateCallSign();
        }

        private readonly Random random = new Random();

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

        private void TextURL_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = TextURL.Text,
                    UseShellExecute = true
                });
            }
            catch {
            
            }
        }

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
    }
}
