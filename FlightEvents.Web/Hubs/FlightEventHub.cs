using FlightEvents.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FlightEvents.Web.Hubs
{
    public class ClientInfo
    {
        public string ClientType { get; set; }
        public string ClientId { get; set; }
        public string ClientVersion { get; set; }
    }

    public class FlightEventHub : Hub<IFlightEventHub>
    {
        public static ConcurrentDictionary<string, string> ConnectionIdToClientIds => connectionIdToClientIds;
        public static ConcurrentDictionary<string, AircraftStatus> ConnectionIdToAircraftStatuses => connectionIdToAircraftStatuses;
        public static ConcurrentDictionary<string, ClientInfo> ConnetionIdToClientInfos => connetionIdToClientInfos;

        private static readonly ConcurrentDictionary<string, string> connectionIdToClientIds = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, string> clientIdToConnectionIds = new ConcurrentDictionary<string, string>();

        private static readonly ConcurrentDictionary<string, AircraftStatus> connectionIdToAircraftStatuses = new ConcurrentDictionary<string, AircraftStatus>();
        private static readonly ConcurrentDictionary<string, ATCInfo> connectionIdToToAtcInfos = new ConcurrentDictionary<string, ATCInfo>();
        private static readonly ConcurrentDictionary<string, ATCStatus> connectionIdToAtcStatuses = new ConcurrentDictionary<string, ATCStatus>();

        private static readonly ConcurrentDictionary<string, ChannelWriter<AircraftStatusBrief>> clientIdToChannelWriter = new ConcurrentDictionary<string, ChannelWriter<AircraftStatusBrief>>();

        private static readonly ConcurrentDictionary<string, (string, AircraftPosition)> connectionIdToTeleportRequest = new ConcurrentDictionary<string, (string, AircraftPosition)>();
        private static readonly ConcurrentDictionary<string, string> teleportTokenToConnectionId = new ConcurrentDictionary<string, string>();

        private static readonly ConcurrentDictionary<string, ClientInfo> connetionIdToClientInfos = new ConcurrentDictionary<string, ClientInfo>();

        private readonly ILogger<FlightEventHub> logger;
        private readonly IOptionsMonitor<FeaturesOptions> featuresOptionsAccessor;
        private readonly IDiscordConnectionStorage discordConnectionStorage;
        private readonly ILeaderboardStorage leaderboardStorage;
        private readonly IFlightEventStorage flightEventStorage;
        private readonly IATCFlightPlanStorage flightPlanStorage;

        public FlightEventHub(ILogger<FlightEventHub> logger, IOptionsMonitor<FeaturesOptions> featuresOptionsAccessor,
            IDiscordConnectionStorage discordConnectionStorage, ILeaderboardStorage leaderboardStorage,
            IFlightEventStorage flightEventStorage, IATCFlightPlanStorage flightPlanStorage)
        {
            this.logger = logger;
            this.featuresOptionsAccessor = featuresOptionsAccessor;
            this.discordConnectionStorage = discordConnectionStorage;
            this.leaderboardStorage = leaderboardStorage;
            this.flightEventStorage = flightEventStorage;
            this.flightPlanStorage = flightPlanStorage;
        }

        public override async Task OnConnectedAsync()
        {
            // Web or Client
            var clientType = (string)Context.GetHttpContext().Request.Query["clientType"];
            var clientId = (string)Context.GetHttpContext().Request.Query["clientId"];
            var clientVersion = (string)Context.GetHttpContext().Request.Query["clientVersion"];
            if (clientId != null)
            {
                connectionIdToClientIds[Context.ConnectionId] = clientId;
                // When a user reconnect, old connection ID is still in the list and need to be removed
                if (clientIdToConnectionIds.TryGetValue(clientId, out var oldConnectionId))
                {
                    connectionIdToClientIds.TryRemove(oldConnectionId, out _);
                }
                clientIdToConnectionIds[clientId] = Context.ConnectionId;
            }
            switch (clientType)
            {
                case "Web":
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Map");
                    break;
                case "Bot":
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Bot");
                    break;
                default:
                    break;
            }

            connetionIdToClientInfos.TryAdd(Context.ConnectionId, new ClientInfo
            {
                ClientType = clientType,
                ClientId = clientId,
                ClientVersion = clientVersion
            });
            await Clients.Group("Admin").UpdateClientList(connetionIdToClientInfos.Values.ToList());

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (connectionIdToClientIds.TryRemove(Context.ConnectionId, out var clientId))
            {
                clientIdToConnectionIds.TryRemove(clientId, out _);
                await Clients.Groups("Map", "ATC").UpdateATC(clientId, null, null);
            }
            RemoveCacheOnConnectionId(Context.ConnectionId);

            connetionIdToClientInfos.TryRemove(Context.ConnectionId, out _);
            await Clients.Group("Admin").UpdateClientList(connetionIdToClientInfos.Values.ToList());

            await base.OnDisconnectedAsync(exception);
        }

        [Authorize(Roles = "Admin")]
        public async Task LoginAdmin()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admin");
            await Clients.Caller.UpdateClientList(connetionIdToClientInfos.Values.ToList());
        }

        public void LoginATC(ATCInfo atc)
        {
            connectionIdToToAtcInfos[Context.ConnectionId] = atc;
        }

        public async Task UpdateATC(ATCStatus status)
        {
            if (connectionIdToClientIds.TryGetValue(Context.ConnectionId, out var clientId) && connectionIdToToAtcInfos.TryGetValue(Context.ConnectionId, out var atc))
            {
                int? fromFrequency = null;
                if (connectionIdToAtcStatuses.TryGetValue(Context.ConnectionId, out var lastStatus))
                {
                    fromFrequency = lastStatus.FrequencyCom;
                }
                if (status != null)
                {
                    connectionIdToAtcStatuses[Context.ConnectionId] = status;
                }
                if (fromFrequency != status?.FrequencyCom)
                {
                    await Clients.Groups("Bot").ChangeFrequency(clientId, fromFrequency, FixEuroscopeFrequency(status?.FrequencyCom));
                }

                await Clients.Groups("Map").UpdateATC(clientId, status, atc);
            }
        }

        public async Task UpdateAircraft(AircraftStatus status)
        {
            if (connectionIdToClientIds.TryGetValue(Context.ConnectionId, out var clientId))
            {
                // Sanitize status
                if (Math.Abs(status.Latitude) < 0.02 && Math.Abs(status.Longitude) < 0.02)
                {
                    // Aircraft is not loaded
                    status.FrequencyCom1 = 0;
                }

                // Cache latest status
                int fromFrequency = 0;
                if (connectionIdToAircraftStatuses.TryGetValue(Context.ConnectionId, out var lastStatus))
                {
                    fromFrequency = GetActiveFrequency(lastStatus);
                }
                connectionIdToAircraftStatuses[Context.ConnectionId] = status;

                if (!connectionIdToAtcStatuses.TryGetValue(Context.ConnectionId, out _))
                {
                    // Switch Discord channel based on COM1 change if ATC Mode is not active
                    var toFrequency = GetActiveFrequency(status);
                    if (fromFrequency != toFrequency)
                    {
                        logger.LogDebug("Send signal to Bot on changing frequency of {clientId} from {from} to {to}", clientId, fromFrequency, toFrequency);
                        await Clients.Groups("Bot").ChangeFrequency(clientId, fromFrequency == 0 ? null : (int?)fromFrequency, toFrequency == 0 ? null : (int?)toFrequency);
                    }
                }
                await Clients.Groups("ATC").UpdateAircraft(clientId, status);

                // NOTE: status broadcast is done by StatusBroadcastService to control throttling
            }
        }

        public async Task RequestStatusFromDiscord(ulong discordUserId)
        {
            var clientIds = await discordConnectionStorage.GetClientIdsAsync(discordUserId);
            foreach (var clientId in clientIds)
            {
                if (clientIdToConnectionIds.TryGetValue(clientId, out var connectionId) &&
                    connectionIdToAircraftStatuses.TryGetValue(connectionId, out var status))
                {
                    await Clients.Caller.UpdateAircraftToDiscord(discordUserId, clientId, status);
                    return;
                }
            }
            await Clients.Caller.UpdateAircraftToDiscord(discordUserId, clientIds.FirstOrDefault(), null);
        }

        public async Task RequestAircraftInfo(string clientId)
        {
            if (clientIdToConnectionIds.TryGetValue(clientId, out var connectionId))
            {
                await Clients.Client(connectionId).RequestAircraftInfo(Context.ConnectionId);
            }
        }

        public Task ReturnAircraftInfo(string requesterConnectionId, AircraftData info)
        {
            return Clients.Client(requesterConnectionId).ReturnAircraftInfo(connectionIdToClientIds[Context.ConnectionId], info);
        }

        public async Task AddFlightPlan(string clientId, string callsign, string source, FlightPlanCompact flightPlan)
        {
            await flightPlanStorage.SetFlightPlanAsync(callsign, clientId, flightPlan);

            await Clients.Group("ATC").ReturnFlightPlan(clientId, flightPlan);
        }

        public async Task RequestFlightPlan(string callsign)
        {
            var (clientId, flightPlan) = await flightPlanStorage.GetFlightPlanAsync(callsign);

            if (flightPlan == null)
            {
                await Clients.All.RequestFlightPlan(Context.ConnectionId, callsign);
            }
            else
            {
                await Clients.Caller.ReturnFlightPlan(clientId, flightPlan);
            }
        }

        public async Task ReturnFlightPlan(string clientId, FlightPlanCompact flightPlan, List<string> atcConnectionIds)
        {
            await Clients.Clients(atcConnectionIds).ReturnFlightPlan(clientId, flightPlan);
        }

        public async Task RequestFlightPlanDetails(string clientId)
        {
            var pairs = connectionIdToClientIds.ToArray();
            if (pairs.Any(p => p.Value == clientId))
            {
                var connectionId = pairs.First(p => p.Value == clientId).Key;
                await Clients.Clients(connectionId).RequestFlightPlanDetails(Context.ConnectionId);
            }
        }

        public async Task ReturnFlightPlanDetails(string connectionId, FlightPlanData flightPlan, string webConnectionId)
        {
            await Clients.Clients(webConnectionId).ReturnFlightPlanDetails(connectionId, flightPlan);
        }

        public async Task<ChannelReader<AircraftStatusBrief>> RequestFlightRoute(string clientId)
        {
            var channel = Channel.CreateUnbounded<AircraftStatusBrief>();
            clientIdToChannelWriter[clientId] = channel.Writer;
            await Clients.Clients(clientIdToConnectionIds[clientId]).RequestFlightRoute(Context.ConnectionId);
            return channel.Reader;
        }

        public async Task StreamFlightRoute(ChannelReader<AircraftStatusBrief> channel)
        {
            if (clientIdToChannelWriter.TryRemove(connectionIdToClientIds[Context.ConnectionId], out var writer))
            {
                Exception localException = null;
                try
                {
                    // Wait asynchronously for data to become available
                    while (await channel.WaitToReadAsync())
                    {
                        // Read all currently available data synchronously, before waiting for more data
                        while (channel.TryRead(out var status))
                        {
                            await writer.WriteAsync(status);
                        }
                    }
                }
                catch (Exception ex)
                {
                    localException = ex;
                }

                writer.Complete(localException);
            }
        }

        public async Task SendMessage(string from, string to, string message)
        {
            if (featuresOptionsAccessor.CurrentValue.SendMessage)
            {
                await Clients.All.SendMessage(from, to, message);
            }
        }

        public async Task ChangeUpdateRateByCallsign(string callsign, int hz)
        {
            await Clients.All.ChangeUpdateRateByCallsign(callsign, hz);
        }

        public async Task NotifyUpdateRateChanged(int hz)
        {
            await Clients.Group("Map").NotifyUpdateRateChanged(connectionIdToClientIds[Context.ConnectionId], hz);
        }

        public async Task Join(string group)
        {
            if (IsPublicGroup(group))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, group);

                if (group.StartsWith("Stopwatch:"))
                {
                    if (Guid.TryParse(group.Split(":")[1], out var eventId))
                    {
                        var evt = await flightEventStorage.GetAsync(eventId);

                        var eventStopwatches = stopwatches.GetOrAdd(eventId, new ConcurrentDictionary<Guid, EventStopwatch>());
                        foreach (var stopwatch in eventStopwatches.Values)
                        {
                            await Clients.Caller.UpdateStopwatch(stopwatch, DateTimeOffset.UtcNow);
                        }

                        var records = await leaderboardStorage.LoadAsync(evt.Id);
                        await Clients.Caller.UpdateLeaderboard(records);
                    }
                }

                if (group.StartsWith("Leaderboard:"))
                {
                    var eventIdString = group.Split(":")[1];
                    if (Guid.TryParse(eventIdString, out var eventId))
                    {
                        var evt = await flightEventStorage.GetAsync(eventId);

                        if (evt != null)
                        {
                            var records = await leaderboardStorage.LoadAsync(evt.Id);
                            await Clients.Caller.UpdateLeaderboard(records);
                        }
                    }
                }
            }
        }

        public async Task Leave(string group)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        }

        #region ATC

        public async Task SendATC(string to, string message)
        {
            var tokens = message.Split(":");
            var command = tokens[0][..3];
            var callsign = tokens[0][3..];

            switch (command)
            {
                case "$FP":
                    {
                        //$"$FP{callsign}:*A:{ifrs}:{aircraftType.Replace(":", "_")}:{speed}:{departure}:{departureEstimatedTime}:{departureActualTime}:{altitude}:{arrival}:{enrouteTime}:{fuelTime}:{alternate}:{remarks}:{route}";
                        var type = tokens[2];
                        var aircraftType = tokens[3];
                        var speed = tokens[4];
                        var departure = tokens[5];
                        var departureEstimatedTime = tokens[6];//
                        var departureActualTime = tokens[7];//
                        var altitude = tokens[8];
                        var arrival = tokens[9];
                        var enrouteTimeHour = tokens[10];
                        var enrouteTimeMinute = tokens[11];
                        var fuelTimeHour = tokens[12];//
                        var fuelTimeMinute = tokens[13];//
                        var alternate = tokens[14];
                        var remarks = tokens[15];
                        var route = tokens[16];

                        TimeSpan? enrouteTime = null;
                        int.TryParse(altitude, out int cruisingAltitude);
                        int.TryParse(speed, out int cruisingSpeed);
                        if (int.TryParse(enrouteTimeHour, out var hour) && int.TryParse(enrouteTimeMinute, out var minute))
                        {
                            enrouteTime = new TimeSpan(hour, minute, 0);
                        }

                        await flightPlanStorage.SetFlightPlanAsync(callsign, null, new FlightPlanCompact
                        {
                            Callsign = callsign,
                            Type = type == "I" ? "IFR" : "VFR",
                            AircraftType = aircraftType,
                            Departure = departure,
                            Destination = arrival,
                            Alternate = alternate,
                            CruisingAltitude = cruisingAltitude,
                            CruisingSpeed = cruisingSpeed,
                            EstimatedEnroute = enrouteTime,
                            Route = route,
                            Remarks = remarks,
                        });
                    }
                    break;
                case "#TM":
                    if (tokens.Length == 3 && tokens[1] == "FP")
                    {
                        if (!tokens[2].EndsWith(" GET") && !tokens[2].EndsWith(" release") && !tokens[2].Contains(" SET "))
                        {
                            logger.LogInformation("Unknown flight plan message '{message}' to {to}", message, to);
                        }

                        // Skip message sending to FP
                        return;
                    }
                    break;
            }

            await Clients.GroupExcept("ATC", Context.ConnectionId).SendATC(to, message);
        }

        #endregion

        #region Stopwatch

        private static ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, EventStopwatch>> stopwatches = new ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, EventStopwatch>>();

        [Authorize(Policy = "StopwatchManager")]
        public async Task GetStopwatches()
        {
            await Clients.Caller.ReturnStopwatches(stopwatches.Values.SelectMany(o => o.Values).ToList());
        }

        [Authorize(Policy = "StopwatchManager")]
        public async Task AddStopwatches(List<EventStopwatch> inputStopwatches)
        {
            foreach (var input in inputStopwatches)
            {
                var eventStopwatches = stopwatches.GetOrAdd(input.EventId, new ConcurrentDictionary<Guid, EventStopwatch>());
                eventStopwatches.TryRemove(input.Id, out _);
                if (eventStopwatches.TryAdd(input.Id, input))
                {
                    await Clients.Group("Stopwatch:" + input.EventId).UpdateStopwatch(input, DateTimeOffset.UtcNow);
                }
            }
        }

        [Authorize(Policy = "StopwatchManager")]
        public async Task AddStopwatch(EventStopwatch input)
        {
            var stopwatch = new EventStopwatch
            {
                Id = Guid.NewGuid(),
                LeaderboardName = input.LeaderboardName,
                Name = input.Name,
                EventId = input.EventId,
                AddedDateTime = DateTimeOffset.UtcNow
            };
            var eventStopwatches = stopwatches.GetOrAdd(input.EventId, new ConcurrentDictionary<Guid, EventStopwatch>());
            if (eventStopwatches.TryAdd(stopwatch.Id, stopwatch))
            {
                await Clients.Group("Stopwatch:" + input.EventId).UpdateStopwatch(stopwatch, DateTimeOffset.UtcNow);
            }
        }

        [Authorize(Policy = "StopwatchManager")]
        public async Task StartAllStopwatches(Guid eventId)
        {
            var eventStopwatches = stopwatches.GetOrAdd(eventId, new ConcurrentDictionary<Guid, EventStopwatch>());
            var startTime = DateTimeOffset.UtcNow;
            foreach (var stopwatch in eventStopwatches.Values)
            {
                if (stopwatch.StartedDateTime == null && stopwatch.StoppedDateTime == null)
                {
                    stopwatch.StartedDateTime = startTime;
                    stopwatch.StoppedDateTime = null;
                    await Clients.Group("Stopwatch:" + eventId).UpdateStopwatch(stopwatch, DateTimeOffset.UtcNow);
                }
            }
        }

        [Authorize(Policy = "StopwatchManager")]
        public async Task StartStopwatch(Guid eventId, Guid id)
        {
            var eventStopwatches = stopwatches.GetOrAdd(eventId, new ConcurrentDictionary<Guid, EventStopwatch>());
            if (eventStopwatches.TryGetValue(id, out var stopwatch))
            {
                if (stopwatch.StartedDateTime == null && stopwatch.StoppedDateTime == null)
                {
                    stopwatch.StartedDateTime = DateTimeOffset.UtcNow;
                    await Clients.Group("Stopwatch:" + eventId).UpdateStopwatch(stopwatch, DateTimeOffset.UtcNow);
                }
            }
        }

        [Authorize(Policy = "StopwatchManager")]
        public async Task RestartStopwatch(Guid eventId, Guid id)
        {
            var eventStopwatches = stopwatches.GetOrAdd(eventId, new ConcurrentDictionary<Guid, EventStopwatch>());
            if (eventStopwatches.TryGetValue(id, out var stopwatch))
            {
                stopwatch.StartedDateTime = DateTimeOffset.UtcNow;
                stopwatch.StoppedDateTime = null;
                stopwatch.LapsDateTime.Clear();
                await Clients.Group("Stopwatch:" + eventId).UpdateStopwatch(stopwatch, DateTimeOffset.UtcNow);
            }
        }

        [Authorize(Policy = "StopwatchManager")]
        public async Task ResetStopwatch(Guid eventId, Guid id)
        {
            var eventStopwatches = stopwatches.GetOrAdd(eventId, new ConcurrentDictionary<Guid, EventStopwatch>());
            if (eventStopwatches.TryGetValue(id, out var stopwatch))
            {
                stopwatch.StartedDateTime = null;
                stopwatch.StoppedDateTime = null;
                stopwatch.LapsDateTime.Clear();
                await Clients.Group("Stopwatch:" + eventId).UpdateStopwatch(stopwatch, DateTimeOffset.UtcNow);
            }
        }

        [Authorize(Policy = "StopwatchManager")]
        public async Task LapStopwatch(Guid eventId, Guid id)
        {
            var eventStopwatches = stopwatches.GetOrAdd(eventId, new ConcurrentDictionary<Guid, EventStopwatch>());
            if (eventStopwatches.TryGetValue(id, out var stopwatch)
                && stopwatch.StartedDateTime.HasValue && !stopwatch.StoppedDateTime.HasValue)
            {
                var dateTime = DateTimeOffset.UtcNow;
                stopwatch.LapsDateTime.Add(dateTime);

                var evt = await flightEventStorage.GetAsync(eventId);
                if (IsRaceCompleted(stopwatch, evt))
                {
                    stopwatch.StoppedDateTime = dateTime;
                }

                await Clients.Group("Stopwatch:" + eventId).UpdateStopwatch(stopwatch, DateTimeOffset.UtcNow);
            }
        }

        [Authorize(Policy = "StopwatchManager")]
        public async Task StopStopwatch(Guid eventId, Guid id)
        {
            var eventStopwatches = stopwatches.GetOrAdd(eventId, new ConcurrentDictionary<Guid, EventStopwatch>());
            if (eventStopwatches.TryGetValue(id, out var stopwatch))
            {
                stopwatch.StoppedDateTime = DateTimeOffset.UtcNow;
                await Clients.Group("Stopwatch:" + eventId).UpdateStopwatch(stopwatch, DateTimeOffset.UtcNow);
            }
        }

        [Authorize(Policy = "StopwatchManager")]
        public async Task RemoveStopwatch(Guid eventId, Guid id)
        {
            var eventStopwatches = stopwatches.GetOrAdd(eventId, new ConcurrentDictionary<Guid, EventStopwatch>());
            if (eventStopwatches.TryRemove(id, out var stopwatch))
            {
                await Clients.Group("Stopwatch:" + eventId).RemoveStopwatch(stopwatch);
            }
        }

        [Authorize(Policy = "StopwatchManager")]
        public async Task SaveStopwatch(Guid eventId, Guid id)
        {
            var eventStopwatches = stopwatches.GetOrAdd(eventId, new ConcurrentDictionary<Guid, EventStopwatch>());
            if (eventStopwatches.TryRemove(id, out var stopwatch))
            {
                await Clients.Group("Stopwatch:" + eventId).RemoveStopwatch(stopwatch);

                var evt = await flightEventStorage.GetAsync(eventId);
                // Create leaderboard for each lap and full race
                if (IsRaceCompleted(stopwatch, evt))
                {
                    // If there is no checkpoint or racers complete all checkpoint, create full race lap
                    await CreateFullRaceLeaderboardRecord(stopwatch);
                }

                if (evt.Checkpoints != null)
                {
                    for (var i = 0; i < stopwatch.LapsDateTime.Count; i++)
                    {
                        // If there is no checkpoint or racers complete all checkpoint, create record for each lap
                        await CreateLapLeaderboardRecord(stopwatch, i);
                    }
                }

                var records = await leaderboardStorage.LoadAsync(evt.Id);
                await Clients.Group("Stopwatch:" + eventId).UpdateLeaderboard(records);
                await Clients.Group("Leaderboard:" + evt.Id).UpdateLeaderboard(records);
            }
        }

        private async Task CreateFullRaceLeaderboardRecord(EventStopwatch stopwatch)
        {
            var lapTime = stopwatch.LapsDateTime[^1] - stopwatch.StartedDateTime.Value;

            var leaderboardRecord = new LeaderboardRecord(stopwatch, 0, lapTime, lapTime);

            await leaderboardStorage.SaveAsync(leaderboardRecord);
        }

        private async Task CreateLapLeaderboardRecord(EventStopwatch stopwatch, int i)
        {
            var lastTime = i == 0 ? stopwatch.StartedDateTime.Value : stopwatch.LapsDateTime[i - 1];
            var timeSinceLast = stopwatch.LapsDateTime[i] - lastTime;

            var leaderboardRecord = new LeaderboardRecord(stopwatch, i + 1, timeSinceLast, stopwatch.LapsDateTime[i] - stopwatch.StartedDateTime.Value);

            await leaderboardStorage.SaveAsync(leaderboardRecord);
        }

        [Authorize(Policy = "StopwatchManager")]
        public async Task ReloadLeaderboard(Guid eventId)
        {
            var records = await leaderboardStorage.LoadAsync(eventId);
            await Clients.Group("Stopwatch:" + eventId).UpdateLeaderboard(records);
            await Clients.Group("Leaderboard:" + eventId).UpdateLeaderboard(records);
        }

        #endregion

        public void RequestTeleport(string token, double latitude, double longitude, double altitude)
        {
            connectionIdToTeleportRequest[Context.ConnectionId] = (token, new AircraftPosition
            {
                Latitude = latitude,
                Longitude = longitude,
                Altitude = altitude
            });
            teleportTokenToConnectionId[token] = Context.ConnectionId;
        }

        public async Task AcceptTeleport(string token)
        {
            if (teleportTokenToConnectionId.TryGetValue(token, out string connectionId) && connectionIdToTeleportRequest.TryGetValue(connectionId, out var request))
            {
                await Clients.Caller.Teleport(connectionId, request.Item2);
            }
        }

        public static void RemoveCacheOnConnectionId(string connectionId)
        {
            connectionIdToAircraftStatuses.TryRemove(connectionId, out _);
            connectionIdToAtcStatuses.TryRemove(connectionId, out _);
        }

        private static int GetActiveFrequency(AircraftStatus status)
            => status.TransmitCom3 ? status.FrequencyCom3 : (status.TransmitCom2 ? status.FrequencyCom2 : status.FrequencyCom1);

        private static bool IsPublicGroup(string groupName)
            => groupName == "ATC" || groupName == "ClientMap" || groupName.StartsWith("Leaderboard:") || groupName.StartsWith("Stopwatch:");

        private static int? FixEuroscopeFrequency(int? frequency)
        {
            if (frequency.HasValue)
            {
                var twoDigits = frequency.Value % 100;
                if (twoDigits == 20 || twoDigits == 40 || twoDigits == 70)
                {
                    return frequency.Value + 5;
                }
            }

            return frequency;
        }

        private static bool IsRaceCompleted(EventStopwatch stopwatch, Data.FlightEvent evt)
        {
            return (evt.Checkpoints == null && stopwatch.LapsDateTime.Count > 0)
                || (evt.Checkpoints != null && stopwatch.LapsDateTime.Count == evt.Checkpoints.Count - 1);
        }
    }
}
