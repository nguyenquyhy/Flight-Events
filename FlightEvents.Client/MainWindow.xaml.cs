using FlightEvents.Client.Logics;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
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
    }
}
