using MessagePack;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace FlightEvents.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MessagePackSecurity.Active = MessagePackSecurity.UntrustedData;

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
