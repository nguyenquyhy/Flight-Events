using FlightEvents.Data;
using FlightEvents.Web.Logics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightEvents.Web.Hubs
{
    public interface IFlightEventHub
    {
        /// <summary>
        /// Send updated ATC status to the map
        /// </summary>
        Task UpdateATC(string clientId, ATCStatus status, ATCInfo atc);

        /// <summary>
        /// Send updated aircraft status to the map
        /// </summary>
        Task UpdateAircraft(string clientId, AircraftStatus status);

        /// <summary>
        /// Send updated aircraft status to the bot
        /// </summary>
        Task UpdateAircraftToDiscord(ulong discordUserId, string clientId, AircraftStatus status);

        /// <summary>
        /// Add flight plan to the storage for requesting later
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="callsign"></param>
        /// <param name="flightPlan"></param>
        /// <returns></returns>
        Task AddFlightPlan(string clientId, string callsign, string source, FlightPlanCompact flightPlan);

        /// <summary>
        /// Ask client to send back flight plan if the client has a particular callsign
        /// </summary>
        Task RequestFlightPlan(string connectionId, string callsign);

        /// <summary>
        /// Return the flight plan to ATC
        /// </summary>
        /// <param name="clientId">Current Client ID</param>
        Task ReturnFlightPlan(string clientId, FlightPlanCompact flightPlan);

        /// <summary>
        /// Ask client to send back full flight plan info
        /// </summary>
        /// <param name="connectionId"></param>
        Task RequestFlightPlanDetails(string connectionId);

        /// <summary>
        /// Return the full flight plan info to the map
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="flightPlan"></param>
        Task ReturnFlightPlanDetails(string connectionId, FlightPlanData flightPlan);

        Task RequestFlightRoute(string webConnectionId);

        Task ReturnFlightRoute(string webConnectionId, List<AircraftStatusBrief> route);

        /// <summary>
        /// Send a message to client
        /// </summary>
        /// <param name="fromCallsign"></param>
        /// <param name="toCallsign"></param>
        /// <param name="message"></param>
        Task SendMessage(string fromCallsign, string toCallsign, string message);

        /// <summary>
        /// Signal Bot of a frequency change
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="fromFrequency"></param>
        /// <param name="toFrequency"></param>
        Task ChangeFrequency(string clientId, int? fromFrequency, int? toFrequency);

        Task SendATC(string to, string message);

        Task ChangeUpdateRateByCallsign(string callsign, int hz);

        Task NotifyUpdateRateChanged(string clientId, int hz);

        /// <summary>
        /// Instruct client to teleport the aircraft to a certain position
        /// </summary>
        /// <param name="connectionId">Connection ID of the requesting map</param>
        /// <param name="position"></param>
        Task Teleport(string connectionId, AircraftPosition position);

        Task UpdateStopwatch(EventStopwatch stopwatch, DateTimeOffset serverTime);
        Task RemoveStopwatch(EventStopwatch stopwatch);
        Task UpdateLeaderboard(List<LeaderboardRecord> leaderboardRecords);

        Task UpdateRaceResult(IEnumerable<Racer> racers);
    }
}
