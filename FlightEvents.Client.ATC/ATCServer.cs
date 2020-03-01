using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace FlightEvents.Client.ATC
{
    public class ConnectedEventArgs : EventArgs
    {
        public ConnectedEventArgs(string callsign)
        {
            Callsign = callsign;
        }

        public string Callsign { get; }
    }

    public class ATCServer
    {
        private const string ClientCode = "FLTEV";
        private const string ClientName = "Flight Events";

        private readonly ILogger<ATCServer> logger;

        private StreamWriter writer;
        private bool vrc = false;
        private bool atc = false;
        private string callsign;
        private TcpListener tcpListener;
        private TcpClient tcpClient;

        public event EventHandler<ConnectedEventArgs> Connected;

        public ATCServer(ILogger<ATCServer> logger)
        {
            this.logger = logger;
        }

        public void Start()
        {
            tcpListener = new TcpListener(IPAddress.Any, 6809);
            tcpListener.Start();

            Task.Run(() =>
            {
                return AcceptAndProcessAsync(tcpListener);
            });
        }

        public void Stop()
        {
            tcpClient?.Close();
            tcpClient = null;
            tcpListener?.Stop();
            tcpListener = null;
        }

        public async Task SendPositionAsync(string callsign, string squawk, double latitude, double longitude, double altitude, double groundSpeed)
        {
            var pos = $"@N:{callsign}:{squawk}:1:{latitude}:{longitude}:{altitude}:{groundSpeed}:62905944:5";
            await writer?.WriteLineAsync(pos);
            await writer?.FlushAsync();

            logger.LogDebug("Sent Position: " + pos);
        }

        public async Task SendFlightPlan(string callsign, bool isIFR, string type, string registration, string title, double frequency, string departure, string arrival, double groundSpeed, double altitude)
        { 
            var ifrs = isIFR ? "I" : "V";
            var next = departure + " NATEX " + arrival;

            var fp = $"$FP{callsign}:*A:{ifrs}:{type}:{groundSpeed}:{departure}:::{altitude}:{arrival}:::00:00:NONE:Aircraft = {title}  Tuned to {frequency} Registration = {registration}:{next}:";
            //var uspd = 125;
            //var fp = $"$FP{callsign}:*A:{ifrs}:{utype}:{(object)uspd}:ZZZZ:00:00:ZZZZ:ZZZZ:00:00:00:00:NONE:Flight Plan is not available for the user aircraft. You know where you are going!                    Aircraft = {utitle} Tuned to {ufreq} Registration = {ureg}::";

            await writer?.WriteLineAsync(fp);
            await writer?.FlushAsync();
            logger.LogInformation("Sent Flight Plan: " + fp);
        }

        private async Task AcceptAndProcessAsync(TcpListener tcpListener)
        {
            while (true)
            {
                logger.LogInformation("Waiting for connection...");
                tcpClient = tcpListener.AcceptTcpClient();
                logger.LogInformation("Accepted a connection");
                using var stream = tcpClient.GetStream();
                var reader = new StreamReader(stream);
                writer = new StreamWriter(stream);

                while (true)
                {
                    var info = await reader.ReadLineAsync();
                    logger.LogInformation($"Receive: {info}");

                    if (info.Contains("VRC") && !atc)
                    {
                        vrc = true;
                        atc = true;
                        await writer.WriteLineAsync($"$DI{ClientCode}:CLIENT:client V1.00:3ef36a24");
                        await writer.FlushAsync();
                        logger.LogInformation("Sent VRC Hello");
                    }
                    //else if (!this.atc && !this.es)
                    //{
                    //    this.es = true;
                    //    this.atc = true;
                    //    Console.WriteLine("Sent EuroScope Hello");
                    //}

                    if (info.StartsWith("#DA"))
                    {
                        logger.LogInformation("Client is disconnected");
                        writer = null;
                        break;
                    }

                    //if (info.StartsWith("$AX"))
                    //{
                    //    string station = info.Substring(25, 4);
                    //    try
                    //    {
                    //        var oldLines = System.IO.File.ReadAllLines(met);
                    //        var newLines = ((IEnumerable<string>)oldLines).Where<string>((Func<string, bool>)(line => !line.Contains(station)));
                    //        System.IO.File.WriteAllLines(met, newLines);
                    //        WebRequest webRequest = WebRequest.Create("http://tgftp.nws.noaa.gov/data/observations/metar/stations/" + station + ".TXT");
                    //        using (WebResponse response = webRequest.GetResponse())
                    //        {
                    //            using (Stream MET = response.GetResponseStream())
                    //            {
                    //                using (StreamReader reader = new StreamReader(MET))
                    //                {
                    //                    reader.ReadLine();
                    //                    string metar = reader.ReadLine();
                    //                    string sndmet = "$ARJoinFS:" + callsign + ":METAR:" + metar;
                    //                    System.IO.File.AppendAllText(met, sndmet + Environment.NewLine);
                    //                    Console.WriteLine("Retreived the latest METAR for " + station);
                    //                    metar = (string)null;
                    //                    sndmet = (string)null;
                    //                }
                    //            }
                    //        }
                    //        oldLines = (string[])null;
                    //        newLines = (IEnumerable<string>)null;
                    //        webRequest = (WebRequest)null;
                    //    }
                    //    catch
                    //    {
                    //        Console.WriteLine("Could not get latest METAR for " + station);
                    //    }
                    //    await Form1.Global.sw.WriteLineAsync(System.IO.File.ReadAllText(met));
                    //}

                    if (this.vrc)
                    {
                        if (info.Contains($"$CQ{callsign}:SERVER:ATC:{callsign}") && !this.atc)
                        {
                            logger.LogInformation("Send VRC");
                            await writer.WriteLineAsync($"$CR{ClientCode}:{callsign}:ATC:Y:{callsign}");
                            await writer.FlushAsync();
                            this.atc = true;
                        }
                    }
                    else
                    {
                        if (info.StartsWith("#AA"))
                        {
                            logger.LogInformation("Connected");

                            callsign = info.Substring(3, 4);
                            await writer.WriteLineAsync($"#TM{ClientCode}:{callsign}:Connected to {ClientName}.");
                            await writer.FlushAsync();

                            Connected?.Invoke(this, new ConnectedEventArgs(callsign));
                        }
                    }
                }
            }
        }
    }
}
