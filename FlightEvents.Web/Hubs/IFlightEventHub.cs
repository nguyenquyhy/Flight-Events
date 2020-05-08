using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightEvents.Web.Hubs
{
    public interface IFlightEventHub
    {
        /// <summary>
        /// Send updated ATC status to the map
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="status"></param>
        Task UpdateATC(string connectionId, ATCStatus status, ATCInfo atc);

        /// <summary>
        /// Send updated aircraft status to the map
        /// </summary>
        Task UpdateAircraft(string connectionId, AircraftStatus status);

        /// <summary>
        /// Ask client to send back flight plan if the client has a particular callsign
        /// </summary>
        Task RequestFlightPlan(string connectionId, string callsign);

        /// <summary>
        /// Return the flight plan to ATC
        /// </summary>
        /// <param name="connectionId">Current Connection ID</param>
        Task ReturnFlightPlan(string connectionId, FlightPlanCompact flightPlan);

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
    }
}
