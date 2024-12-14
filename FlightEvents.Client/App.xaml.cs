﻿using DiscordRPC;
using FlightEvents.Client.ATC;
using FlightEvents.Client.Logics;
using FlightEvents.Client.SimConnectFSX;
using FlightEvents.Client.ViewModels;
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
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessDPIAware();

        private const string DefaultWebServerUrl = "https://events.flighttracker.tech";

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

            // HACK: workaround for issue in WPF https://github.com/dotnet/wpf/issues/5375
            SetProcessDPIAware();

            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                Configuration = builder.Build();
            }
            catch (UnauthorizedAccessException)
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory());

                Configuration = builder.Build();
            }

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
                .WriteTo.Logger(config => config
                    .MinimumLevel.Information()
                    .WriteTo.File("flightevents.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 3, buffered: true)
                )
                .CreateLogger();

            services.AddOptions<AppSettings>().Bind(Configuration)
                .ValidateDataAnnotations()
                .PostConfigure(options =>
                {
                    options.WebServerUrl ??= DefaultWebServerUrl;
                });

            services.AddLogging(configure =>
            {
                configure.AddSerilog();
            });

            services.AddSingleton<IEventGraphQLClient, EventGraphQLClient>();
            services.AddSingleton<IEventFetcher, EventFetcher>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<IFlightConnector, SimConnectFlightConnector>();
            services.AddSingleton<UdpBroadcastLogic>();
            services.AddSingleton<ATCServer>();
            services.AddSingleton<UserPreferencesLoader>();
            services.AddSingleton(new VersionLogic("https://events-storage.flighttracker.tech/downloads/versions.json"));

            services.AddTransient(typeof(MainWindow));

            var discordRpcClient = new DiscordRpcClient("688293497748455489");
            discordRpcClient.OnReady += (sender, e) =>
            {
                Debug.WriteLine(string.Format("Received Ready from user {0}", e.User.Username));
            };
            discordRpcClient.OnPresenceUpdate += (sender, e) =>
            {
                Debug.WriteLine(string.Format("Received Update! {0}", e.Presence));
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
                    mainWindow.ShowSimConnectErrorMessage();
                }
            }
        }

        private async Task InitializeSimConnectAsync(SimConnectFlightConnector simConnect, MainViewModel viewModel)
        {
            while (true)
            {
                try
                {
                    var userPrefLoader = ServiceProvider.GetService<UserPreferencesLoader>();
                    var slowMode = await userPrefLoader.GetSettingsAsync(o => o.SlowMode);

                    viewModel.SimConnectionState = ConnectionState.Connecting;
                    simConnect.Initialize(Handle, slowMode);
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
