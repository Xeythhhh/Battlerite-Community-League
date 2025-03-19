//using System.Diagnostics;
//using BCL.Core.Services.Args;
//using BCL.Domain;
//using BCL.Domain.Entities.Matches;
//using BCL.Domain.Enums;
//using BCL.Persistence.Sqlite.Repositories.Abstract;

//namespace BCL.Core.Services;
//public class BankService : IBankService
//{
//    public static event IBankService.GuildTransactionEvent? GuildTransaction;
//    protected virtual async Task OnGuildTransaction(GuildTransactionEventArgs args)
//        => await GuildTransaction?.Invoke(this, args)!;

//    private readonly IUserRepository _userRepository;

//    private static readonly Dictionary<Ulid, Dictionary<ulong, _Bet>> ActiveBets = new();

//    public BankService(
//        IUserRepository userRepository
//    )
//    {
//        _userRepository = userRepository;
//    }

//    public void Payout(Match match)
//    {
//        if (match.Outcome is MatchOutcome.Canceled or MatchOutcome.InProgress) return;

//        foreach (var user in match.PlayerInfos.Select(p => _userRepository.GetById(p.Id)!))
//        {
//            var side = match.GetSide(user);
//            var firstWin = false;
//            var win =
//                (side is Match.Side.Team1 && match.Outcome is MatchOutcome.Team1) ||
//                (side is Match.Side.Team2 && match.Outcome is MatchOutcome.Team2);

//            var amount = win ? DomainConfig.Currency.Win : DomainConfig.Currency.Loss;
//            if (user.FirstWinClaimed.Date != DateTime.UtcNow.Date && win)
//            {
//                amount += DomainConfig.Currency.FirstWin;
//                user.FirstWinClaimed = DateTime.UtcNow;
//                firstWin = true;
//            }

//            if (user.Vip) amount *= DomainConfig.Currency.VipMultiplier;
//            user.ModifyBalance(amount, $"Match Payout {match.Id} ({(win ? "Win" : "Loss")}){(firstWin ? " + First win of the day" : string.Empty)}");
//        }

//        _userRepository.SaveChanges();
//    }

//    public void Refund(Ulid matchId, ulong discordId)
//    {
//        var user = _userRepository.GetByDiscordId(discordId);
//        if (user is null) return;

//        if (!ActiveBets[matchId].TryGetValue(discordId, out var bet)) return;

//        ActiveBets[matchId].Remove(discordId);
//        user.UnFreeze(bet.Amount);
//        _userRepository.SaveChanges();
//    }

//    // ReSharper disable once InconsistentNaming
//    internal record _Bet(double Amount, MatchOutcome Outcome);
//    public BetResponse Bet(Ulid matchId, ulong discordId, MatchOutcome outcome)
//    {
//        if (!ActiveBets.ContainsKey(matchId)) ActiveBets.Add(matchId, new Dictionary<ulong, _Bet>());

//        var user = _userRepository.GetByDiscordId(discordId);
//        if (user is null) return new BetResponse(BetStatus.Declined, 0, "You need to be registered to use the bet system.");

//        if (user.AvailableBalance - user.BetAmount < 0) return new BetResponse(BetStatus.Declined, 0, "You can not afford to bet on this match.");

//        if (!ActiveBets[matchId].TryAdd(discordId, new _Bet(user.BetAmount, outcome)))
//            return new BetResponse(BetStatus.Declined, 0, "You already have a bet on this match."); //Should only happen if the system is used wrong by the developer

//        user.FreezeBalance(user.BetAmount);
//        _userRepository.SaveChanges();

//        return new BetResponse(BetStatus.Succesful, user.BetAmount);
//    }

//    public async Task PredictionPayout(Ulid matchId)
//    {
//        if (!ActiveBets.TryGetValue(matchId, out var bets)) return;

//        var match = MatchService.ActiveMatches.Single(m => m.Id == matchId);
//        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
//        switch (match.Outcome)
//        {
//            case MatchOutcome.InProgress: throw new UnreachableException();
//            case MatchOutcome.Canceled: return;
//        }

//        var users = bets.Keys.Select(k => _userRepository.GetByDiscordId(k)!);
//        var team1 = bets.Values.Where(b => b.Outcome is MatchOutcome.Team1).Sum(b => b.Amount);
//        var team2 = bets.Values.Where(b => b.Outcome is MatchOutcome.Team2).Sum(b => b.Amount);

//        if (team1 + team2 == 0) return;

//        foreach (var user in users)
//        {
//            var bet = bets[user.DiscordId];
//            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
//            double payout = match.Outcome switch
//            {
//                MatchOutcome.Team1 when bet.Outcome is MatchOutcome.Team1 => bet.Amount / team1 * team2,
//                MatchOutcome.Team2 when bet.Outcome is MatchOutcome.Team2 => bet.Amount / team2 * team1,
//                _ => -bet.Amount
//            };

//            user.UnFreeze(bet.Amount);
//            user.ModifyBalance(payout, $"Bet {bet.Amount} on {matchId}");

//            await OnGuildTransaction(new GuildTransactionEventArgs(user.LatestBalanceSnapshot, user.DiscordId));
//        }

//        ActiveBets.Remove(matchId);
//        await _userRepository.SaveChangesAsync();
//    }
//}

//public interface IBankService
//{
//    delegate Task GuildTransactionEvent(object sender, GuildTransactionEventArgs args);
//    void Payout(Match match);
//    void Refund(Ulid matchId, ulong discordId);
//    BetResponse Bet(Ulid matchId, ulong discordId, MatchOutcome matchOutcome);
//    Task PredictionPayout(Ulid matchId);
//}

//public class BetResponse
//{
//    public BetResponse(BetStatus status, double amount, string message = "")
//    {
//        Status = status;
//        Amount = amount;
//        Message = message;
//    }

//    public BetStatus Status { get; set; }
//    public string Message { get; set; }
//    public double Amount { get; set; }
//}

//public enum BetStatus
//{
//    Succesful,
//    Declined
//}
