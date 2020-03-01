using FlightEvents.Client.ATC;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace FlightEvents.MockClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly HubConnection hub;
        private readonly Timer timer;
        private readonly Random random = new Random();
        private double latitude = 47.36360168;
        private double longitude = 17.50079918;
        private double heading;
        private double airspeed;
        private double altitude;
        private ATCServer atcServer;
        private const double sec = 2;

        public MainWindow()
        {
            InitializeComponent();

            hub = new HubConnectionBuilder()
                .WithUrl("https://localhost:44359/FlightEventHub")
                .WithAutomaticReconnect()
                .Build();

            timer = new Timer(sec * 1000);
            timer.Elapsed += Timer_ElapsedAsync;
        }

        private void Timer_ElapsedAsync(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(async () =>
            {
                if (hub.State == HubConnectionState.Connected)
                {
                    await hub.SendAsync("UpdateAircraft", hub.ConnectionId, new AircraftStatus
                    {
                        Callsign = TextCallsign.Text,
                        Longitude = longitude,
                        Latitude = latitude,
                        Heading = heading,
                        TrueHeading = heading,
                        Altitude = altitude,
                        AltitudeAboveGround = altitude,
                        IndicatedAirSpeed = airspeed
                    });

                    var distance = airspeed / 3600.0 * sec;

                    longitude += Math.Sin(heading / 360.0 * Math.PI * 2) * distance / (Math.Cos(latitude / 360.0 * Math.PI * 2) * 60.108);
                    latitude += Math.Cos(heading / 360.0 * Math.PI * 2) * distance / 60.108;
                }
            });
        }

        private async void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            await hub.StartAsync();

            TextConnectionId.Text = hub.ConnectionId;
            TextCallsign.Text = hub.ConnectionId.Substring(0, 6).ToUpper();

            heading = random.NextDouble() * 360;
            airspeed = random.NextDouble() * 100 + 100;
            altitude = random.NextDouble() * 5500 + 10000;

            timer.Start();
        }

        private async void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            await hub.StopAsync();
        }

        private void ButtonStartVATSIM_Click(object sender, RoutedEventArgs e)
        {
            var loggerFactory = LoggerFactory.Create(config => config.AddDebug().SetMinimumLevel(LogLevel.Information));

            atcServer = new ATCServer(loggerFactory.CreateLogger<ATCServer>());
            atcServer.Connected += AtcServer_Connected;
            atcServer.Start();
        }

        private void AtcServer_Connected(object sender, ConnectedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                TextCallsign.Text = e.Callsign;
                ButtonStartVATSIM.IsEnabled = false;
            });

            Task.Run(async () =>
            {
                while (true)
                {
                    var callsign = "HY3088";
                    var squawk = "1233";
                    longitude += 0.0001;
                    latitude += 0.0001;
                    var altitude = 12345;
                    var groundSpeed = 120;

                    await atcServer.SendPositionAsync(callsign, squawk, latitude, longitude, altitude, groundSpeed, TransponderMode.ModeC);

                    await Task.Delay(1000);
                }
            });
        }

        private void ButtonStopVATSIM_Click(object sender, RoutedEventArgs e)
        {
            atcServer?.Stop();
            atcServer = null;
        }

        private async void ButtonSendFP_Click(object sender, RoutedEventArgs e)
        {
            var callsign = "HY3088";

            var type = "Test";
            var title = "Test aircraft";
            var dep = "LHPR";
            var arr = "LHSY";
            var reg = "AAAAA";
            var route = "NATEX";

            await atcServer.SendFlightPlanAsync(callsign, true, type, reg, title, dep, arr, route, 200, 15000, TimeSpan.FromHours(1.5));
        }
    }
}
