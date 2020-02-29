using Microsoft.AspNetCore.SignalR.Client;
using System;
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
        private double longitude;
        private double latitude;
        private double heading;
        private double airspeed;
        private double altitude;

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

            longitude = random.NextDouble() * 360 - 180;
            latitude = random.NextDouble() * 180 - 90;
            heading = random.NextDouble() * 360;
            airspeed = random.NextDouble() * 100 + 100;
            altitude = random.NextDouble() * 5500 + 1000;

            timer.Start();
        }

        private async void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            await hub.StopAsync();
        }
    }
}
