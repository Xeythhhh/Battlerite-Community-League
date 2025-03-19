using BCL.Core.Services.Abstract;
using BCL.Core.Services.Args;
using BCL.Core.Services.Queue;
using BCL.Domain.Dtos;
using BCL.Domain.Entities.Matches;
using BCL.Domain.Entities.Queue;
using BCL.Domain.Entities.Users;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus.Entities;

namespace BCL.Core.Services;
public class MatchService(
    IMatchRepository matchRepository,
    IRankingService rankingService,
    IStatsService statsService,
    IUserRepository userRepository,
    IAnalyticsRepository analytics
        //IBankService bank
        ) : IMatchService
{
    //private readonly IBankService _bank;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public static event IMatchService.MatchStartedEvent? MatchStarted;
    public static event IMatchService.MatchFinishedEvent? MatchFinished;
    public static List<Match> ActiveMatches { get; set; }

    static MatchService()
    {
        ActiveMatches = [];
    }

    public void StartMatch(
        Team team1,
        Team team2,
        Match match,
        Map map,
        List<string> format,
        DraftType draftType = DraftType.Sequential)
    {
        match.Draft = new Draft
        {
            Captain1DiscordId = team1.Captain.DiscordId,
            Captain2DiscordId = team2.Captain.DiscordId,
            DraftType = draftType,
            Steps = GetDraftSteps(format),
            RemainingActions = CoreConfig.Draft.Sequential.ActionsAtStart
        };

        ActiveMatches.Add(match);

        _ = OnMatchStarted(new MatchStartedEventArgs(team1, team2, match, format, map));
    }

    public async Task<double> FinishMatch(string matchId)
        => await FinishMatch(Ulid.Parse(matchId));

    public async Task<double> FinishMatch(Ulid id)
    {
        Match match = ActiveMatches.FirstOrDefault(m => m.Id == id) ?? throw new Exception($"No active match with id '{id}'");

        ActiveMatches.Remove(match);
        if (QueueService.TestMode) return 0d;
        if (match.Team1_PlayerInfos.Concat(match.Team2_PlayerInfos).Any(p => p == null)) return 0d;

        double eloShift = await rankingService.UpdateElo(match);
        match.EloShift = eloShift;

        await _gate.WaitAsync(2000);
        await matchRepository.AddAsync(match);
        await matchRepository.SaveChangesAsync();
        await statsService.UpdateStats(match);
        //_bank.Payout(match);
        _gate.Release();
        _ = OnMatchFinished();
        return eloShift;
    }

    // ReSharper disable once InconsistentNaming
    public void UpdateDraftTimeAndLink(Ulid matchId, List<DraftTime> draftTimes, DiscordMessage discordMessage)
    {
        Match? match = matchRepository.GetById(matchId); if (match is null) return;

        match.JumpLink = discordMessage.JumpLink;

        List<User> users = match.DiscordUserIds.ToList()
            .Select(discordId => userRepository.GetByDiscordId(discordId)!)
            .DistinctBy(u => u.Id)
            .ToList();

        Domain.Entities.Analytics.RegionStats? regionStats = analytics.GetRegionStats(match.Region);
        List<TimeSpan> newRegionTimes = [];
        foreach (User? user in users)
        {
            SetLatestMatchLink(discordMessage, user, match);
            if (match.Draft.DraftType is DraftType.Simultaneous) continue;
            if (match.Outcome is MatchOutcome.Canceled) continue;

            List<DraftTime> userTimes = draftTimes.Where(t => t.DiscordId == user.DiscordId).ToList();
            if (userTimes.Count == 0) continue;
            if (userTimes.Count < match.Draft.Steps.Count)
            {
                int missingCount = match.Draft.Steps.Count - userTimes.Count;
                if (missingCount > 0)
                {
                    TimeSpan fillerDuration = user.CurrentSeasonStats.AverageDraftTime == TimeSpan.Zero
                        ? new TimeSpan(Convert.ToInt64(userTimes.Average(t => t.Duration.Ticks)))
                        : new TimeSpan(Convert.ToInt64(user.CurrentSeasonStats.AverageDraftTime.Ticks / match.Draft.Steps.Count));

                    for (int i = 0; i < missingCount; i++) userTimes.Add(new DraftTime(user.DiscordId, fillerDuration));
                }
            }

            TimeSpan draftTime = new(userTimes.Sum(x => x.Duration.Ticks));
            newRegionTimes.Add(draftTime);

            if (user.CurrentSeasonStats.LongestDraftTime < draftTime)
            {
                user.CurrentSeasonStats.LongestDraftTime = draftTime;
                user.CurrentSeasonStats.LongestDraftLink = discordMessage.JumpLink;
            }

            if (user.CurrentSeasonStats.ShortestDraftTime > draftTime)
            {
                user.CurrentSeasonStats.ShortestDraftTime = draftTime;
                user.CurrentSeasonStats.ShortestDraftLink = discordMessage.JumpLink;
            }

            List<TimeSpan> data = Enumerable.Repeat(user.CurrentSeasonStats.AverageDraftTime, user.CurrentSeasonStats.TimedDrafts).ToList();
            data.Add(draftTime);
            user.CurrentSeasonStats.AverageDraftTime = new TimeSpan(Convert.ToInt64(data.Average(t => t.Ticks)));
            user.CurrentSeasonStats.TimedDrafts++;

            if (regionStats is null) continue;

            if (regionStats.LongestTime < draftTime)
            {
                regionStats.LongestTime = draftTime;
                regionStats.LongestUser = user;
                regionStats.LongestLink = discordMessage.JumpLink;
            }

            // ReSharper disable once InvertIf
            if (regionStats.ShortestTime > draftTime)
            {
                regionStats.ShortestTime = draftTime;
                regionStats.ShortestUser = user;
                regionStats.ShortestLink = discordMessage.JumpLink;
            }
        }

        if (regionStats is not null)
        {
            List<TimeSpan> data = Enumerable.Repeat(regionStats.Average, regionStats.TimedDrafts).ToList();
            newRegionTimes.ForEach(data.Add);

            regionStats.Average = new TimeSpan(Convert.ToInt64(data.Average(t => t.Ticks)));
            regionStats.TimedDrafts += newRegionTimes.Count;
        }

        Domain.Entities.Analytics.MigrationInfo? migrationInfo = analytics.GetMigrationInfo();
        migrationInfo!.IncrementSeasonMatchCount();

        analytics.SaveChanges();
        userRepository.SaveChanges();
        matchRepository.SaveChanges();
    }

    private static void SetLatestMatchLink(DiscordMessage matchHistoryMessage, User user, Match match)
    {
        user.LatestMatchLink = matchHistoryMessage.JumpLink;
        user.LatestMatch_DiscordLink_ToolTip = match.Id.ToString();

        Match.Side side = match.GetSide(user);
        double eloShift = Math.Abs(match.EloShift);
        user.LatestMatch_DiscordLink_Label = $"{(match.Outcome is MatchOutcome.Canceled ? "~~" : string.Empty)}{match.Season} **{match.League}** " + side switch
        {
            Match.Side.Team1 when match.Outcome is MatchOutcome.Team1 => $"+{eloShift}",
            Match.Side.Team2 when match.Outcome is MatchOutcome.Team2 => $"+{eloShift}",
            Match.Side.Team1 when match.Outcome is MatchOutcome.Team2 => $"-{eloShift}",
            Match.Side.Team2 when match.Outcome is MatchOutcome.Team1 => $"-{eloShift}",
            _ => "Drop~~"
        };
    }

    /// <summary>
    /// Gets the draft steps using given format or default from config
    /// </summary>
    /// <param name="format"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static List<DraftStep> GetDraftSteps(List<string>? format = null)
    {
        List<DraftStep> draftSteps = (format ?? CoreConfig.Draft.ProFormat)
            .ConvertAll(ds => new DraftStep
            {
                Action = ds switch
                {
                    "P" => DraftAction.Pick,
                    "B" => DraftAction.Ban,
                    "MP" => DraftAction.MapPick,
                    "MB" => DraftAction.MapBan,
                    "R" => DraftAction.Reserve,
                    "GB" => DraftAction.GlobalBan,
                    _ => throw new ArgumentOutOfRangeException(nameof(ds), ds, null)
                },
            });

        draftSteps[^1].IsLastPick = true;
        return draftSteps;
    }

    protected virtual async Task OnMatchStarted(MatchStartedEventArgs args)
        => await MatchStarted?.Invoke(this, args)!;

    protected virtual async Task OnMatchFinished()
        => await MatchFinished?.Invoke()!;

    //this has to be static for attributes TODO: contribute DI for attributes in DSharpPlus
    public static bool IsInMatch(ulong discordUserId) => ActiveMatches.Any(m => m.DiscordUserIds.Any(id => id == discordUserId));
    public static bool IsInMatch(User user) => IsInMatch(user.DiscordId);

    public static ulong GetTeamChannelId(ulong discordUserId)
    {
        Match? match = ActiveMatches.FirstOrDefault(m => m.DiscordUserIds.Contains(discordUserId));
        Match.Side? side = match?.GetSide(discordUserId);

        return side switch
        {
            Match.Side.Team1 => match!.Team1ChannelId,
            Match.Side.Team2 => match!.Team2ChannelId,
            _ => 0,
        };
    }
}
