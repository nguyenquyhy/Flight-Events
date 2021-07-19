using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace UltraATC.FSDServer
{
    

    public class TCPUser
    {

        public string Callsign { get; set; }
        public IPAddress IpAddress { get; set; }

        //Generates Random String for ClientID
        public string ClientID { get; set; } = RandomString.Generate(32);

        private FSDServer mainThread;
        private HubConnector signalRHub;

        private readonly HttpClient httpClient;
        private TcpClient tcpClient;
        private Stream stream;
        private StreamWriter tcpWriter;
        private StreamReader tcpReader;

        private const string ClientCode = "ULATC";
        private const string ClientName = "UltraATC";

        //Events to be fired on the HubConnector
        public event EventHandler<ConnectedEventArgs> Connected;
        public event EventHandler<FlightPlanRequestedEventArgs> FlightPlanRequested;
        public event EventHandler<MessageSentEventArgs> MessageSent;
        public event EventHandler<AtcUpdatedEventArgs> AtcUpdated;
        public event EventHandler<AtcLoggedOffEventArgs> AtcLoggedOff;
        public event EventHandler<AtcMessageSentEventArgs> AtcMessageSent;

        /// <summary>
        /// Initial Constructor
        /// </summary>
        /// <param name="client">Tcp Connection for current user</param>
        /// <param name="fsdServer">Reference to Main Thread where dictionary is held</param>
        public TCPUser(TcpClient client, FSDServer fsdServer)
        {
            tcpClient = client;
            mainThread = fsdServer;

            //Follow stream and declare writers/readers
            stream = tcpClient.GetStream();
            tcpWriter = new StreamWriter(stream)
            {
                AutoFlush = true
            };
            tcpReader = new StreamReader(stream);
            
            //Web Client for API fetching of the METAR
            this.httpClient = new HttpClient();

            //Initial Declaration of SignalRClass
            signalRHub = new HubConnector(this);

            //^^Event Subscriptions from the HubConnector
            signalRHub.AircraftUpdated += SignalRHub_AircraftUpdated;
            signalRHub.FlightPlanUpdated += SignalRHub_FlightPlanUpdated;
            signalRHub.AtcMessageRecieved += SignalRHub_AtcMessageRecieved;
        }

        

        /// <summary>
        /// Initialization Method called by the main thread
        /// after adding to the dictionary.
        /// </summary>
        /// <returns>None</returns>
        public async Task Initialize()
        {
            //Initial Connection Message
            await SendAsync($"$DISERVER:CLIENT:VATSIM FSD V3.13:3ef36a24");

            //Keep Looping on this thread for new TCP messages
            while (true) {

                //Process TCP Stream Line by Line
                var info = await tcpReader.ReadLineAsync();
            
                if (await ProcessLineAsync(info))
                {
                    break;
                }
            }

            //Run Disconnect if stream is dropped
            Disconnect();

        }

        
        /// <summary>
        /// Disconnect Method. Clears Dictionary from main thread 
        /// and stops TCP Connections for this client.
        /// </summary>
        public void Disconnect()
        {
            tcpWriter = null;
            tcpClient.Close();
            AtcLoggedOff?.Invoke(this, new AtcLoggedOffEventArgs(Callsign));
            mainThread.Connections.Remove(tcpClient.Client.RemoteEndPoint);
        }


        #region TCP Processing
        /// <summary>
        /// Processes all of the required FSD commands sent through the TCP Stream.
        /// All Events fire on the HubConnector for processing on the SignalR server.
        /// </summary>
        /// <param name="info">Current passed from TCP Stream for processing.</param>
        /// <returns>Returns false if completed succesfully to keep the loop going and the stream open.</returns>
        private async Task<bool> ProcessLineAsync(string info)
        {
            //Check if Null and raise error to prevent thread blocking.
            if (info == null) throw new ArgumentNullException(nameof(info));

            //Inital Login Response from Radar Client.
            if (info.StartsWith("#AA"))
            {
                // #AACYVR_TWR:SERVER:HY:NA:123:3:9:1:0:49.19470:-123.18397:100
                // #AAEGHQ_ATIS:SERVER:Daniel Button:1343255::4:100

                var tokens = info.Substring("#AA".Length).Split(":");
                Callsign = tokens[0];
                var to = tokens[1];
                var RealName = tokens[2];
                var Certificate = tokens[3];
                var password = tokens[4];
                var Rating = tokens[5];
                var lat = tokens.Length > 9 ? double.Parse(tokens[9], CultureInfo.InvariantCulture) : (double?)null;
                var lon = tokens.Length > 10 ? double.Parse(tokens[10], CultureInfo.InvariantCulture) : (double?)null;
                await SendAsync($"#TM{ClientCode}:{Callsign}:Connected to {ClientName}.");

                //Password check currently defaulted to off.
                if (false /*password != "ATCUltra"*/)
                {
                    // $ERserver:unknown:006:12321:Invalid CID/password

                    //Issue Disconnect message for UX purposes
                    await SendAsync($"$ERserver:unknown:006:{Certificate}:Invalid Password");
                    Disconnect();
                }
                else
                {
                    //Welcome message issued from the server.
                    await SendAsync($"#TMSERVER:{Callsign}:UltraATC Radar - v{Environment.Version}");
                    await SendAsync($"#TMSERVER:{Callsign}:Welcome to UltraATC's Radar Environment.");
                    await SendAsync($"#TMSERVER:{Callsign}:Make sure that you have joined the \"Crew Lounge\" before PRIM up.");
                    await SendAsync($"#TMSERVER:{Callsign}:Message Bpruett#5874 for any questions!");

                    //Fire Connected event to trigger the SignalR Connection
                    Connected?.Invoke(this, new ConnectedEventArgs(Callsign, RealName, Certificate, Rating, lat, lon));
                }
            }

            //Fetch Metar then return it in FSD format to the client
            if (info.StartsWith("$AX"))
            {
                var station = info.Substring($"$AX{Callsign}:SERVER:METAR:".Length, 4);
                await SendMETARAsync(station);
            }

            //Flight Plan and Handoff Commands
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
                            await SendAsync($"$CRSERVER:{Callsign}:ATC:Y:{data}");
                            break;

                        case "CAPS":
                            await SendAsync($"$CRSERVER:{Callsign}:CAPS:ATCINFO=1:SECPOS=1");
                            break;
                        case "IP":
                            var ipep = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
                            var ipa = ipep.Address;
                            IpAddress = ipa;
                            await SendAsync($"$CRSERVER:{Callsign}:IP:{ipa}");

                            await SendAsync($"$CQSERVER:{Callsign}:CAPS");
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

            //Not sure the purpose of this one :) lol
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

            // Manual transfer
            if (info.StartsWith("$HO") || info.StartsWith("#PC"))
            {

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

            //Messages
            if (info.StartsWith($"#TM{Callsign}:"))
            {
                // TODO: Message (e.g. #TMHYHY:FP:HY3088 SET 2677)
                // #TMEDDM_TWR:@18700:hello all

                var tokens = info.Split(new char[] { ':' }, 3);
                var to = tokens[1];
                var msg = tokens[2];

                MessageSent?.Invoke(this, new MessageSentEventArgs(to, msg));
                AtcMessageSent?.Invoke(this, new AtcMessageSentEventArgs("*", info));
            }

            //Unknown
            if (info.StartsWith("#AP"))
            {
                // #APNWA360:SERVER:1363753::1:100:6:Jim Levain KBLI
                // #APTHY73Q:SERVER:1349469::1:100:2:Bedran Batkitar LTFM
            }

            //Change FP
            if (info.StartsWith("$AM"))
            {
                // $AMEDDF_TWR:SERVER:AS-205:I:TBM9:255:EDDF:9999:9999:12000:EDDL:99:99:99:99:ALTN::EDDF DF999 D1 DF996 DF995 D4 D5 D6 DF992 LISKU KUMIK DEGOM BOMBA GAPNU RONAD EDDL
                //$AMKCLT_TWR: SERVER: N1342T: I: ZZZZ: 0:KCLT: 5772193:1700073:0:NONE: 1:0:0:1:NONE::+
                var tokens = info.Substring("$AM".Length).Split(new char[] { ':' }, 4);
                var sender = tokens[0];
                //var recipient = tokens[1]; // SERVER
                var callsign = tokens[2];
                var flightPlan = tokens[3];

                AtcMessageSent?.Invoke(this, new AtcMessageSentEventArgs("*", $"$FP{callsign}:*A:{flightPlan}"));
            }


            //ATC UPDATE
            if (info.StartsWith("%" + Callsign))
            {
                // %EDDM_TWR:18700:4:100:3:48.35378:11.78609:0
                var tokens = info.Split(':');
                var freq = int.Parse("1" + tokens[1]);
                var alt = int.Parse(tokens[2]);
                //var protocol = tokens[3];
                //var rating = tokens[4];
                var lat = double.Parse(tokens[5], CultureInfo.InvariantCulture);
                var lng = double.Parse(tokens[6], CultureInfo.InvariantCulture);

                AtcUpdated?.Invoke(this, new AtcUpdatedEventArgs(Callsign, freq, alt, lat, lng));
                AtcMessageSent?.Invoke(this, new AtcMessageSentEventArgs("*", info));

            }

            //DISCONNECT and Stop TCP Stream
            if (info.StartsWith($"#DA{Callsign}:SERVER"))
            {
                // #DAEDDM_TWR:SERVER
                Disconnect();

                return true;
            }

            return false;


        }



        //Main Sending Method to the Client
        public async Task SendAsync(string data)
        {
            if (tcpWriter != null)
            {
                tcpWriter.WriteLineAsync(data);
            }
            else
            {
                Console.WriteLine("Cannot Send to" + Callsign + ": " + data);
            }
        } 
        #endregion

        #region Metar
        /// <summary>
        /// Formats metar then feeds into the TCP Send Method
        /// </summary>
        /// <param name="station">Metar station in ICAO format</param>
        /// <returns>FSD formated Metar</returns>
        private async Task SendMETARAsync(string station)
        {
            var metar = await GetLiveMETARAsync(station);
            SendAsync($"$ARSERVER:{Callsign}:METAR:{metar}");
        }

        //Metar API Fetch
        /// <summary>
        /// Quieries METAR API
        /// </summary>
        /// <param name="station">Metar station in ICAO format</param>
        /// <returns>Metar Data</returns>
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
        #endregion


        /// <summary>
        /// Sends flightplan through the TCP stream on another thread
        /// </summary>
        /// <param name="callsign">Aircraft Callsign</param>
        /// <param name="flightRule">IFR/VFR</param>
        /// <param name="aircraftType">Aircraft Type</param>
        /// <param name="registration">N/A</param>
        /// <param name="title">N/A</param>
        /// <param name="departure">Departure Field (ICAO)</param>
        /// <param name="arrival">Arrival Field (ICAO)</param>
        /// <param name="alternate">Alternate Field (ICAO)</param>
        /// <param name="route">Flight Routing (ICAO)</param>
        /// <param name="speed">Planned Speed</param>
        /// <param name="altitude">Planned Altitude</param>
        /// <param name="enroute">Planned Enroute Time</param>
        /// <param name="pilotRemarks">Other Remarks</param>
        /// <returns>Formats flight plan in FSD format then sends it through the TCP stream.</returns>
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

            SendAsync(fp);
        }


        #region EVENT_HANDLERS

        /// <summary>
        /// Catches aircraft update event from SignalR hub and sends it to the TCP Stream.
        /// </summary>
        /// <param name="sender">Event Sender</param>
        /// <param name="e">Aircraft Update Event Arguments</param>
        private async void SignalRHub_AircraftUpdated(object sender, AircraftUpdatedEventArgs e)
        {
            var ATCTransponderMode = e.AircraftStatus.TransponderMode switch
            {
                FlightEvents.TransponderMode.Standby => AtcTransponderMode.Standby,
                FlightEvents.TransponderMode.ModeC => AtcTransponderMode.ModeC,
                FlightEvents.TransponderMode.Ident => AtcTransponderMode.Ident,
                _ => AtcTransponderMode.Standby
            };

            var modeString = ATCTransponderMode switch
            {
                AtcTransponderMode.Standby => "S",
                AtcTransponderMode.ModeC => "N",
                AtcTransponderMode.Ident => "Y",
                _ => "N"
            };
            var rating = 1;

            //bitwise operation to convert to PBH value (Pitch, Bank, Heading)
            uint hdg = Convert.ToUInt32(e.AircraftStatus.TrueHeading) * 1024 / 360;
            uint pit = Convert.ToUInt32(0);// pitch); // NOTE: handle negative numbers
            uint bnk = Convert.ToUInt32(0);// bank); // NOTE: handle negative numbers
            uint pbh = ((pit & 0x3FF) << 22) | (bnk & 0x3FF) << 12 | ((hdg & 0x3FF) << 2); //62905944

            var pos = $"@{modeString}:{e.AircraftStatus.Callsign}:{e.AircraftStatus.Transponder}:{rating}:{e.AircraftStatus.Latitude}:{e.AircraftStatus.Longitude}:{e.AircraftStatus.Altitude}:{e.AircraftStatus.GroundSpeed}:{pbh}:5";
            if (tcpWriter != null)
            {
                SendAsync(pos);
            }
        }


        /// <summary>
        /// Catches update flight plan from SignalR server and sends to the TCP stream in FSD format.
        /// </summary>
        /// <param name="sender">Event Sender</param>
        /// <param name="e">Flight Plan Update Args</param>
        private async void SignalRHub_FlightPlanUpdated(object sender, FlightPlanUpdatedEventArgs e)
        {
            var ifrs = e.FlightPlan.Type == "IFR" ? "I" : "V";

            // NOTE: this is not needed right now due to lack of data
            //var remarks = $"Aircraft = {title.Replace(":", "_")}. Registration = {registration.Replace(":", "_")}";
            var remarks = string.IsNullOrWhiteSpace(e.FlightPlan.Remarks) ? string.Empty : e.FlightPlan.Remarks.Replace("\n", " ").Replace(":", "_");

            var departureEstimatedTime = "";
            var departureActualTime = "";

            var enrouteTime = e.FlightPlan.EstimatedEnroute == null ? ":" : $"{e.FlightPlan.EstimatedEnroute.Value.Hours:00}:{e.FlightPlan.EstimatedEnroute.Value.Minutes:00}";
            var fuelTime = ":";

            var fp = $"$FP{e.FlightPlan.Callsign}:*A:{ifrs}:{e.FlightPlan.AircraftType.Replace(":", "_")}:{e.FlightPlan.CruisingSpeed}:{e.FlightPlan.Departure}:{departureEstimatedTime}:{departureActualTime}:{e.FlightPlan.CruisingAltitude}:{e.FlightPlan.Destination}:{enrouteTime}:{fuelTime}:{e.FlightPlan.Alternate}:{remarks}:{e.FlightPlan.Route}";

            SendAsync(fp);
        }

        /// <summary>
        /// Checks if message from signalR server is applicable to client then forwards it
        /// to the TCP Stream.
        /// </summary>
        /// <param name="sender">Event Sender</param>
        /// <param name="e">Message Event Args</param>
        private void SignalRHub_AtcMessageRecieved(object sender, AtcMessageSentEventArgs e)
        {
            if (e.To == "*" || e.To == "@94835" || Callsign == e.To)
            {
                SendAsync(e.Message);
            }
        }

        #endregion
    }
}
