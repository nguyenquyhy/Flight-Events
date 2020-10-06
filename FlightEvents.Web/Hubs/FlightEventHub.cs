using FlightEvents.Data;
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
    public class FlightEventHub : Hub<IFlightEventHub>
    {
        public static ConcurrentDictionary<string, string> ConnectionIdToClientIds => connectionIdToClientIds;
        public static ConcurrentDictionary<string, AircraftStatus> ConnectionIdToAircraftStatuses => connectionIdToAircraftStatuses;

        private static readonly ConcurrentDictionary<string, string> connectionIdToClientIds = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, string> clientIdToConnectionIds = new ConcurrentDictionary<string, string>();

        private static readonly ConcurrentDictionary<string, AircraftStatus> connectionIdToAircraftStatuses = new ConcurrentDictionary<string, AircraftStatus>();
        private static readonly ConcurrentDictionary<string, ATCInfo> connectionIdToToAtcInfos = new ConcurrentDictionary<string, ATCInfo>();
        private static readonly ConcurrentDictionary<string, ATCStatus> connectionIdToAtcStatuses = new ConcurrentDictionary<string, ATCStatus>();

        private static readonly ConcurrentDictionary<string, ChannelWriter<AircraftStatusBrief>> clientIdToChannelWriter = new ConcurrentDictionary<string, ChannelWriter<AircraftStatusBrief>>();

        private static readonly ConcurrentDictionary<string, (string, AircraftPosition)> connectionIdToTeleportRequest = new ConcurrentDictionary<string, (string, AircraftPosition)>();
        private static readonly ConcurrentDictionary<string, string> teleportTokenToConnectionId = new ConcurrentDictionary<string, string>();

        private readonly ILogger<FlightEventHub> logger;
        private readonly IOptionsMonitor<FeaturesOptions> featuresOptionsAccessor;
        private readonly IDiscordConnectionStorage discordConnectionStorage;
        private readonly ILeaderboardStorage leaderboardStorage;
        private readonly IFlightEventStorage flightEventStorage;

        public FlightEventHub(ILogger<FlightEventHub> logger, IOptionsMonitor<FeaturesOptions> featuresOptionsAccessor,
            IDiscordConnectionStorage discordConnectionStorage, ILeaderboardStorage leaderboardStorage,
            IFlightEventStorage flightEventStorage)
        {
            this.logger = logger;
            this.featuresOptionsAccessor = featuresOptionsAccessor;
            this.discordConnectionStorage = discordConnectionStorage;
            this.leaderboardStorage = leaderboardStorage;
            this.flightEventStorage = flightEventStorage;
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
            await base.OnDisconnectedAsync(exception);
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
                    await Clients.Groups("Bot").ChangeFrequency(clientId, fromFrequency, status?.FrequencyCom);
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
                    fromFrequency = lastStatus.FrequencyCom1;
                }
                connectionIdToAircraftStatuses[Context.ConnectionId] = status;

                if (!connectionIdToAtcStatuses.TryGetValue(Context.ConnectionId, out _))
                {
                    // Switch Discord channel based on COM1 change if ATC Mode is not active
                    var toFrequency = status.FrequencyCom1;
                    if (fromFrequency != toFrequency)
                    {
                        logger.LogDebug("Send signal to Bot on changing frequency of {clientId} from {from} to {to}", clientId, fromFrequency, toFrequency);
                        await Clients.Groups("Bot").ChangeFrequency(clientId, fromFrequency == 0 ? null : (int?)fromFrequency, toFrequency == 0 ? null : (int?)toFrequency);
                    }
                }
                await Clients.Groups("ATC").UpdateAircraft(clientId, status);
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

        public async Task RequestFlightPlan(string callsign)
        {
            await Clients.All.RequestFlightPlan(Context.ConnectionId, callsign);
        }

        public async Task ReturnFlightPlan(string connectionId, FlightPlanCompact flightPlan, List<string> atcConnectionIds)
        {
            await Clients.Clients(atcConnectionIds).ReturnFlightPlan(connectionId, flightPlan);
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
            await Groups.AddToGroupAsync(Context.ConnectionId, group);

            if (group.StartsWith("Stopwatch:"))
            {
                var eventCode = group.Split(":")[1];
                var evt = await flightEventStorage.GetByStopwatchCodeAsync(eventCode);

                var eventStopwatches = stopwatches.GetOrAdd(eventCode, new ConcurrentDictionary<Guid, EventStopwatch>());
                foreach (var stopwatch in eventStopwatches.Values)
                {
                    await Clients.Caller.UpdateStopwatch(stopwatch, DateTimeOffset.UtcNow);
                }

                var records = await leaderboardStorage.LoadAsync(evt.Id);
                await Clients.Caller.UpdateLeaderboard(records);
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

        public async Task Leave(string group)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        }

        #region ATC

        public async Task SendATC(string to, string message)
        {
            await Clients.GroupExcept("ATC", Context.ConnectionId).SendATC(to, message);
        }

        #endregion

        #region Stopwatch

        private static ConcurrentDictionary<string, ConcurrentDictionary<Guid, EventStopwatch>> stopwatches = new ConcurrentDictionary<string, ConcurrentDictionary<Guid, EventStopwatch>>();

        public async Task AddStopwatch(EventStopwatch input)
        {
            var stopwatch = new EventStopwatch
            {
                Id = Guid.NewGuid(),
                LeaderboardName = input.LeaderboardName,
                Name = input.Name,
                EventCode = input.EventCode,
                AddedDateTime = DateTimeOffset.UtcNow
            };
            var eventStopwatches = stopwatches.GetOrAdd(input.EventCode, new ConcurrentDictionary<Guid, EventStopwatch>());
            if (eventStopwatches.TryAdd(stopwatch.Id, stopwatch))
            {
                await Clients.Group("Stopwatch:" + input.EventCode).UpdateStopwatch(stopwatch, DateTimeOffset.UtcNow);
            }
        }

        public async Task StartAllStopwatches(string eventCode)
        {
            var eventStopwatches = stopwatches.GetOrAdd(eventCode, new ConcurrentDictionary<Guid, EventStopwatch>());
            var startTime = DateTimeOffset.UtcNow;
            foreach (var stopwatch in eventStopwatches.Values)
            {
                if (stopwatch.StartedDateTime == null && stopwatch.StoppedDateTime == null)
                {
                    stopwatch.StartedDateTime = startTime;
                    stopwatch.StoppedDateTime = null;
                    await Clients.Group("Stopwatch:" + eventCode).UpdateStopwatch(stopwatch, DateTimeOffset.UtcNow);
                }
            }
        }

        public async Task StartStopwatch(string eventCode, Guid id)
        {
            var eventStopwatches = stopwatches.GetOrAdd(eventCode, new ConcurrentDictionary<Guid, EventStopwatch>());
            if (eventStopwatches.TryGetValue(id, out var stopwatch))
            {
                if (stopwatch.StartedDateTime == null && stopwatch.StoppedDateTime == null)
                {
                    stopwatch.StartedDateTime = DateTimeOffset.UtcNow;
                    await Clients.Group("Stopwatch:" + eventCode).UpdateStopwatch(stopwatch, DateTimeOffset.UtcNow);
                }
            }
        }

        public async Task RestartStopwatch(string eventCode, Guid id)
        {
            var eventStopwatches = stopwatches.GetOrAdd(eventCode, new ConcurrentDictionary<Guid, EventStopwatch>());
            if (eventStopwatches.TryGetValue(id, out var stopwatch))
            {
                stopwatch.StartedDateTime = DateTimeOffset.UtcNow;
                stopwatch.StoppedDateTime = null;
                stopwatch.LapsDateTime.Clear();
                await Clients.Group("Stopwatch:" + eventCode).UpdateStopwatch(stopwatch, DateTimeOffset.UtcNow);
            }
        }

        public async Task LapStopwatch(string eventCode, Guid id)
        {
            var eventStopwatches = stopwatches.GetOrAdd(eventCode, new ConcurrentDictionary<Guid, EventStopwatch>());
            if (eventStopwatches.TryGetValue(id, out var stopwatch)
                && stopwatch.StartedDateTime.HasValue && !stopwatch.StoppedDateTime.HasValue)
            {
                var dateTime = DateTimeOffset.UtcNow;
                stopwatch.LapsDateTime.Add(dateTime);

                var evt = await flightEventStorage.GetByStopwatchCodeAsync(eventCode);
                if (stopwatch.LapsDateTime.Count == evt.LeaderboardLaps.Count)
                {
                    stopwatch.StoppedDateTime = dateTime;
                }

                await Clients.Group("Stopwatch:" + eventCode).UpdateStopwatch(stopwatch, DateTimeOffset.UtcNow);
            }
        }

        public async Task StopStopwatch(string eventCode, Guid id)
        {
            var eventStopwatches = stopwatches.GetOrAdd(eventCode, new ConcurrentDictionary<Guid, EventStopwatch>());
            if (eventStopwatches.TryGetValue(id, out var stopwatch))
            {
                stopwatch.StoppedDateTime = DateTimeOffset.UtcNow;
                await Clients.Group("Stopwatch:" + eventCode).UpdateStopwatch(stopwatch, DateTimeOffset.UtcNow);
            }
        }

        public async Task RemoveStopwatch(string eventCode, Guid id)
        {
            var eventStopwatches = stopwatches.GetOrAdd(eventCode, new ConcurrentDictionary<Guid, EventStopwatch>());
            if (eventStopwatches.TryRemove(id, out var stopwatch))
            {
                await Clients.Group("Stopwatch:" + eventCode).RemoveStopwatch(stopwatch);
            }
        }

        public async Task SaveStopwatch(string eventCode, Guid id)
        {
            var eventStopwatches = stopwatches.GetOrAdd(eventCode, new ConcurrentDictionary<Guid, EventStopwatch>());
            if (eventStopwatches.TryRemove(id, out var stopwatch))
            {
                await Clients.Group("Stopwatch:" + eventCode).RemoveStopwatch(stopwatch);

                var evt = await flightEventStorage.GetByStopwatchCodeAsync(eventCode);
                // Create leaderboard for each lap and full race
                if (stopwatch.LapsDateTime.Count == evt.LeaderboardLaps.Count)
                {
                    var lapTime = stopwatch.LapsDateTime[^1] - stopwatch.StartedDateTime.Value;

                    var leaderboardRecord = new LeaderboardRecord
                    {
                        EventId = evt.Id,
                        LeaderboardName = stopwatch.LeaderboardName,
                        SubIndex = 0,
                        PlayerName = stopwatch.Name,
                        Score = -(long)lapTime.TotalMilliseconds,
                        ScoreDisplay = $"{lapTime.Hours:00}:{lapTime.Minutes:00}:{lapTime.Seconds:00}.{lapTime.Milliseconds:000}"
                    };

                    await leaderboardStorage.SaveAsync(leaderboardRecord);
                }

                for (var i = 0; i < stopwatch.LapsDateTime.Count; i++)
                {
                    var startTime = i == 0 ? stopwatch.StartedDateTime.Value : stopwatch.LapsDateTime[i - 1];
                    var lapTime = stopwatch.LapsDateTime[i] - startTime;

                    var leaderboardRecord = new LeaderboardRecord
                    {
                        EventId = evt.Id,
                        LeaderboardName = stopwatch.LeaderboardName,
                        SubIndex = i + 1,
                        PlayerName = stopwatch.Name,
                        Score = -(long)lapTime.TotalMilliseconds,
                        ScoreDisplay = $"{lapTime.Hours:00}:{lapTime.Minutes:00}:{lapTime.Seconds:00}.{lapTime.Milliseconds:000}"
                    };

                    await leaderboardStorage.SaveAsync(leaderboardRecord);
                }

                var records = await leaderboardStorage.LoadAsync(evt.Id);
                await Clients.Group("Stopwatch:" + eventCode).UpdateLeaderboard(records);
                await Clients.Group("Leaderboard:" + evt.Id).UpdateLeaderboard(records);
            }

        }

        #endregion

        public void RequestTeleport(string token, AircraftPosition position)
        {
            connectionIdToTeleportRequest[Context.ConnectionId] = (token, position);
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
    }

    public class EventStopwatch
    {
        public Guid Id { get; set; }
        public string EventCode { get; set; }
        public string LeaderboardName { get; set; }
        public string Name { get; set; }
        public DateTimeOffset AddedDateTime { get; set; }
        public DateTimeOffset? StartedDateTime { get; set; }
        public List<DateTimeOffset> LapsDateTime { get; set; } = new List<DateTimeOffset>();
        public DateTimeOffset? StoppedDateTime { get; set; }
    }
}
