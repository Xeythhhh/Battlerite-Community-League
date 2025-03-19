using BCL.Common.Extensions;
using BCL.Discord.Bot;
using BCL.Discord.Components.Dashboards;
using BCL.Domain.Entities.Queue;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus.ModalCommands;
using DSharpPlus.ModalCommands.Attributes;

using Newtonsoft.Json;

namespace BCL.Discord.Commands.ModalCommands;
public class AdminModals(
    IMapRepository mapRepository,
    IChampionRepository championRepository,
    DiscordEngine discordEngine) : ModalCommandModule
{
    [ModalCommand("EditMap")]
    public async Task EditMap(
        ModalContext context,
        string id,
        int frequency,
        bool enabled,
        bool pro)
    {
        Map? map = await mapRepository.GetByIdAsync(id);
        if (map is null) return;

        string oldJson = JsonConvert.SerializeObject(map, Formatting.Indented);
        map.Frequency = frequency;
        map.Disabled = !enabled;
        map.Pro = pro;
        string newJson = JsonConvert.SerializeObject(map, Formatting.Indented);

        await mapRepository.SaveChangesAsync();

        string content = $"""
                       **Map** `{map.Name}` with id `{id}` updated by {context.User.Mention}
                       ```diff
                       {oldJson.Diff(newJson)}
                       ```
                       """;

        await context.CreateResponseAsync($"{map.Name} updated.");
        await discordEngine.Log(content);
    }

    [ModalCommand("EditChampion")]
    public async Task EditChampion(
        ModalContext context,
        string id,
        string restrictions,
        bool enabled,
        bool stdBan,
        string _)
    {
        Champion? champion = await championRepository.GetByIdAsync(id);
        if (champion is null) return;

        bool updatedRestrictions = restrictions.Equals(champion.Restrictions, StringComparison.CurrentCultureIgnoreCase);

        string oldJson = JsonConvert.SerializeObject(champion, Formatting.Indented);
        champion.Restrictions = restrictions;
        champion.Disabled = !enabled;
        champion.StandardBanned = stdBan;
        string newJson = JsonConvert.SerializeObject(champion, Formatting.Indented);

        await championRepository.SaveChangesAsync();

        string content = $"""
                       **Champion** `{champion.Name}` with id `{id}` updated by {context.User.Mention}
                       ```diff
                       {oldJson.Diff(newJson)}
                       ```
                       """;

        await context.CreateResponseAsync($"{champion.Name} updated");
        await discordEngine.Log(content);

        if (!updatedRestrictions) return;

        await discordEngine.QueueTracker.Refresh(target: QueueTracker.QueueTrackerField.Restrictions);
    }
}
