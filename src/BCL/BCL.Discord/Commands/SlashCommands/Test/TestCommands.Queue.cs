using BCL.Core.Services.Abstract;
using BCL.Discord.Commands.ModalCommands;
using BCL.Domain;
using BCL.Domain.Entities.Abstract;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace BCL.Discord.Commands.SlashCommands.Test;

public partial class TestCommands
{
    [SlashCommand("fill", "Queue up some fake users")]
    public async Task Fill(InteractionContext context,
        [Option("count", "number of test users to queue")] Int64 count = 6,
        [Option("pro", "yes or no")] bool pro = true,
        [Option("standardLeague", "standardLeague")] bool standardLeague = true,
        [Option("proLeague", "proLeague")] bool proLeague = true,
        [Option("eu", "eu")] bool eu = true,
        [Option("na", "na")] bool na = true,
        [Option("crossRegion", "crossRegion")] bool crossRegion = false,
        [Option("server", "server")] Region server = Region.Eu,
        [Option("testName", "Append the test name to your fake users.")] string? testName = null)
        => await _Fill(context.Interaction,
            count, pro, standardLeague, proLeague, eu, na, crossRegion, server,
            userRepository, championRepository, queueService, context.Client,
            testName);

    public static async Task _Fill(
        DiscordInteraction interaction,
        Int64 count,
        bool pro, bool standardLeague, bool proLeague, bool eu, bool na, bool crossRegion,
        Region server,
        IUserRepository userRepository, IChampionRepository championRepository, IQueueService queueService,
        DiscordClient client,
        string? testName)
    {
        await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            .WithContent($"Attempting to create {count} test users..."));

        if (!DiscordConfig.IsTestBot) { await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Unavailable in production :)")); return; }

        List<Domain.Entities.Queue.Champion> champions = championRepository.GetAll().ToList();

        List<Domain.Entities.Queue.Champion> melee = champions.Where(c => c is { Class: ChampionClass.Melee, Role: ChampionRole.Dps }).ToList();
        List<Domain.Entities.Queue.Champion> ranged = champions.Where(c => c is { Class: ChampionClass.Ranged, Role: ChampionRole.Dps }).ToList();
        List<Domain.Entities.Queue.Champion> support = champions.Where(c => c.Role == ChampionRole.Healer).ToList();

        List<User> testUsers = userRepository.Get(u => u.IsTestUser).ToList();
        testUsers.ForEach(u =>
        {
            Thread.Sleep(100);
            queueService.Leave(u.DiscordId);
            userRepository.Delete(u.Id);
        });
        testUsers.Clear();

        for (int i = 0; i < count; i++)
        {
            (string? m, string? r, string? s) = UserModals.ValidateChampionPool(
                melee.Aggregate("", (current, champion) => Whatever(champion, current)),
                ranged.Aggregate("", (current, champion) => Whatever(champion, current)),
                support.Aggregate("", (current, champion) => Whatever(champion, current)),
                championRepository);

            string something = Ulid.NewUlid().ToString()![^4..];
            Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<User> fakeUser = await userRepository.AddAsync(new User
            {
                Name = $"{testName ?? "TestUser"}_{something}",
                InGameName = $"{testName ?? "TestUser"}_{something}",
                DiscordId = client.CurrentUser.Id,
                Rating = Random.Shared.Next(50, 800),
                Rating_Standard = Random.Shared.Next(50, 800),
                DefaultMelee = string.IsNullOrWhiteSpace(m) ? "None" : m,
                DefaultRanged = string.IsNullOrWhiteSpace(r) ? "None" : r,
                DefaultSupport = string.IsNullOrWhiteSpace(s) ? "None" : s,
                IsTestUser = true,
                PlacementGamesRemaining = 10,
                PlacementGamesRemaining_Standard = 10,
                Server = Region.Eu,
                SeasonStats =
                [
                    new()
                    {
                        Season = DomainConfig.Season,
                        Snapshots =
                        [
                            new()
                        ]
                    }
                ]
            });
            testUsers.Add(fakeUser.Entity);
        }

        testUsers.ForEach(u =>
        {
            u.Pro = pro;
            u.ProQueue = pro || proLeague;
            u.StandardQueue = standardLeague;
            u.Eu = eu;
            u.Na = na;
            u.CrossRegion = crossRegion;
            u.Server = server;
        });

        await userRepository.SaveChangesAsync();

        foreach (User? user in testUsers)
        {
            await Task.Delay(200);
            queueService.Join(user, (QueueRole)Random.Shared.Next(0, 3));
        }

        await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"Added {count} test users to the queue."));
        return;

        static string Whatever(TokenEntity champion, string current)
        {
            string value = string.Empty;
            if (Random.Shared.Next() % 5 == 0) value = $"\n{champion.Name}";

            return $"{current}{value}";
        }
    }
}
