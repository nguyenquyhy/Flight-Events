using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.IO;
using System.Reflection;

namespace FlightEvents.DiscordBot.AtisBot
{
    class Program
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
                        .WriteTo.File(Path.Combine(assemblyDirectory, "flightevents-bot-atis.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 3)
                        .CreateLogger();

                    logging
                        .ClearProviders()
                        .AddSerilog()
                        .AddEventSourceLogger();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions<AppOptions>().Bind(hostContext.Configuration).ValidateDataAnnotations();

                    var appOptions = new AppOptions();
                    hostContext.Configuration.Bind(appOptions);

                    services.AddHostedService<AtisService>();
                });
    }
}
