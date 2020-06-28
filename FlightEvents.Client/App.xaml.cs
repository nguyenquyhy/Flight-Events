using DiscordRPC;
using FlightEvents.Client.ATC;
using FlightEvents.Client.Logics;
using FlightEvents.Client.SimConnectFSX;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        #region Single Instance Enforcer

        readonly SingletonApplicationEnforcer enforcer = new SingletonApplicationEnforcer(args =>
        {
            Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = Current.MainWindow as MainWindow;
                if (mainWindow != null && args != null)
                {
                    mainWindow.RestoreWindow();
                }
            });
        }, "FlightEvents.Client");

        #endregion

        public ServiceProvider ServiceProvider { get; private set; }

        private MainWindow mainWindow = null;
        private IntPtr Handle;

        public IConfigurationRoot Configuration { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            if (!e.Args.Contains("--multiple-instances") && enforcer.ShouldApplicationExit())
            {
                try
                {
                    Shutdown();
                }
                catch { }
            }

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

            mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Loaded += MainWindow_Loaded;
            mainWindow.Show();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .MinimumLevel.Information()
                .WriteTo.File("flightevents.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 3, buffered: true)
                .CreateLogger();

            services.AddOptions<AppSettings>().Bind(Configuration).ValidateDataAnnotations();

            services.AddLogging(configure =>
            {
                configure.AddSerilog();
            });

            services.AddSingleton<MainViewModel>();
            services.AddSingleton<IFlightConnector, SimConnectFlightConnector>();
            services.AddSingleton<ATCServer>();
            services.AddSingleton(new UserPreferencesLoader("preferences.json"));
            services.AddSingleton(new VersionLogic("https://events-storage.flighttracker.tech/downloads/versions.json"));

            services.AddTransient(typeof(MainWindow));

            var discordRpcClient = new DiscordRpcClient("688293497748455489");
            discordRpcClient.OnReady += (sender, e) =>
            {
                Debug.WriteLine("Received Ready from user {0}", e.User.Username);
            };
            discordRpcClient.OnPresenceUpdate += (sender, e) =>
            {
                Debug.WriteLine("Received Update! {0}", e.Presence);
            };
            services.AddSingleton(discordRpcClient);
            services.AddSingleton<DiscordRichPresentLogic>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (Log.Logger != null)
            {
                Log.CloseAndFlush();
            }
            base.OnExit(e);
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

                try
                {
                    await InitializeSimConnectAsync(simConnect, viewModel).ConfigureAwait(true);
                }
                catch (BadImageFormatException ex)
                {
                    ServiceProvider.GetService<ILogger<MainWindow>>().LogError(ex, "Cannot initialize SimConnect!");

                    var result = MessageBox.Show(mainWindow, "SimConnect not found. This component is needed to connect to Flight Simulator.\n" +
                        "Please download SimConnect from\n\nhttps://events-storage.flighttracker.tech/downloads/SimConnect.zip\n\n" +
                        "follow the ReadMe.txt in the zip file and try to start again.\n\nThis program will now exit.\n\nDo you want to open the SimConnect link above?",
                        "Needed component is missing",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Error);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "https://events-storage.flighttracker.tech/downloads/SimConnect.zip",
                                UseShellExecute = true
                            });
                        }
                        catch { }
                    }

                    Shutdown(-1);
                }
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
