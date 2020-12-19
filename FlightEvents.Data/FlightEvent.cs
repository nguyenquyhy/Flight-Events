using System;
using System.Collections.Generic;

namespace FlightEvents.Data
{
    public enum FlightEventType
    {
        SightSeeing,
        ATC,
        Race
    }

    public class FlightEvent
    {
        // HACK: defaultValue in HotChocolate seems to assign wrong date when DateTimeOffset.MinValue is used
        // hence we introduce a different default
        public static readonly DateTimeOffset DefaultDateTimeOffset = new DateTimeOffset(1000, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public Guid Id { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public DateTimeOffset UpdatedDateTime { get; set; }

        public DateTimeOffset StartDateTime { get; set; }
        public DateTimeOffset? EndDateTime { get; set; }

        public FlightEventType? Type { get; set; }

        public string Name { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }

        public string Waypoints { get; set; }
        public string Route { get; set; }

        public List<string> Leaderboards { get; set; }
        /// <summary>
        /// Note: First/Last points are assumed to be start/finish respectively.
        /// </summary>
        public List<FlightEventCheckpoint> Checkpoints { get; set; }

        public List<string> FlightPlanIds { get; set; }

        public List<FlightEventChecklistItem> ChecklistItems { get; set; }

        public void UpdateTo(FlightEvent current)
        {
            if (StartDateTime > DefaultDateTimeOffset) current.StartDateTime = StartDateTime;
            if (EndDateTime != default) current.EndDateTime = EndDateTime;

            if (Type != default) current.Type = Type;

            if (Name != default) current.Name = Name;
            if (Description != default) current.Description = Description;
            if (Url != default) current.Url = Url;

            if (Waypoints != default) current.Waypoints = Waypoints;
            if (Route != default) current.Route = Route;

            if (Leaderboards != default) current.Leaderboards = Leaderboards;
            if (Checkpoints != default) current.Checkpoints = Checkpoints;

            if (FlightPlanIds != default) current.FlightPlanIds = FlightPlanIds;
            if (ChecklistItems != default) current.ChecklistItems = ChecklistItems;
        }
    }

    public class FlightEventCheckpoint
    {
        public string Waypoint { get; set; }
        public string Symbol { get; set; }
    }

    public class FlightEventChecklistItem
    {
        public FlightEventChecklistItemType Type { get; set; }
        public string SubType { get; set; }

        public string Title { get; set; }
        public long? DiscordServerId { get; set; }
        public long? DiscordChannelId { get; set; }
        public List<FlightEventChecklistItemLink> Links { get; set; }
    }

    public class FlightEventChecklistItemLink
    {
        public string Type { get; set; }
        public string Url { get; set; }
    }

    public enum FlightEventChecklistItemType
    {
        Client,
        Discord,
        MicrosoftSimulator
    }

    public static class FlightEventChecklistItemSubType
    {
        public const string ConnectToDiscord = "ConnectToDiscord";
    }
}
