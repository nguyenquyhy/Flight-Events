using FlightEvents.Data;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.IO;
using System.Reflection;

namespace FlightEvents.DiscordBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureAppConfiguration(configBuilder =>
                {
                    configBuilder.AddJsonFile("appsettings.Release.json", optional: true);
                })
                .ConfigureLogging(logging =>
                {
                    var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                    Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .WriteTo.Debug()
                        .WriteTo.Console()
                        .WriteTo.File(Path.Combine(assemblyDirectory, "flightevents-bot.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 3)
                        .CreateLogger();

                    logging
                        .ClearProviders()
                        .AddSerilog()
                        .AddEventSourceLogger();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions<AppOptions>().Bind(hostContext.Configuration).ValidateDataAnnotations();
                    services.AddOptions<DiscordOptions>().Bind(hostContext.Configuration.GetSection("Discord")).ValidateDataAnnotations();
                    services.AddOptions<AtisOptions>().Bind(hostContext.Configuration.GetSection("Atis")).ValidateDataAnnotations();
                    services.AddOptions<AzureTableDiscordOptions>().Bind(hostContext.Configuration.GetSection("FlightPlan:AzureStorage")).ValidateDataAnnotations();
                    services.AddOptions<AzureTableAtisChannelOptions>().Bind(hostContext.Configuration.GetSection("FlightPlan:AzureStorage")).ValidateDataAnnotations();

                    var appOptions = new AppOptions();
                    hostContext.Configuration.Bind(appOptions);

                    var hub = new HubConnectionBuilder()
                        .WithUrl(appOptions.WebServerUrl + "/FlightEventHub?clientType=Bot")
                        .WithAutomaticReconnect()
                        .Build();

                    services.AddSingleton(hub);

                    services.AddTransient<IDiscordConnectionStorage, AzureTableDiscordConnectionStorage>();
                    services.AddTransient<ChannelMaker>();
                    services.AddTransient<RegexMatcher>();

                    services.AddTransient<IAtisChannelStorage, AzureTableAtisChannelStorage>();
                    services.AddSingleton<AtisProcessManager>();

                    services.AddHostedService<MovingWorker>();
                    services.AddHostedService<CleaningWorker>();
                    services.AddHostedService<DiscordMessageWorker>();
                    services.AddHostedService<AtisWorker>();
                });
    }
}
