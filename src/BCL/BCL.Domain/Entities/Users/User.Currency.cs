using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace BCL.Domain.Entities.Users;
public partial class User
{
    public DateTime FirstWinClaimed { get; set; }
    public DateTime DailyBonusClaimed { get; set; }
    public DateTime LastTransactionDate { get; set; }

    public double Balance { get; private set; } = DomainConfig.Currency.RegistrationBonus;
    public string BalanceHistory { get; private set; } = $"{DateTime.UtcNow}|{DomainConfig.Currency.RegistrationBonus}|{DomainConfig.Currency.RegistrationBonus}|Registered|{DomainConfig.Season}";
    public record BalanceSnapshot(DateTime CreatedAt, double Balance, double Amount, string Info, string Season);
    [NotMapped]
    public IEnumerable<BalanceSnapshot> BalanceSnapshots => BalanceHistory.Split(';').Select(s =>
    {
        string[] values = s.Split('|');
        DateTime createdAt = DateTime.Parse(values[0]);
        double balance = double.Parse(values[1]);
        double amount = double.Parse(values[2]);
        string info = values[3];
        string? season = values.ElementAtOrDefault(4);

        if (season is not null) return new BalanceSnapshot(createdAt, balance, amount, info, season);

        BalanceHistory = BalanceHistory.Replace(s, $"{s}|{DomainConfig.Season}");
        return new BalanceSnapshot(createdAt, balance, amount, info, DomainConfig.Season);
    });
    [NotMapped] public BalanceSnapshot LatestBalanceSnapshot => BalanceSnapshots.Last();

    public double ModifyBalance(double amount, string info)
    {
        info = info.Replace("|", "/").Replace(";", "#"); //reserved characters

        Balance += amount;
        BalanceHistory += $";{DateTime.UtcNow}|{Balance}|{amount}|{info}|{DomainConfig.Season}";
        return Balance;
    }

    public void InitBalance([CallerMemberName] string memberName = "")
    {
        if (!memberName.Contains("Migrate", StringComparison.CurrentCultureIgnoreCase)) return; //only called by the bot version migrator

        Balance = DomainConfig.Currency.RegistrationBonus;
        BalanceHistory = $"{DateTime.UtcNow}|{Balance}|{DomainConfig.Currency.RegistrationBonus}|Registered|{DomainConfig.Season}";
        BalanceSentToday = 0;
        TransactionsToday = 0;
        LastTransactionDate = DateTime.MinValue;
    }

    public double BetAmount { get; set; } = DomainConfig.Currency.DefaultBetAmount;
    public double BalanceSentToday { get; set; }
    public int TransactionsToday { get; set; }

    [NotMapped] public double AvailableBalance => Balance - Frozen;
    public double Frozen { get; set; }
    public void FreezeBalance(double amount)
    {
        Frozen += amount;
        Balance -= amount;
    }

    public void UnFreeze(double amount)
    {
        if (Frozen - amount < 0) throw new InvalidOperationException($"Can not unfreeze amount. Requested: {amount} | Available: {Frozen}");

        Frozen -= amount;
        Balance += amount;
    }
}
