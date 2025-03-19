using BCL.Discord.Extensions;
using BCL.Discord.Utils;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus.Entities;

using Microsoft.Extensions.DependencyInjection;

namespace BCL.Discord.Components.Dashboards;
public partial class QueueTracker
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public static class MembersField
    {
        private static readonly (string, int) MostActiveDefault = ("You could be #1", 0);
        private static IUserRepository _userRepository;

        public static (string, int) MostActive { get; private set; } = MostActiveDefault;
        public static Dictionary<string, Filter> QueueInfos { get; private set; } = [];

        private static string _content = "";

        public static void Setup(IServiceProvider services)
        {
            IServiceScope scope = services.CreateScope();
            _userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        }

        public static DiscordMessageBuilder CreateBuilder() =>
            new DiscordMessageBuilder().WithContent(Value);

        public static string Value => $"""
        ### Members
        {Wrap(_content)}
        """;

        private static string GetContent()
        {
            // Determine maximum column widths
            int filterFieldLength = Math.Max(QueueInfos.Max(q => q.Key.Length), 6); // Minimum length of "Filter"

            // Calculate the width of the table
            int rowWidth = filterFieldLength + 1 + // Filter field width + padding
                           7 + // Total column width
                           8 + // Active column width
                           6 + // Highest MMR column width
                           10 + // Highest Winrate column width
                           9;  // Played column width

            // Header padded to match the table width
            string header = $"{ANSIColors.Black}{ANSIColors.Background.Cyan} Most Active Player    {ANSIColors.White}{ANSIColors.Background.Black} ♔{MostActive.Item1}♔ ";

            // Correct padding based on visible length (ignoring ANSI escape sequences)
            int visibleLength = AnsiUtils.GetVisibleLength(header);
            if (visibleLength < rowWidth)
                header += new string(' ', rowWidth - visibleLength);

            // Table header
            string tableHeader = $"{ANSIColors.Reset}{ANSIColors.Background.Black} {ANSIColors.Underline}{ANSIColors.Background.Black}Filter{new string(' ', filterFieldLength - 6)}| Total| Active| ^MMR| ^Winrate| ^Played";

            // Generate rows
            _content = QueueInfos
                .Where(r => r.Value.Display)
                .Aggregate(
                    $"{header}\n{tableHeader}",
                    (current, filter) =>
                        $"{current}\n{ANSIColors.Reset}{ANSIColors.Background.Black} {ANSIColors.Underline}{filter.Value.Decorator}{filter.Key.FormatForDiscordCode(filterFieldLength)}| {filter.Value.Count.FormatForDiscordCode(5, true)}| {filter.Value.Active.FormatForDiscordCode(6, true)}| {filter.Value.HighestMmr.FormatForDiscordCode(4, true)}| {filter.Value.HighestWinrate.ToString("P").FormatForDiscordCode(8, true)}| {filter.Value.MostGamesPlayed.FormatForDiscordCode(7, true)}"
                )
                .Replace(" 0", "  "); // Replace "0" with spaces for cleaner display.

            return _content;
        }

        public static void Refresh()
        {
            MostActive = MostActiveDefault;
            QueueInfos = new Dictionary<string, Filter>
            {
                { "All", new Filter("All") },
                { "Pro", new Filter("Pro", ANSIColors.Green) },
                { "Standard", new Filter("Standard", ANSIColors.Green) },
                { "Vip", new Filter("Vip", ANSIColors.Magenta) }
            };

            foreach (Region region in Enum.GetValues<Region>())
                QueueInfos.Add(region.ToString()!, new Filter(region.ToString()!, ANSIColors.Yellow));

            QueueInfos["Unknown"].DoNotDisplay();

            // Process users
            IEnumerable<User> users = _userRepository.GetAll();
            foreach (User user in users)
            {
                if (!user.Approved) continue;

                bool active = (DateTime.UtcNow - user.LastPlayed) < TimeSpan.FromDays(7);
                int gamesPlayed = user.LatestSnapshot?.GamesPlayed ?? 0;

                if (active && gamesPlayed > MostActive.Item2)
                    MostActive = (user.InGameName, gamesPlayed);

                string server = user.Server.ToString();

                int gamesPlayed_Standard = user.LatestSnapshot?.GamesPlayed_Standard ?? 0;
                int gamesPlayed_Pro = user.LatestSnapshot?.GamesPlayed_Pro ?? 0;

                double rating_Standard =  0;
                double rating = 0;

                double winrate = 0;
                double winrate_Standard = 0;
                double winrate_Pro = 0;

                if (gamesPlayed_Standard > 10)
                {
                    rating_Standard = user.LatestSnapshot?.Rating_Standard ?? 0;
                    winrate_Standard = user.LatestSnapshot?.WinRate_Standard ?? 0;
                }

                if (gamesPlayed_Pro > 10)
                {
                    rating = !user.Pro ? rating_Standard
                        : Math.Max(rating_Standard, user.LatestSnapshot?.Rating ?? 0);
                    winrate_Pro = user.LatestSnapshot?.WinRate_Pro ?? 0;
                }

                if(gamesPlayed > 10)
                    winrate = user.LatestSnapshot?.WinRate ?? 0;

                UpdateFilter("All", active, gamesPlayed, rating, winrate);
                UpdateFilter("Standard", active, gamesPlayed_Standard, rating_Standard, winrate_Standard);
                UpdateFilter(server, active, gamesPlayed, rating, winrate);
                if (user.Pro) UpdateFilter("Pro", active, gamesPlayed_Pro, rating, winrate_Pro);
                if (user.Vip) UpdateFilter("Vip", active, gamesPlayed, rating, winrate);
            }

            _content = GetContent();
        }

        private static void UpdateFilter(string key, bool active, int gamesPlayed, double mmr, double winrate)
        {
            if (!QueueInfos.TryGetValue(key, out Filter? filter)) return;

            filter.Count++;
            if (active) filter.Active++;
            if (gamesPlayed > filter.MostGamesPlayed) filter.MostGamesPlayed = gamesPlayed;
            if (mmr > filter.HighestMmr) filter.HighestMmr = mmr;
            if (winrate > filter.HighestWinrate) filter.HighestWinrate = winrate;
        }

        public class Filter(string key, string decorator = "")
        {
            public int Active { get; set; }
            public int Count { get; set; }
            public string Decorator { get; set; } = $"{ANSIColors.White}{decorator}";

            public void DoNotDisplay() => _display = false;
            private bool _display = true;
            public bool Display => _display && Count > 0;
            public double HighestMmr { get; set; }
            public double HighestWinrate { get; set; }
            public string Key { get; set; } = key;
            public int MostGamesPlayed { get; set; }
        }
    }
}
