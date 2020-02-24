using FlightEvents.Client.Logics;
using System.Windows;
using System.Windows.Input;

namespace FlightEvents.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(IFlightConnector flightConnector)
        {
            InitializeComponent();
            flightConnector.AircraftStatusUpdated += FlightConnector_AircraftStatusUpdated;
        }

        private async void FlightConnector_AircraftStatusUpdated(object sender, AircraftStatusUpdatedEventArgs e)
        {
            //viewModel.FlightStatus = e.FlightStatus;

            //if (isReady)
            //{
            //    try
            //    {
            //        var gpsData = Encoding.UTF8.GetBytes($"XGPSFS2020,{e.FlightStatus.Longitude},{e.FlightStatus.Latitude},{e.FlightStatus.Altitude},{e.FlightStatus.Heading},{e.FlightStatus.IndicatedAirSpeed}");
            //        var statusData = Encoding.UTF8.GetBytes($"XATTFS2020,{e.FlightStatus.TrueHeading},{e.FlightStatus.Pitch},{e.FlightStatus.Bank}");
            //        await client?.SendAsync(gpsData, gpsData.Length);
            //        await client?.SendAsync(statusData, statusData.Length);
            //    }
            //    catch (Exception ex)
            //    {
            //        logger.LogError(ex, "Cannot send flight status!");
            //    }
            //}
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
