using FlightEvents.Data;
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
                    services.AddOptions<AzureTableOptions>().Bind(hostContext.Configuration.GetSection("FlightPlan:AzureStorage")).ValidateDataAnnotations();

                    services.AddTransient<IDiscordConnectionStorage, AzureTableDiscordConnectionStorage>();
                    services.AddHostedService<MovingWorker>();
                    services.AddHostedService<CleaningWorker>();
                });
    }
}
