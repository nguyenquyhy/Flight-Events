using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightEvents.Data
{
    public class LeaderboardRecord
    {
        public Guid EventId { get; set; }
        public string PlayerName { get; set; }
        public string LeaderboardName { get; set; }
        public int SubIndex { get; set; }
        public long Score { get; set; }
        public string ScoreDisplay { get; set; }
    }

    public interface ILeaderboardStorage
    {
        Task<List<LeaderboardRecord>> LoadAsync(Guid eventId);
        Task<LeaderboardRecord> SaveAsync(LeaderboardRecord record);
    }
}
