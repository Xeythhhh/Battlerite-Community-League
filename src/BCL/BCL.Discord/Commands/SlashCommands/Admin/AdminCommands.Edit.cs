using BCL.Discord.Attributes.Permissions;
using BCL.Discord.Commands.ModalCommands;
using BCL.Discord.OptionProviders;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.ModalCommands;
using DSharpPlus.SlashCommands;

namespace BCL.Discord.Commands.SlashCommands.Admin;
public partial class AdminCommands
{
    [SlashCommand_Staff]
    [SlashCommandGroup("Edit", "Edit a token.", false)]
    [SlashModuleLifespan(SlashModuleLifespan.Scoped)]
    public class Edit(IMapRepository mapRepository, IChampionRepository championRepository)
        : ApplicationCommandModule
    {
        [SlashCommand("Map", "Edit a map")]
        public async Task Map(InteractionContext context,
            [ChoiceProvider(typeof(MapChoiceProvider))]
            [Option("Map", "Map you want to edit")] string mapId)
        {
            Domain.Entities.Queue.Map? map = await mapRepository.GetByIdAsync(mapId);
            if (map == null) return;

            DiscordInteractionResponseBuilder mapModal = ModalBuilder
                .Create(nameof(AdminModals.EditMap), map.Id.ToString())
                .WithTitle($"Edit {map.Name}")
                .AddComponents(new TextInputComponent(
                    "Frequency", "mapFrequency", "The weight to be used by the random map generator",
                    map.Frequency.ToString(), max_length: 3))
                .AddComponents(new TextInputComponent(
                    "Enabled (0 / 1)", "mapEnabled", "Enable(1) or disable(0) the map",
                    map.Disabled ? "0" : "1", max_length: 1))
                .AddComponents(new TextInputComponent(
                    "Pro (0 / 1)", "pro", "Enable(1) or disable(0) the map in ProLeague only",
                    map.Pro ? "1" : "0", max_length: 1));

            await context.CreateResponseAsync(InteractionResponseType.Modal, mapModal);
        }

        [SlashCommand("Melee", "Edit a champion")]
        public async Task Melee(InteractionContext context,
            [ChoiceProvider(typeof(MeleeUnrestrictedChoiceProvider))]
            [Option("Champion", "Champion you want to edit")] string championId) => await EditChampion(context, championId);

        [SlashCommand("Ranged", "Edit a champion")]
        public async Task Ranged(InteractionContext context,
            [ChoiceProvider(typeof(RangedUnrestrictedChoiceProvider))]
            [Option("Champion", "Champion you want to edit")] string championId) => await EditChampion(context, championId);

        [SlashCommand("Support", "Edit a champion")]
        public async Task Support(InteractionContext context,
            [ChoiceProvider(typeof(SupportUnrestrictedChoiceProvider))]
            [Option("Champion", "Champion you want to edit")] string championId) => await EditChampion(context, championId);

        async Task EditChampion(BaseContext context, string championId)
        {
            Domain.Entities.Queue.Champion? champion = await championRepository.GetByIdAsync(championId);
            if (champion == null) return;

            DiscordInteractionResponseBuilder championModal = ModalBuilder
                .Create(nameof(AdminModals.EditChampion), championId)
                .WithTitle($"Edit {champion.Name}")
                .AddComponents(new TextInputComponent(
                    "Restrictions", "championRestrictions", "Champion's restricted assets (rites, skins, etc)",
                    champion.Restrictions, required: false, style: TextInputStyle.Paragraph, max_length: 200))
                .AddComponents(new TextInputComponent(
                    "Enabled (0 / 1)", "championEnabled", "Enable(1) or disable(0) the champion",
                    champion.Disabled ? "0" : "1")) //Don't ask me why this is inverted, I wouldn't be able to answer xD
                .AddComponents(new TextInputComponent(
                    "Standard Ban (0 / 1)", "championStandardBan", "Enable(1) or disable(0) the champion in Standard",
                    champion.StandardBanned ? "1" : "0"))
                .AddComponents(new TextInputComponent(
                    "Notes", "notes", "This field doesn't do anything",
                    "If you change the disabled state, you need to use /refreshChoiceProviders which can make the bot unresponsive by up to 5 minutes. Ignore this field",
required: false, style: TextInputStyle.Paragraph));

            await context.CreateResponseAsync(InteractionResponseType.Modal, championModal);
        }
    }
}
