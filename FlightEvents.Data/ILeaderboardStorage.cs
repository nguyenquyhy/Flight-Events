using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightEvents.Data
{
    public class LeaderboardRecord
    {
        public LeaderboardRecord()
        {

        }

        public LeaderboardRecord(EventStopwatch stopwatch, int leaderboardSubIndex, TimeSpan timeSinceLast, TimeSpan timeSinceStart)
        {
            EventId = stopwatch.EventId;
            LeaderboardName = stopwatch.LeaderboardName;
            SubIndex = leaderboardSubIndex;
            PlayerName = stopwatch.Name;
            Remarks = stopwatch.Remarks;
            Score = -(long)timeSinceLast.TotalMilliseconds;
            ScoreDisplay = $"{timeSinceLast.Hours:00}:{timeSinceLast.Minutes:00}:{timeSinceLast.Seconds:00}.{timeSinceLast.Milliseconds:000}";
            TimeSinceStart = (long)timeSinceStart.TotalMilliseconds;
            TimeSinceLast = (long)timeSinceLast.TotalMilliseconds;
        }

        public Guid EventId { get; set; }
        public string PlayerName { get; set; }
        public string Remarks { get; set; }
        public string LeaderboardName { get; set; }
        public int SubIndex { get; set; }
        public long Score { get; set; }
        public string ScoreDisplay { get; set; }
        public long TimeSinceStart { get; set; }
        public long TimeSinceLast { get; set; }
    }

    public interface ILeaderboardStorage
    {
        Task<List<LeaderboardRecord>> LoadAsync(Guid eventId);
        Task<LeaderboardRecord> SaveAsync(LeaderboardRecord record);
    }
}
