using BCL.Discord.Extensions;
using BCL.Discord.Utils;
using BCL.Domain;
using BCL.Domain.Entities.Matches;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus.Entities;

using Microsoft.Extensions.DependencyInjection;

namespace BCL.Discord.Components.Dashboards;

public partial class QueueTracker
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public static class MatchesField
    {
        private static IMatchRepository _matchRepository;
        private static IServiceProvider _services;

        public static List<Filter> QueueInfos { get; private set; } = [];
        public static (Region, int) MostActiveRegion { get; private set; } = (Region.Unknown, 0);
        public static int FieldCount => QueueInfos.Count(q => q.DisplayOnQueueTracker);

        private static string _content = "";

        public static void Setup(IServiceProvider services)
        {
            _services = services;
            IServiceScope scope = _services.CreateScope();
            _matchRepository = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        }
        public static void Refresh()
        {
            IEnumerable<Match> matches = _matchRepository.GetAllCompleted();

            QueueInfos = ProcessMatches(matches);

            MostActiveRegion = QueueInfos
                .Where(q => q.FilteredBy == Filter.FilterType.Region)
                .OrderByDescending(q => q.ThisWeek)
                .Select(q => (Enum.Parse<Region>(q.Key), q.ThisWeek))
                .FirstOrDefault((Region.Unknown, 0));

            _content = GetContent();
        }

        private static List<Filter> ProcessMatches(IEnumerable<Match> matches)
        {
            List<Filter> queueInfos = [];

            IEnumerable<IGrouping<(string, Filter.FilterType, string), Match>> groupedQueues = matches
                .GroupBy(m => (m.Region.ToString(), Filter.FilterType.Region, ANSIColors.Yellow))
                .Concat(matches.GroupBy(m => (m.League.ToString(), Filter.FilterType.League, ANSIColors.Green)));

            // use DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek); for Sunday as start of the Week
            DateTime startOfWeek = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek + (int)DayOfWeek.Monday);

            foreach (IGrouping<(string filter, Filter.FilterType filteredBy, string color), Match>? queue in groupedQueues)
            {
                Filter queueInfo = new(queue.Key.filter != "Custom")
                {
                    Key = queue.Key.filter,
                    FilteredBy = queue.Key.filteredBy,
                    Decorator = queue.Key.color,
                    Total = queue.Count(),
                    Today = queue.Count(m => m.RecordedAt.Date == DateTime.UtcNow.Date),
                    Yesterday = queue.Count(m => m.RecordedAt.Date == DateTime.UtcNow.Date.AddDays(-1)),
                    ThisWeek = queue.Count(m => m.RecordedAt.Date >= startOfWeek),
                    ThisSeason = queue.Count(m => m.Season == DomainConfig.Season)
                };

                queueInfos.Add(queueInfo);
            }

            // Aggregate total stats
            Filter total = new()
            {
                Key = "All",
                FilteredBy = Filter.FilterType.All,
                Decorator = ANSIColors.White,
                Today = queueInfos.Sum(q => q.Today) / 2,
                ThisSeason = queueInfos.Sum(q => q.ThisSeason) / 2,
                Total = queueInfos.Sum(q => q.Total) / 2
            };
            if (total.Total == 0) total.Total = -1; // Edge case for fresh installs

            queueInfos.Add(total);
            return queueInfos;
        }

        private static string GetContent()
        {
            if (QueueInfos.Count == 0) return "**No Matches Available**";

            int todayFieldLength = Math.Max(QueueInfos.Max(q => q.Key.Length), 5);

            // Calculate the total width of the table row
            int rowWidth = todayFieldLength + 4 + // Today field width + 3 padding + 1 separator
                           11 + // 10 Yesterday field width + 1 separator
                           6 +  // 5 Week field width + 1 separator
                           8 +  // 7 Season field width + 1 separator
                           7;   // 7 Total field width

            // Generate the first line and pad it to match the table width
            string header = $"{ANSIColors.Black}{ANSIColors.Background.Cyan} Most Active Region {ANSIColors.White}{ANSIColors.Background.Black} {MostActiveRegion.Item1}".TrimEnd();

            // Correct padding based on visible length (ignoring ANSI escape sequences)
            int visibleLength = AnsiUtils.GetVisibleLength(header);
            if (visibleLength < rowWidth)
                header += new string(' ', rowWidth - visibleLength);

            string tableHeader = $"{ANSIColors.Reset}{ANSIColors.Background.Black} {ANSIColors.Underline}Today{new string(' ', todayFieldLength - 2)}| Yesterday| Week| Season| Total";

            // Generate rows
            string content = QueueInfos
                .Where(q => q.DisplayOnQueueTracker)
                .Aggregate(
                    $"{header}\n{tableHeader}",
                    (current, queue) =>
                        $"{current}\n{ANSIColors.Reset}{ANSIColors.Background.Black} {ANSIColors.Underline}{queue.Decorator}{queue.Key.FormatForDiscordCode(todayFieldLength)}|{queue.Today.FormatForDiscordCode(2, true)}| {queue.Yesterday.FormatForDiscordCode(9, true)}| {queue.ThisWeek.FormatForDiscordCode(4, true)}| {queue.ThisSeason.FormatForDiscordCode(6, true)}|{queue.Total.FormatForDiscordCode(6, true)}")
                .Replace(" 0", "  "); // Replace "0" with spaces for cleaner display.

            return content;
        }

        public static DiscordMessageBuilder CreateBuilder() =>
            new DiscordMessageBuilder().WithContent(Value);

        private static string Value => $"""
        ### Matches ⇒ <#{DiscordConfig.Channels.MatchHistoryId}>
        {Wrap(_content)}
        """;

        public class Filter(bool display = true)
        {
            public enum FilterType { Region, League, Season, Activity, All }

            public string Decorator { get; set; } = ANSIColors.White;
            public bool DisplayOnQueueTracker => display && Total != 0;
            public FilterType FilteredBy { get; set; }
            public string Key { get; set; } = "Not Initialized";
            public int ThisSeason { get; set; }
            public int Today { get; set; }
            public int Total { get; set; }
            public int Yesterday { get; set; }
            public int ThisWeek { get; set; }
        }
    }
}
