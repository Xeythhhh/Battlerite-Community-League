using BCL.Core.Services;
using BCL.Discord.Attributes.Permissions;
using BCL.Discord.Bot;
using BCL.Discord.Components;

using DSharpPlus.SlashCommands;

#pragma warning disable CS4014

namespace BCL.Discord.Commands.SlashCommands.Admin;
public partial class AdminCommands
{
    [SlashCommand_Staff]
    [SlashCommandGroup("utils", "Utilities", false)]
    [SlashModuleLifespan(SlashModuleLifespan.Scoped)]
    public class Utilities(DiscordEngine discordEngine, MatchManager matchManager) : ApplicationCommandModule
    {
        [SlashCommand("RefreshOptionProviders", "Refreshes option providers, bot might become unresponsive for up to 5 minutes", false)]
        public async Task RefreshOptionProviders(InteractionContext context)
        {
            context.CreateResponseAsync("Refreshing Option Providers, this can take up to 5 minutes, stand by :).");
            discordEngine.Log($"RefreshOptionProviders triggered by {context.User.Mention}. This can take up to 5 minutes");
            discordEngine.SetupOptionProviders();
            await context.Client.GetSlashCommands().RefreshCommands();
        }

        //[SlashCommand("Dump", "Dumps all user info(use in a spam channel or dms ideally)")]
        //public async Task Dump(InteractionContext context)
        //{
        //    //todo Userinfo dump /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //    //
        //    //  populate these from all relevant users
        //    //  maybe /dump optional:<membership> or optional:<matchType> whatever we'll see, probably just pro for starters
        //    //
        //    //string[] inGameName
        //    //string[] discordName
        //    //string[] membership
        //    //double[] rating
        //    //double[] highestRating
        //    //int[] gamesPlayedPro
        //    //string[] winrate
        //    //
        //    //  whatever else staff needs
        //    //  generate text file
        //    //
        //    //await using var memoryStream = new MemoryStream();
        //    //await using TextWriter textWriter = new StreamWriter(memoryStream);
        //    //await textWriter.WriteAsync("blabla");
        //    //await textWriter.FlushAsync();
        //    //memoryStream.Position = 0;
        //    //var bytes = memoryStream.ToArray();
        //    //
        //    //  send file
        //    //todo end////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //    var users = _userRepository.GetAll().Where(u => u.CurrentSeasonStats.LatestSnapshot.GamesPlayed > 0).OrderByDescending(u => u.Rating);
        //    var content = users.Aggregate("Ign | Last known discord name | Membership | Rating | Games  played (pro) | Winrate (pro)\n", (current, u) =>
        //        current + $"{u.InGameName} ; {u.Name} ; {(u.Pro ? "Pro" : "Standard")} ;  {u.Rating} ; {u.CurrentSeasonStats.LatestSnapshot.GamesPlayed} ; {u.CurrentSeasonStats.LatestSnapshot.WinRate:0.00%} \n");
        //    var contentSlices = new List<string>();
        //    try
        //    {
        //        while (content.Length > 0)
        //        {
        //            if (content.Length >= 2000)
        //            {
        //                contentSlices.Add(content[..2000]);
        //                content = content.Remove(0, 2000);
        //            }
        //            else
        //            {
        //                contentSlices.Add(content);
        //                content = string.Empty;
        //            }
        //        }

        //        foreach (var message in contentSlices.Select(slice => new DiscordMessageBuilder()
        //                     .WithContent(slice)))
        //        {
        //            await message.SendAsync(context.Channel);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e);
        //    }
        //}

        [SlashCommand("PurgeActiveMatches", "Yes")]
        public async Task PurgeActiveMatches(InteractionContext context)
        {
            int matchCount = MatchService.ActiveMatches.Count;
            MatchService.ActiveMatches.Clear();
            int discordMatchCount = matchManager.ActiveMatches.Count;
            matchManager.ActiveMatches.Clear();
            string content = $"Removed all active matches ({matchCount}M / {discordMatchCount}DM)";
            await context.CreateResponseAsync(content);
            await discordEngine.Log(content);
        }

        [SlashCommand("CheckActiveMatches", "Yes")]
        public async Task CheckActiveMatches(InteractionContext context) =>
            await context.CreateResponseAsync($"{MatchService.ActiveMatches.Count}M / {matchManager.ActiveMatches.Count}DM");
    }
}
