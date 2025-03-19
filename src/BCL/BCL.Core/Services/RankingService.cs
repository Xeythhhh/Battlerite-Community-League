using System.Diagnostics;

using BCL.Common.Extensions;
using BCL.Core.Services.Abstract;
using BCL.Domain.Entities.Matches;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;

namespace BCL.Core.Services;

public class RankingService(IUserRepository userRepository, ITeamRepository teamRepository) : IRankingService
{
    public async Task<double> UpdateElo(Match match)
    {
        double eloShift = 0d;
        if (match.Outcome is MatchOutcome.Canceled or MatchOutcome.InProgress) return eloShift;
        if (match.League == League.Premade3V3)
        {
            PremadeTeam team1 = (await teamRepository.GetByIdAsync(
                (await userRepository.GetByIdAsync(
                    match.Team1_PlayerInfos.First().Id))!.TeamId))!;

            PremadeTeam team2 = (await teamRepository.GetByIdAsync(
                (await userRepository.GetByIdAsync(
                    match.Team2_PlayerInfos.First().Id))!.TeamId))!;

            eloShift = GetEloShift(CoreConfig.Queue.RatingShift_Pro,
                team1.Rating,
                team2.Rating,
                match.Outcome);

            team1.Rating += eloShift;
            team2.Rating -= eloShift;
        }
        else
        {
            if (match.League != League.Pro && match.League != League.Standard) return eloShift;

            List<User> team1 = match.Team1_PlayerInfos
                .Select(p => p.Id)
                .Select(i => userRepository.GetById(i)!)
                .ToList();

            List<User> team2 = match.Team2_PlayerInfos
                .Select(p => p.Id)
                .Select(i => userRepository.GetById(i)!)
                .ToList();

            double team1Elo = match.League == League.Pro
                ? team1.Average(p => p.Rating)
                : team1.Average(p => p.Rating_Standard);

            double team2Elo = match.League == League.Pro
                ? team2.Average(p => p.Rating)
                : team2.Average(p => p.Rating_Standard);

            double eloK = match.League switch
            {
                League.Pro => CoreConfig.Queue.RatingShift_Pro,
                League.Standard => CoreConfig.Queue.RatingShift_Standard,
                _ => 0,
            };

            eloShift = GetEloShift(eloK, team1Elo, team2Elo, match.Outcome);

            team1.ForEach(p => UpdatePlayerElo(p, eloShift, match, team1));
            team2.ForEach(p => UpdatePlayerElo(p, -eloShift, match, team2));
        }
        await userRepository.SaveChangesAsync();
        return eloShift;
    }

    private static void UpdatePlayerElo(User user, double eloShift, Match match, List<User> team)
    {
        Match.Side side = match.GetSide(user);
        List<Match.EloModifier> playerEloModifiers = match.EloModifiers
            .Where(mod => mod.AppliesTo.In(side, Match.Side.Both))
            .ToList();

        if ((match.League is League.Pro && user.IsInPlacements)
            || (match.League is League.Standard && user.IsInPlacements_Standard))
        {
            playerEloModifiers.Add(new Match.EloModifier(
                "Placements",
                1.7,
                false,
                side));
        }

        if (user.Rating != 0) // avoid division by zero
        {
            playerEloModifiers.Add(new Match.EloModifier(
                "Normalized",
                match.Won(user)
                    ? team.Average(p => p.Rating) / user.Rating     // if you won
                    : user.Rating / team.Average(p => p.Rating), // if you lost
                false,
                side));
        }

        eloShift = Math.Round(playerEloModifiers
            .Where(modifier => modifier.Predicate(team, user) &&
                (!modifier.ApplyToLossesOnly || !match.Won(user)))
            .Aggregate(eloShift, (current, modifier) => current * modifier.Factor));

        switch (match.League)
        {
            case League.Pro:
                user.Rating += eloShift;
                break;

            case League.Standard:
                user.Rating_Standard += eloShift;
                break;

            case League.Event:
            case League.Tournament:
            case League.Custom:
            case League.Premade3V3:
                //player elo should not be affected in these leagues
                break;

            default:
                throw new UnreachableException();
        }

        match.PlayerEloShifts.Add((user.Id, eloShift));
    }

    /// <summary>
    /// Gets Exact win expectation for Team 1
    /// </summary>
    /// <param name="team1Elo"></param>
    /// <param name="team2Elo"></param>
    /// <returns></returns>
    private static double GetExactWinExpectation(double team1Elo, double team2Elo)
        => 1 / (1 + Math.Pow(10, (team2Elo - team1Elo) / 400.0));

    /// <summary>
    /// Gets Rounded win expectation for Team 1
    /// </summary>
    /// <param name="team1Elo"></param>
    /// <param name="team2Elo"></param>
    /// <returns></returns>
    // ReSharper disable once UnusedMember.Local
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable RCS1213 // Remove unused member declaration
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1822 // Mark members as static
    private double GetWinExpectation(double team1Elo, double team2Elo)
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore IDE0079 // Remove unnecessary suppression
#pragma warning restore RCS1213 // Remove unused member declaration
#pragma warning restore IDE0051 // Remove unused private members
        => Math.Round(GetExactWinExpectation(team1Elo, team2Elo), 2);

    /// <summary>
    /// Gets Elo Shift for a match result otherwise known as delta
    /// </summary>
    /// <param name="eloK">K parameter - double how much elo changes for a 50% winrate match</param>
    /// <param name="team1Elo"></param>
    /// <param name="team2Elo"></param>
    /// <param name="outcome"></param>
    /// <returns></returns>
    public static double GetEloShift(double eloK, double team1Elo, double team2Elo, MatchOutcome outcome)
        => (int)(eloK * (Math.Pow((int)outcome - 1, 2) - GetExactWinExpectation(team1Elo, team2Elo)));
}
