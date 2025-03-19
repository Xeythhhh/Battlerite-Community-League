//using BCL.Common.Extensions;
//using BCL.Core;
//using BCL.Discord.Bot;
//using BCL.Discord.Extensions;
//using BCL.Domain;
//using BCL.Domain.Entities.Users;
//using BCL.Domain.Enums;
//using DSharpPlus.Entities;
//using DSharpPlus.SlashCommands;
//using Newtonsoft.Json;
//using QuickChart;

//#pragma warning disable CS4014

//namespace BCL.Discord.Commands.SlashCommands.Users;
//public partial class UserCommands
//{
//    private async Task _Wallet(BaseContext context, User user, DiscordUser discordUser, string[] seasons)
//    {
//        await context.FollowUpAsync(new DiscordFollowupMessageBuilder()
//            .WithContent("Generating **wallet** embed..."));

//        if (context.User.Id != user.DiscordId &&
//            context.Member.Roles.All(r => r.Id != DiscordConfig.Roles.StaffId))
//        {
//            await context.EditResponseAsync(new DiscordWebhookBuilder()
//                .WithContent("This information is private"));
//            return;
//        }

//        await context.EditResponseAsync(new DiscordWebhookBuilder()
//            .AddEmbed(await GetWalletEmbedAsync(discordUser, user, seasons, _discordEngine))
//            .AddComponents(BuildFilterSelectComponent(user.Id, context.User.Id, context.Guild.Emojis, seasons, 
//                SelectOption.GetOptions(user),
//                SelectFor.Wallet)));
//    }

//    public static async Task<DiscordEmbedBuilder> GetWalletEmbedAsync(DiscordUser discordUser, User user, string[] seasons, DiscordEngine discordEngine)
//    {
//        var rawSnapshots = user.BalanceSnapshots.Where(s => seasons.Any(ss => ss == s.Season))
//            .OrderBy(s => s.CreatedAt)
//            .ToList();
//        var snapshots = rawSnapshots.Count < 250 ? rawSnapshots
//            : CompressSnapshots(rawSnapshots).ToList();

//        #region Chart

//        var chart = new Chart
//        {
//            Width = 1000,
//            BackgroundColor = $"rgba(0,0,0, {user.ChartAlpha})",
//            Config = $$"""
//                       {
//                           type: 'line',
//                           data: {
//                               labels: [{{string.Join(", ", snapshots.Select(_ => "'⠀'"))}}],
//                               datasets: [{
//                                   label: 'Balance',
//                                   steppedLine: true,
//                                   pointRadius: 0,
//                                   borderDash: [5, 5],
//                                   borderColor: getGradientFillHelper('horizontal', [ '{{user.Chart_SecondaryRatingColor}}', '{{user.Chart_MainRatingColor}}' ]),
//                                   backgroundColor: getGradientFillHelper('vertical', [ `{{user.Chart_MainWinrateColor}}`,  `{{user.Chart_MainWinrateColor}}`,  'rgba(45,45,45,0.1)' ]),
//                                   data: [{{string.Join(", ", snapshots.Select(s => $"'{s.Balance}'"))}}],
//                               },],
//                           },
//                           options: {
//                               plugins: {
//                                   datalabels: {
//                                       align: 'top',
//                                       position: 'auto',
//                                       backgroundColor: 'rbga(0,0,0,0.4)',
//                                       color: 'rbga(255,255,255,1)',
//                                       formatter: (value) => {
//                                           return `$ ${parseFloat(value).toFixed(2)}`;
//                                       },
//                                       display: function(context){
//                                           return ((context.dataIndex - 5) % 25 == 0) ? 'auto' : false;
//                                       },
//                                   },
//                               },
//                               responsive: true,
//                               legend: { display: false },
//                               title: { display: true, text: "Wallet - {{JsonConvert.ToString(user.InGameName).Trim('"')}} - Bot Version: {{CoreConfig.Version}} | {{DateTime.UtcNow:yyyy MMMM dd}}{{(rawSnapshots.Count != snapshots.Count ? " | Interpolated" : string.Empty)}}", },
//                           	scales: {
//                       	        xAxes:[{ display: false }],
//                       	        yAxes:[
//                                       {
//                       	                position: 'left',
//                       	                scaleLabel: { display: true }
//                                       },
//                                       {
//                       	                position: 'right',
//                       	                scaleLabel: { display: true }
//                                       },
//                                   ],
//                               },
//                           },
//                       }
//                       """
//        };

//        var filename = $"Chart_Wallet_{user.DiscordId}_{DateTime.UtcNow}.png";
//        DiscordMessage? filehostMessage = null;
//        try
//        {
//            var chartBytes = chart.ToByteArray();

//            await using var memoryStream = new MemoryStream(chartBytes);
//            filehostMessage = await discordEngine.AttachmentsChannel.SendMessageAsync(new DiscordMessageBuilder()
//                .AddFile(filename, memoryStream, true));

//            await memoryStream.DisposeAsync();
//        }
//        catch (Exception e)
//        {
//            discordEngine.Log(e, "Generating Wallet Chart");
//        }

//        #endregion

//        var dailyTransactionCount = (user.Vip ? int.MaxValue : DomainConfig.Currency.DailyLimitCount) - user.TransactionsToday;
//        var dailyTransactionAmount = (user.Vip ? double.MaxValue % 10000000 : DomainConfig.Currency.DailyLimitAmount) - user.BalanceSentToday;

//        var last = user.BalanceSnapshots.Last();
//        var lastTransactionDecorator = last.Amount switch
//        {
//            < 0 => "- ",
//            > 0 => "+ ",
//            _ => string.Empty
//        };

//        var description = $"""
//                           {user.Mention} {(user.RoleId is 0 ? string.Empty : user.RoleMention)}
//                           Transaction limits reset in {DateTime.Today.AddDays(1).DiscordTime(DiscordTimeFlag.R)}
//                           Daily bonus: {(user.DailyBonusClaimed.Date == DateTime.UtcNow.Date ? "~~Claimed~~" : "**Available**")}
//                           Multiplier: **{(user.Vip ? DomainConfig.Currency.VipMultiplier : 1)}**x
//                           Transactions: **{rawSnapshots.Count}**

//                           Last transaction: {last.CreatedAt.DiscordTime(DiscordTimeFlag.R)}
//                           ```diff
//                           {lastTransactionDecorator} {last.Amount.ToGuildCurrencyString()} | {last.Info}
//                           ```
//                           """;
//        var balance = $"""
//                       ```{user.Styling}
//                       Current:{user.AvailableBalance.ToGuildCurrencyString().FormatForDiscordCode(15, true)}
//                       Highest:{(rawSnapshots.MaxBy(s => s.Balance)?.Balance ?? 0d).ToGuildCurrencyString().FormatForDiscordCode(15, true)}
//                       Frozen:{user.Frozen.ToGuildCurrencyString().FormatForDiscordCode(16, true)}
//                       ```
//                       """;
//        var limits = $"""
//                      ```{user.Styling}
//                      Count:{dailyTransactionCount.FormatForDiscordCode(16, true)}
//                      Amount:{dailyTransactionAmount.ToGuildCurrencyString().FormatForDiscordCode(16, true)}

//                      ```
//                      """;

//        return new DiscordEmbedBuilder()
//            .WithAuthor($"{user.InGameName} - {string.Join('|', seasons)}", null, discordUser.AvatarUrl)
//            .WithTitle("Wallet")
//            .WithDescription(description)
//            .AddField("Balance", balance, true)
//            .AddField("Limits", limits, true)
//            .WithColor(new DiscordColor(user.ProfileColor))
//            .WithTimestamp(DateTime.UtcNow)
//            .WithFooter(CoreConfig.Version, discordEngine.Guild.IconUrl)
//            .WithImageUrl(filehostMessage?.Attachments[0].Url ?? "https://i.imgur.com/a2YnFAd.jpeg");
//    }

//    private static IEnumerable<User.BalanceSnapshot> CompressSnapshots(IReadOnlyCollection<User.BalanceSnapshot> snapshots)
//    {
//        var index = 0;
//        return snapshots.GroupBy(_ => index++ / ((int)Math.Ceiling(snapshots.Count / 250d)))
//            .Select(g =>
//            {
//                var values = g.ToList();
//                var last = g.Last();
//                return new User.BalanceSnapshot(
//                    last.CreatedAt,
//                    values.Average(s => s.Balance),
//                    values.Sum(s => s.Amount),
//                    $"{last.Info} (and {values.Count - 1} other compressed snapshots)",
//                    last.Season);
//            });
//    }
//}
