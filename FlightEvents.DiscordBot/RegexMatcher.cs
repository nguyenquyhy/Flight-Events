using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FlightEvents.DiscordBot
{
    public class RegexMatcher
    {
        public (T key, Match match)? Match<T>(Dictionary<T, Regex> regexes, string content)
        {
            foreach (var pair in regexes)
            {
                var match = pair.Value.Match(content);
                if (match.Success) return (pair.Key, match);
            }
            return null;
        }
    }
}
