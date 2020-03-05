using FlightEvents.Client.ATC;
using FlightEvents.Client.Logics;
using FlightEvents.Client.SimConnectFSX;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace FlightEvents.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public ServiceProvider ServiceProvider { get; private set; }

        private MainWindow mainWindow = null;
        private IntPtr Handle;

        public IConfigurationRoot Configuration { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
#if !DEBUG
            AppCenter.Start("6a75536f-3bd1-446c-b707-c31aabe3fb6f", typeof(Analytics), typeof(Crashes));
#endif

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            Configuration = builder.Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();
            try
            {
                mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                mainWindow.Loaded += MainWindow_Loaded;
                mainWindow.Show();
            }
            catch (BadImageFormatException)
            {
                MessageBox.Show("SimConnect not found. This component is needed to connect to Flight Simulator.\n" +
                    "Please download SimConnect from\n\nhttp://www.fspassengers.com/?action=simconnect\n\n" +
                    "follow the ReadMe.txt in the zip file and try to start again.\n\nThis program will now exit.",
                    "Needed component is missing",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(-1);
            }
        }

        private void ConfigureServices(ServiceCollection services)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .MinimumLevel.Information()
                .WriteTo.File("flightevents.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 3)
                .CreateLogger();

            services.AddOptions();
            services.Configure<AppSettings>(Configuration);
            services.AddLogging(configure =>
            {
                configure.AddSerilog();
            });

            services.AddSingleton<MainViewModel>();
            services.AddSingleton<IFlightConnector, SimConnectFlightConnector>();
            services.AddSingleton<ATCServer>();
            services.AddSingleton(new UserPreferencesLoader("preferences.json"));

            services.AddTransient(typeof(MainWindow));
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize SimConnect
            var flightConnector = ServiceProvider.GetService<IFlightConnector>();
            if (flightConnector is SimConnectFlightConnector simConnect)
            {
                simConnect.Closed += SimConnect_Closed;

                // Create an event handle for the WPF window to listen for SimConnect events
                Handle = new WindowInteropHelper(sender as Window).Handle; // Get handle of main WPF Window
                var HandleSource = HwndSource.FromHwnd(Handle); // Get source of handle in order to add event handlers to it
                HandleSource.AddHook(simConnect.HandleSimConnectEvents);

                var viewModel = ServiceProvider.GetService<MainViewModel>();
                await InitializeSimConnectAsync(simConnect, viewModel).ConfigureAwait(true);
            }
        }

        private async Task InitializeSimConnectAsync(SimConnectFlightConnector simConnect, MainViewModel viewModel)
        {
            while (true)
            {
                try
                {
                    viewModel.SimConnectionState = ConnectionState.Connecting;
                    simConnect.Initialize(Handle);
                    viewModel.SimConnectionState = ConnectionState.Connected;
                    break;
                }
                catch (COMException)
                {
                    viewModel.SimConnectionState = ConnectionState.Failed;
                    await Task.Delay(5000).ConfigureAwait(true);
                }
            }
        }

        private async void SimConnect_Closed(object sender, EventArgs e)
        {
            var simConnect = sender as SimConnectFlightConnector;
            var viewModel = ServiceProvider.GetService<MainViewModel>();
            viewModel.SimConnectionState = ConnectionState.Idle;

            await InitializeSimConnectAsync(simConnect, viewModel).ConfigureAwait(true);
        }
    }
}
