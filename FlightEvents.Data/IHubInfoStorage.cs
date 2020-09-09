using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace FlightEvents.Data
{
    public class HubInfoStorage
    {
        #region Data that should be cleared on restart & not across servers

        public static ConcurrentDictionary<string, string> ConnectionIdToClientIds { get; } = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, string> clientIdToConnectionIds = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, AircraftStatus> connectionIdToAircraftStatuses = new ConcurrentDictionary<string, AircraftStatus>();

        #endregion

        #region

        #endregion

        public static ConcurrentDictionary<string, AircraftStatus> ConnectionIdToAircraftStatuses => connectionIdToAircraftStatuses;
        public static ConcurrentDictionary<string, string> ClientIdToAircraftGroup => clientIdToAircraftGroup;

        private static readonly ConcurrentDictionary<string, ATCInfo> connectionIdToToAtcInfos = new ConcurrentDictionary<string, ATCInfo>();
        private static readonly ConcurrentDictionary<string, ATCStatus> connectionIdToAtcStatuses = new ConcurrentDictionary<string, ATCStatus>();

        private static readonly ConcurrentDictionary<string, ChannelWriter<AircraftStatusBrief>> clientIdToChannelWriter = new ConcurrentDictionary<string, ChannelWriter<AircraftStatusBrief>>();

        private static readonly ConcurrentDictionary<string, (string, AircraftPosition)> connectionIdToTeleportRequest = new ConcurrentDictionary<string, (string, AircraftPosition)>();
        private static readonly ConcurrentDictionary<string, string> teleportTokenToConnectionId = new ConcurrentDictionary<string, string>();

        private static readonly ConcurrentDictionary<string, string> clientIdToAircraftGroup = new ConcurrentDictionary<string, string>();
    }
}
