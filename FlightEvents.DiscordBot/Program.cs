using FlightEvents.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

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
                .ConfigureAppConfiguration(configBuilder => 
                {
                    configBuilder.AddJsonFile("appsettings.Release.json", optional: true);
                })
                .ConfigureLogging(logging =>
                {
                    Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .WriteTo.Debug()
                        .WriteTo.Console()
                        .WriteTo.File("flightevents-bot.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 3)
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
                    services.AddHostedService<Worker>();
                });
    }
}
