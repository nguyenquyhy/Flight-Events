﻿using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace FlightEvents.Client.ATC
{
    public enum AtcTransponderMode
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
        private string callsign;
        private TcpListener tcpListener;
        private TcpClient tcpClient;

        public event EventHandler<ConnectedEventArgs> Connected;
        public event EventHandler<FlightPlanRequestedEventArgs> FlightPlanRequested;
        public event EventHandler<MessageSentEventArgs> MessageSent;
        public event EventHandler<AtcUpdatedEventArgs> AtcUpdated;
        public event EventHandler<AtcLoggedOffEventArgs> AtcLoggedOff;

        /// <summary>
        /// Signal a Message need to be sent to another ATC client
        /// </summary>
        public event EventHandler<AtcMessageSentEventArgs> AtcMessageSent;

        public ATCServer(ILogger<ATCServer> logger)
        {
            this.logger = logger;
            this.httpClient = new HttpClient();
        }

        public void Start(bool vatsimMode)
        {
            tcpListener = new TcpListener(IPAddress.Any, 6809);
            tcpListener.Start();

            Task.Run(async () =>
            {
                try
                {
                    await AcceptAndProcessAsync(tcpListener, vatsimMode);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Cannot process ATC message!");
                }
            });
        }

        public void Stop()
        {
            tcpClient?.Close();
            tcpClient = null;
            tcpListener?.Stop();
            tcpListener = null;
        }

        public async Task SendPositionAsync(string callsign, string squawk, double latitude, double longitude, double altitude, double groundSpeed, double pitch, double bank, double heading, AtcTransponderMode transponderMode)
        {
            var modeString = transponderMode switch
            {
                AtcTransponderMode.Standby => "S",
                AtcTransponderMode.ModeC => "N",
                AtcTransponderMode.Ident => "Y",
                _ => "N"
            };
            var rating = 1;

            //bitwise operation to convert to PBH value (Pitch, Bank, Heading)
            uint hdg = Convert.ToUInt32(heading) * 1024 / 360;
            uint pit = Convert.ToUInt32(0);// pitch); // NOTE: handle negative numbers
            uint bnk = Convert.ToUInt32(0);// bank); // NOTE: handle negative numbers
            uint pbh = ((pit & 0x3FF) << 22) | (bnk & 0x3FF) << 12 | ((hdg & 0x3FF) << 2); //62905944

            var pos = $"@{modeString}:{callsign}:{squawk}:{rating}:{latitude}:{longitude}:{altitude}:{groundSpeed}:{pbh}:5";
            if (writer != null)
            {
                await writer.WriteLineAsync(pos);
                logger.LogTrace("Sent Position: {position}", pos);
            }
        }

        public async Task SendFlightPlanAsync(string callsign, string flightRule, string aircraftType, string registration, string title,
            string departure, string arrival, string alternate, string route, int? speed, int altitude, TimeSpan? enroute, string pilotRemarks)
        {
            var ifrs = flightRule == "IFR" ? "I" : "V";

            // NOTE: this is not needed right now due to lack of data
            //var remarks = $"Aircraft = {title.Replace(":", "_")}. Registration = {registration.Replace(":", "_")}";
            var remarks = string.IsNullOrWhiteSpace(pilotRemarks) ? string.Empty : pilotRemarks.Replace("\n", " ").Replace(":", "_");

            var departureEstimatedTime = "";
            var departureActualTime = "";

            var enrouteTime = enroute == null ? ":" : $"{enroute.Value.Hours:00}:{enroute.Value.Minutes:00}";
            var fuelTime = ":";

            var fp = $"$FP{callsign}:*A:{ifrs}:{aircraftType.Replace(":", "_")}:{speed}:{departure}:{departureEstimatedTime}:{departureActualTime}:{altitude}:{arrival}:{enrouteTime}:{fuelTime}:{alternate}:{remarks}:{route}";

            await writer?.WriteLineAsync(fp);
            await writer?.FlushAsync();
            logger.LogInformation("Sent Flight Plan: " + fp);
        }

        private async Task AcceptAndProcessAsync(TcpListener tcpListener, bool vatsimMode)
        {
            while (true)
            {
                logger.LogInformation("Waiting for connection...");

                tcpClient = tcpListener.AcceptTcpClient();
                logger.LogInformation("Accepted a connection");

                try
                {
                    using var stream = tcpClient.GetStream();
                    var reader = new StreamReader(stream);
                    writer = new StreamWriter(stream)
                    {
                        AutoFlush = true
                    };

                    if (vatsimMode)
                    {
                        await SendAsync($"$DISERVER:CLIENT:VATSIM FSD V3.13:3ef36a24");
                    }

                    while (true)
                    {
                        var info = await reader.ReadLineAsync();

                        if (await ProcessLineAsync(info))
                        {
                            break;
                        }
                    }
                }
                catch (IOException)
                {
                    Disconnect();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <returns>true if connection is finished and need to be closed</returns>
        private async Task<bool> ProcessLineAsync(string info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));

            logger.LogInformation($"Receive: {info}");

            if (info.StartsWith("%" + callsign))
            {
                // %EDDM_TWR:18700:4:100:3:48.35378:11.78609:0
                var tokens = info.Split(':');
                var freq = int.Parse("1" + tokens[1]);
                var alt = int.Parse(tokens[2]);
                //var protocol = tokens[3];
                //var rating = tokens[4];
                var lat = double.Parse(tokens[5], CultureInfo.InvariantCulture);
                var lng = double.Parse(tokens[6], CultureInfo.InvariantCulture);

                AtcUpdated?.Invoke(this, new AtcUpdatedEventArgs(callsign, freq, alt, lat, lng));
                AtcMessageSent?.Invoke(this, new AtcMessageSentEventArgs("*", info));
            }

            if (info.StartsWith($"#DA{callsign}:SERVER"))
            {
                // #DAEDDM_TWR:SERVER
                Disconnect();

                return true;
            }

            if (info.StartsWith("$AM"))
            {
                // Modify flight plan
                // $AMEDDF_TWR:SERVER:AS-205:I:TBM9:255:EDDF:9999:9999:12000:EDDL:99:99:99:99:ALTN::EDDF DF999 D1 DF996 DF995 D4 D5 D6 DF992 LISKU KUMIK DEGOM BOMBA GAPNU RONAD EDDL

                var tokens = info.Substring("$AM".Length).Split(new char[] { ':' }, 4);
                var sender = tokens[0];
                //var recipient = tokens[1]; // SERVER
                var callsign = tokens[2];
                var flightPlan = tokens[3];

                AtcMessageSent?.Invoke(this, new AtcMessageSentEventArgs("*", $"$FP{callsign}:*A:{flightPlan}"));
            }

            if (info.StartsWith("#AP"))
            {
                // #APNWA360:SERVER:1363753::1:100:6:Jim Levain KBLI
                // #APTHY73Q:SERVER:1349469::1:100:2:Bedran Batkitar LTFM
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
                // (e.g. $CQHYHY:@94835:BC:HY3088:2677 --- Set squawk)
                // (e.g. $CQKAUS_TWR:SERVER:FP:DS-TZZ --- Requesting flight plan)

                //$CQCYVR_TWR:@94835:DR:USEP6Q --- Assume a callsign
                //#TMCYVR_TWR:FP:USEP6Q --- Release

                var tokens = info.Substring("$CQ".Length).Split(new char[] { ':' }, 4);
                var sender = tokens[0];
                var recipient = tokens[1];
                var command = tokens[2];
                var data = tokens.Length == 4 ? tokens[3] : null;

                if (recipient == "SERVER")
                {
                    switch (command)
                    {
                        case "ATC":
                            await SendAsync($"$CRSERVER:{callsign}:ATC:Y:{data}");
                            break;

                        case "CAPS":
                            await SendAsync($"$CRSERVER:{callsign}:CAPS:ATCINFO=1:SECPOS=1");
                            break;
                        case "IP":
                            var ipep = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
                            var ipa = ipep.Address;
                            await SendAsync($"$CRSERVER:{callsign}:IP:{ipa}");

                            await SendAsync($"$CQSERVER:{callsign}:CAPS");
                            break;
                        case "FP":
                            FlightPlanRequested?.Invoke(this, new FlightPlanRequestedEventArgs(data));
                            break;
                    }
                }
                else
                {
                    // Real Name
                    // $CQCYVR_TWR:EDDM_GND:RN
                    // Expect: $CREDDM_GND:WADD_TWR:RN:<Name>Hy:<location>Vancouver FIR 2004/1-1 CZVR 20200404:<rating>3

                    AtcMessageSent?.Invoke(this, new AtcMessageSentEventArgs(recipient, info));
                }
            }

            if (info.StartsWith("$CR"))
            {
                //$CRCYVR_TWR:CYVR_GND:CAPS:ATCINFO=1:SECPOS=1:MODELDESC=1:ONGOINGCOORD=1

                var tokens = info.Split(":");
                var recipient = tokens[1];

                if (recipient == "SERVER")
                {
                    // Ignore
                }
                else
                {
                    AtcMessageSent?.Invoke(this, new AtcMessageSentEventArgs(recipient, info));
                }
            }

            if (info.StartsWith("$HO") || info.StartsWith("#PC"))
            {
                // Manual transfer
                // $HOCYVR_TWR:CZVR_CTR:NF-OJS
                // #PCCYVR_TWR:CZVR_CTR:CCP:ST:NF-OJS:1:::::::::

                var tokens = info.Split(":");
                var recipient = tokens[1];

                if (recipient == "SERVER")
                {
                    // Probably never happen
                }
                else
                {
                    AtcMessageSent?.Invoke(this, new AtcMessageSentEventArgs(recipient, info));
                }
            }

            if (info.StartsWith($"#TM{callsign}:"))
            {
                // TODO: Message (e.g. #TMHYHY:FP:HY3088 SET 2677)
                // #TMEDDM_TWR:@18700:hello all

                var tokens = info.Split(new char[] { ':' }, 3);
                var to = tokens[1];
                var msg = tokens[2];

                MessageSent?.Invoke(this, new MessageSentEventArgs(to, msg));
                AtcMessageSent?.Invoke(this, new AtcMessageSentEventArgs("*", info));
            }

            if (info.StartsWith("#AA"))
            {
                // #AACYVR_TWR:SERVER:HY:NA:123:3:9:1:0:49.19470:-123.18397:100
                // #AAEGHQ_ATIS:SERVER:Daniel Button:1343255::4:100
                logger.LogInformation("Connected");

                var tokens = info.Substring("#AA".Length).Split(":");
                callsign = tokens[0];
                var to = tokens[1];
                var realName = tokens[2];
                var certificate = tokens[3];
                var password = tokens[4];
                var rating = tokens[5];
                var lat = tokens.Length > 9 ? double.Parse(tokens[9], CultureInfo.InvariantCulture) : (double?)null;
                var lon = tokens.Length > 10 ? double.Parse(tokens[10], CultureInfo.InvariantCulture) : (double?)null;
                await SendAsync($"#TM{ClientCode}:{callsign}:Connected to {ClientName}.");

                Connected?.Invoke(this, new ConnectedEventArgs(callsign, realName, certificate, rating, lat, lon));
            }

            return false;
        }

        private void Disconnect()
        {
            logger.LogInformation("Client is disconnected");
            writer = null;

            AtcLoggedOff?.Invoke(this, new AtcLoggedOffEventArgs(callsign));
        }

        public async Task SendMETARAsync(string station)
        {
            var metar = await GetLiveMETARAsync(station);
            logger.LogInformation($"Receive metar {metar}");
            await SendAsync($"$ARSERVER:{callsign}:METAR:{metar}");
        }

        public async Task SendAsync(string data)
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
