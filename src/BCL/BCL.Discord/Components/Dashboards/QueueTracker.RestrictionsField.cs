using System.Xml.Linq;

using BCL.Discord.Utils;
using BCL.Domain.Entities.Queue;
using BCL.Persistence.Sqlite.Repositories;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus.Entities;

using Microsoft.Extensions.DependencyInjection;

namespace BCL.Discord.Components.Dashboards;

public partial class QueueTracker
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public static class RestrictionsField
    {
        private static IChampionRepository _championRepository;

        public static void Setup(IServiceProvider services)
        {
            IServiceScope scope = services.CreateScope();
            _championRepository = scope.ServiceProvider.GetRequiredService<IChampionRepository>();
        }

        private static List<ChampionWithRestrictions> restricted = [];
        const string Header = "### Restrictions\n";
        const string BugHeader = "### Restrictions [BUG]\n";
        const string Disclaimer = $"\n{ANSIColors.Black}{ANSIColors.Background.Red} Note: {ANSIColors.Reset}{ANSIColors.Yellow}⚠ May be ignored if all players agree.";

        public static List<DiscordMessageBuilder> GetBuilders() => CreateBuilders();

        private record RestrictioDto(string Value, bool IsBug);
        private record ChampionWithRestrictions(Champion Champion, string FormattedName, List<RestrictioDto> Restrictions);
        public static void Refresh()
        {
            List<Champion> restrictedChampions = [.. _championRepository.GetRestricted()
                .OrderBy(c => c.Role)
                .ThenBy(c => c.Class)];

            int disclaimerVisibleLength = AnsiUtils.GetVisibleLength(Disclaimer) - 1;

            restricted = [];

            foreach (Champion? champion in restrictedChampions)
            {
                string championName = $"{ANSIColors.Black}{ANSIColors.Background.Cyan}{ANSIColors.Underline} {champion.Name}{new string(' ', 9 - champion.Name.Length)}";
                int championNameVisibleLength = AnsiUtils.GetVisibleLength(championName);

                if (champion.Disabled)
                    championName += $"\n{ANSIColors.Background.Black}{ANSIColors.Yellow} ⚠ {ANSIColors.Red}Disabled {ANSIColors.Yellow}⚠ ";

                if (championNameVisibleLength < disclaimerVisibleLength)
                    championName += $"{ANSIColors.White}{ANSIColors.Background.Black}{new string(' ', disclaimerVisibleLength - championNameVisibleLength)}";

                List<string> restrictions = [.. champion.Restrictions?.Split("\n")];
                List<string> bugRestrictions = [.. restrictions.Where(r => r.Contains("[BUG]"))];
                restrictions.RemoveAll(r => r.Contains("[BUG]"));

                if (restrictions.Count > 0 || bugRestrictions.Count > 0)
                    restricted.Add(new(champion, championName, [.. FormatRestrictions(restrictions), .. FormatRestrictions(bugRestrictions, true)]));
            }

            List<RestrictioDto> FormatRestrictions(List<string> input, bool isBug = false)
            {
                if (input is null || input.Count == 0) return [];

                return input.ConvertAll(s =>
                    {
                        List<string> values = [.. s.Split(";")];

                        if (values.Count == 1) values.Insert(0, "??");

                        string decorator = values.Count == 3
                            ? GetAnsiColor(values[2])
                            : ANSIColors.Red;

                        if (isBug) values[1] = values[1].Replace("[BUG]", "").Trim();

                        string restrictionContent = $"{ANSIColors.Background.Black} {ANSIColors.Red}> {decorator}{new string(' ', 6 - values[0].Length)}{values[0]} {ANSIColors.Reset}{ANSIColors.Background.Black}{decorator} {values[1]}";
                        int restrictioncontentVisibleLength = AnsiUtils.GetVisibleLength(restrictionContent);
                        if (restrictioncontentVisibleLength < disclaimerVisibleLength)
                            restrictionContent += new string(' ', disclaimerVisibleLength - restrictioncontentVisibleLength);

                        return new RestrictioDto(restrictionContent, isBug);
                    });
            }
        }

        private static List<DiscordMessageBuilder> CreateBuilders()
        {
            const int maxLength = 2000;
            List<DiscordMessageBuilder> builders = [];
            string content = Header;
            content += Wrap(restricted.Aggregate("", (current, dto) =>
            {
                string value = dto.Champion.Disabled
                    ? $"{dto.FormattedName}\n"
                    : $"{dto.FormattedName}\n{string.Join("\n", dto.Restrictions.Select(r => $"{r.Value}{(r.IsBug ? " [BUG]" : string.Empty)}"))}\n";

                return current + value;
            })[..^1] + Disclaimer);

            if (content.Length + Disclaimer.Length > maxLength)
            {
                content = Header;
                content += Wrap(restricted.Where(c => c.Restrictions.Any(r => !r.IsBug)).Aggregate("", (current, dto) =>
                {
                    string value = dto.Champion.Disabled
                        ? $"{dto.FormattedName}\n"
                        : $"{dto.FormattedName}\n{string.Join("\n", dto.Restrictions.Where(r => !r.IsBug).Select(r => r.Value))}\n";

                    return current + value;
                })[..^1] + Disclaimer);

                string bugContent = BugHeader ?? Header;
                bugContent += Wrap(restricted.Where(c => c.Restrictions.Any(r => r.IsBug)).Aggregate("", (current, dto) =>
                {
                    string value = dto.Champion.Disabled
                        ? $"{dto.FormattedName}\n"
                        : $"{dto.FormattedName}\n{string.Join("\n", dto.Restrictions.Where(r => r.IsBug).Select(r => r.Value))}\n";

                    return current + value;
                })[..^1] + Disclaimer);

                return [
                    new DiscordMessageBuilder().WithContent(content),
                        new DiscordMessageBuilder().WithContent(bugContent)
                ];
            }
            else
            {
                return [
                    new DiscordMessageBuilder().WithContent(content)
                ];
            }
        }
    }
}
