using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using FlightEvents;

namespace UltraATC.FSDServer
{
    class HubConnector
    {
        private HubConnection hub;

        public TCPUser TCPUser;

        public string ClientID;

        public event EventHandler<AircraftUpdatedEventArgs> AircraftUpdated;
        public event EventHandler<FlightPlanUpdatedEventArgs> FlightPlanUpdated;
        public event EventHandler<AtcMessageSentEventArgs> AtcMessageRecieved;

        public HubConnector(TCPUser tcpUser)
        {
            TCPUser = tcpUser;
            ClientID = TCPUser.ClientID;
            tcpUser.Connected += ConnectSignalR;
            tcpUser.AtcUpdated += ATCUpdated;
            tcpUser.AtcLoggedOff += ATCLoggedOff;
            tcpUser.FlightPlanRequested += TcpUser_FlightPlanRequested;
            tcpUser.AtcMessageSent += TcpUser_AtcMessageSent;
            tcpUser.MessageSent += TcpUser_MessageSent;

            
        }

        public async void ConnectSignalR(object sender, ConnectedEventArgs e)
        {
            hub = new HubConnectionBuilder()
                    .WithUrl($"https://events.flighttracker.tech/FlightEventHub?clientType=Client&clientVersion=1&clientId={ClientID}")
                    .WithAutomaticReconnect()
                    .Build();

            hub.Closed += Hub_Closed;
            hub.Reconnecting += Hub_Reconnecting;
            hub.Reconnected += Hub_Reconnected;

            await hub.StartAsync();


            hub.On<string, AircraftStatus>("UpdateAircraft", async (connectionId, aircraftStatus) =>
            {
                AircraftUpdated?.Invoke(this, new AircraftUpdatedEventArgs(aircraftStatus));
            });

            hub.On<string, FlightPlanCompact>("ReturnFlightPlan", async (clientId, flightPlan) =>
            {
                FlightPlanUpdated?.Invoke(this, new FlightPlanUpdatedEventArgs(flightPlan));

            });

            hub.On<string, string>("SendATC", async (to, message) =>
            {
                AtcMessageRecieved?.Invoke(this, new AtcMessageSentEventArgs(to, message));
            });

            await hub.SendAsync("Join", "ATC");

            await hub.SendAsync("LoginATC", new ATCInfo
            {
                Callsign = e.Callsign,
                Latitude = e.Latitude,
                Longitude = e.Longitude,
                RealName = e.RealName,
                Certificate = e.Certificate,
                Rating = e.Rating
            });

        }



        #region EVENT HANDLERS

        private async Task Hub_Closed(Exception arg)
        {
            await TCPUser.SendAsync($"#TMSERVER:{TCPUser.Callsign}:Cannot Connect to Hub. Disconnecting!");
            TCPUser.Disconnect();
        }

        private async Task Hub_Reconnected(string arg)
        {
            await TCPUser.SendAsync($"#TMSERVER:{TCPUser.Callsign}:Hub Connection Restored!");
        }

        private async Task Hub_Reconnecting(Exception arg)
        {
            await TCPUser.SendAsync($"#TMSERVER:{TCPUser.Callsign}:Network Issues... Attempting to Recconect to the Hub...");
        }


        public async void ATCUpdated(object sender, AtcUpdatedEventArgs e)
        {
            await hub.SendAsync("UpdateATC", new ATCStatus
            {
                Callsign = e.Callsign,
                Latitude = e.Latitude,
                Longitude = e.Longitude,
                Altitude = e.Altitude,
                FrequencyCom = e.Frequency
            });
        }

        public async void ATCLoggedOff(object sender, AtcLoggedOffEventArgs e)
        {
            // De-Register ATC specific events
            hub.Remove("UpdateAircraft");
            hub.Remove("ReturnFlightPlan");
            hub.Remove("SendATC");

            await hub.SendAsync("UpdateATC", null);
        }

        private async void TcpUser_FlightPlanRequested(object sender, FlightPlanRequestedEventArgs e)
        {
            await hub.SendAsync("RequestFlightPlan", e.Callsign);
        }

        private async void TcpUser_AtcMessageSent(object sender, AtcMessageSentEventArgs e)
        {
            await hub.SendAsync("SendATC", e.To, e.Message);
        }

        private async void TcpUser_MessageSent(object sender, MessageSentEventArgs e)
        {
            await hub.SendAsync("SendMessage", TCPUser.Callsign, e.To, e.Message);
        } 
        #endregion

    }

}
