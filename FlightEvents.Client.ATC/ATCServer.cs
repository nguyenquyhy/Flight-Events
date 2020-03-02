using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
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

    public class FlightPlanRequestedEventArgs : EventArgs
    {
        public FlightPlanRequestedEventArgs(string callsign)
        {
            Callsign = callsign;
        }

        public string Callsign { get; }
    }

    public enum TransponderMode
    {
        Standby,
        ModeC,
        Ident
    }

    public class ATCServer
    {
        private const string ClientCode = "FLTEV";
        private const string ClientName = "Flight Events";

        private readonly ILogger<ATCServer> logger;
        private readonly HttpClient httpClient;

        private StreamWriter writer;
        private bool vrc = false;
        private bool atc = false;
        private string callsign;
        private TcpListener tcpListener;
        private TcpClient tcpClient;

        public event EventHandler<ConnectedEventArgs> Connected;
        public event EventHandler<FlightPlanRequestedEventArgs> FlightPlanRequested;

        public ATCServer(ILogger<ATCServer> logger)
        {
            this.logger = logger;
            this.httpClient = new HttpClient();
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

        public async Task SendPositionAsync(string callsign, string squawk, double latitude, double longitude, double altitude, double groundSpeed, TransponderMode transponderMode)
        {
            var modeString = transponderMode switch
            {
                TransponderMode.Standby => "S",
                TransponderMode.ModeC => "N",
                TransponderMode.Ident => "Y",
                _ => "N"
            };
            var rating = 1;

            var pos = $"@{modeString}:{callsign}:{squawk}:{rating}:{latitude}:{longitude}:{altitude}:{groundSpeed}:62905944:5";
            await writer?.WriteLineAsync(pos);

            logger.LogDebug("Sent Position: " + pos);
        }

        public async Task SendFlightPlanAsync(string callsign, bool isIFR, string type, string registration, string title,
            string departure, string arrival, string route, int? speed, int altitude, TimeSpan? enroute)
        {
            var ifrs = isIFR ? "I" : "V";
            var alternate = "NONE";
            var remarks = $"Aircraft = {title} Registration = {registration}";

            var fp = $"$FP{callsign}:*A:{ifrs}:{type}:{speed}:{departure}:::{altitude}:{arrival}:::{(enroute == null ? ":" : $"{enroute.Value.Hours.ToString("00")}:{enroute.Value.Minutes.ToString("00")}")}:{alternate}:{remarks}:{route}:";

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
                writer = new StreamWriter(stream)
                {
                    AutoFlush = true
                };

                while (true)
                {
                    var info = await reader.ReadLineAsync();
                    logger.LogInformation($"Receive: {info}");

                    if (info.Contains("VRC") && !atc)
                    {
                        vrc = true;
                        atc = true;
                        await SendAsync($"$DI{ClientCode}:CLIENT:client V1.00:3ef36a24");
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

                    if (info.StartsWith("$AX"))
                    {
                        // METAR
                        var station = info.Substring($"$AX{callsign}:SERVER:METAR:".Length, 4);
                        await SendMETARAsync(station);
                    }

                    if (info.StartsWith("$CQ"))
                    {
                        // TODO: Command 
                        // (e.g. $CQHYHY:@94835:BC:HY3088:2677)
                        // (e.g. $CQKAUS_TWR:SERVER:FP:DS-TZZ)

                        var tokens = info.Substring("$CQ".Length).Split(new char[] { ':' }, 4);
                        var sender = tokens[0];
                        var recipient = tokens[1];
                        var command = tokens[2];
                        var data = tokens.Length == 4 ? tokens[3] : null;

                        switch (command)
                        {
                            case "ATC":
                                if (recipient == "SERVER")
                                {
                                    await SendAsync($"$CRSERVER:{callsign}:ATC:N:{callsign}");
                                }
                                break;

                            case "CAPS":
                                if (recipient == "SERVER")
                                {
                                    await SendAsync($"$CRSERVER:{callsign}:CAPS:ATCINFO=1:SECPOS=1");
                                }

                                break;
                            case "IP":
                                if (recipient == "SERVER")
                                {
                                    var ipep = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
                                    var ipa = ipep.Address;
                                    await SendAsync($"$CRSERVER:{callsign}:IP:{ipa.ToString()}");

                                    await SendAsync($"$CQSERVER:{callsign}:CAPS");
                                }
                                break;

                            case "FP":
                                if (!string.IsNullOrEmpty(data))
                                {
                                    FlightPlanRequested?.Invoke(this, new FlightPlanRequestedEventArgs(data));
                                }
                                break;
                        }
                    }

                    if (info.StartsWith("#TM"))
                    {
                        // TODO: Message (e.g. #TMHYHY:FP:HY3088 SET 2677)0
                    }

                    if (this.vrc)
                    {
                        if (info.Contains($"$CQ{callsign}:SERVER:ATC:{callsign}") && !this.atc)
                        {
                            logger.LogInformation("Send VRC");
                            await SendAsync($"$CR{ClientCode}:{callsign}:ATC:Y:{callsign}");
                            this.atc = true;
                        }
                    }
                    else
                    {
                        if (info.StartsWith("#AA"))
                        {
                            logger.LogInformation("Connected");

                            var tokens = info.Substring("#AA".Length).Split(":");
                            callsign = tokens[0];
                            await SendAsync($"#TM{ClientCode}:{callsign}:Connected to {ClientName}.");

                            Connected?.Invoke(this, new ConnectedEventArgs(callsign));
                        }
                    }
                }
            }
        }

        public async Task SendMETARAsync(string station)
        {
            var metar = await GetLiveMETARAsync(station);
            logger.LogInformation($"Receive metar {metar}");
            await SendAsync($"$ARSERVER:{callsign}:METAR:{metar}");
        }

        private async Task SendAsync(string data)
        {
            if (writer != null)
            {
                await writer.WriteLineAsync(data);
                logger.LogDebug($"Sent: {data}");
            }
            else
            {
                logger.LogDebug($"Cannot send: {data}");
            }
        }

        private async Task<string> GetLiveMETARAsync(string station)
        {
            try
            {
                var data = await httpClient.GetStringAsync($"https://tgftp.nws.noaa.gov/data/observations/metar/stations/{station}.TXT");
                return data.Split("\n")[1].Trim();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }
    }
}
