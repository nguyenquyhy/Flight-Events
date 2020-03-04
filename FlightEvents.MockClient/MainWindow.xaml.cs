using FlightEvents.Client.ATC;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace FlightEvents.MockClient
{
    public class MockAircraft
    {
        public MockAircraft()
        {
            Hub = new HubConnectionBuilder()
                //.WithUrl("https://localhost:44359/FlightEventHub")
                .WithUrl("https://events.flighttracker.tech/FlightEventHub")
                .WithAutomaticReconnect()
                .Build();
        }

        public HubConnection Hub { get; }
        public string ConnectionId { get; set; }
        public string Callsign { get; set; }
        public double Latitude { get; set; } = 47.36360168;
        public double Longitude { get; set; } = 17.50079918;
        public double Heading { get; set; }
        public double Airspeed { get; set; }
        public double Altitude { get; set; }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Timer timer;
        private readonly Random random = new Random();
        private ATCServer atcServer;
        private const double sec = 2;

        private readonly ObservableCollection<MockAircraft> aircrafts = new ObservableCollection<MockAircraft>();

        public MainWindow()
        {
            InitializeComponent();

            timer = new Timer(sec * 1000);
            timer.Elapsed += Timer_ElapsedAsync;

            ListClient.ItemsSource = aircrafts;
        }

        private void Timer_ElapsedAsync(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(async () =>
            {
                foreach (var aircraft in aircrafts.ToList())
                {
                    if (aircraft.Hub.State == HubConnectionState.Connected)
                    {
                        await aircraft.Hub.SendAsync("UpdateAircraft", aircraft.Hub.ConnectionId, new AircraftStatus
                        {
                            Callsign = aircraft.Callsign,
                            Longitude = aircraft.Longitude,
                            Latitude = aircraft.Latitude,
                            Heading = aircraft.Heading,
                            TrueHeading = aircraft.Heading,
                            Altitude = aircraft.Altitude,
                            AltitudeAboveGround = aircraft.Altitude,
                            IndicatedAirSpeed = aircraft.Airspeed
                        });

                        var distance = aircraft.Airspeed / 3600.0 * sec;
                        var rad = aircraft.Heading / 360.0 * Math.PI * 2;
                        aircraft.Longitude += Math.Sin(rad) * distance / (Math.Cos(aircraft.Latitude / 360.0 * Math.PI * 2) * 60.108);
                        aircraft.Latitude += Math.Cos(rad) * distance / 60.108;
                    }
                }
            });
        }

        private async void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            var number = int.Parse(TextNumber.Text);

            for (var i = 0; i < number; i++)
            {
                var aircraft = new MockAircraft();

                await aircraft.Hub.StartAsync();

                aircraft.ConnectionId = aircraft.Hub.ConnectionId;
                aircraft.Callsign = aircraft.Hub.ConnectionId.Substring(0, 6).ToUpper();

                aircraft.Heading = random.NextDouble() * 360;
                aircraft.Airspeed = random.NextDouble() * 100 + 100;
                aircraft.Altitude = random.NextDouble() * 5500 + 10000;

                aircrafts.Add(aircraft);
            }
        }

        private async void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            var aircraft = (sender as FrameworkElement).DataContext as MockAircraft;
            await aircraft.Hub.StopAsync();
            aircrafts.Remove(aircraft);
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
                //TextCallsign.Text = e.Callsign;
                ButtonStartVATSIM.IsEnabled = false;
            });

            Task.Run(async () =>
            {
                var latitude = 47.36360168;
                var longitude = 17.50079918;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            timer.Start();
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            timer.Stop();
        }
    }
}
