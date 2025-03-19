//using BCL.Core;
//using BCL.Discord.Bot;
//using BCL.Discord.Extensions;
//using BCL.Domain;
//using BCL.Persistence.Sqlite.Repositories.Abstract;
//using DSharpPlus.Entities;
//using DSharpPlus.Interactivity.Extensions;
//using DSharpPlus.SlashCommands;
//using Humanizer;
//using Humanizer.Localisation;

//#pragma warning disable CS4014

//namespace BCL.Discord.Commands.SlashCommands.Users;
//public partial class UserCommands
//{
//    [SlashCommandGroup("Currency", "Bunch of random stuff.")]
//    [SlashModuleLifespan(SlashModuleLifespan.Scoped)]
//    public class Currency : ApplicationCommandModule
//    {
//        private readonly IUserRepository _userRepository;
//        private readonly DiscordEngine _discordEngine;

//        public Currency(
//            IUserRepository userRepository,
//            DiscordEngine discordEngine
//        )
//        {
//            _userRepository = userRepository;
//            _discordEngine = discordEngine;
//        }

//        [SlashCommand("Claim", "Claim your daily bonus.")]
//        public async Task Claim(InteractionContext context)
//        {
//            var user = _userRepository.GetByDiscordUser(context.User); if (user is null) { SuggestRegistration(context); return; }

//            await context.CreateResponseAsync("Claiming Daily bonus...", true);

//            if (user.DailyBonusClaimed.Date == DateTime.UtcNow.Date)
//            {
//                await context.EditResponseAsync(new DiscordWebhookBuilder()
//                    .WithContent($"You have already claimed your daily bonus. Try again in `{DateTime.Today.AddDays(1).Subtract(DateTime.UtcNow).Humanize(precision: 3, minUnit: TimeUnit.Second)}`"));
//                return;
//            }

//            var payout = DomainConfig.Currency.Daily;
//            if (user.Vip) payout *= DomainConfig.Currency.VipMultiplier;
//            user.ModifyBalance(payout, $"Daily {DomainConfig.Currency.Name} claim");
//            user.DailyBonusClaimed = DateTime.UtcNow;

//            await _userRepository.SaveChangesAsync();

//            await context.EditResponseAsync(new DiscordWebhookBuilder()
//                .WithContent($"Succesfully claimed {payout.ToGuildCurrencyString()}!"));

//            _discordEngine.LogTransaction(user);
//        }

//        [SlashCommand("Send", ";D")]
//        public async Task Send(InteractionContext context,
//            [Option("Amount", "Amount you want to send")] double amount,
//            [Option("Recipient", "Who are you sending to?")] DiscordUser discordUser)
//        {
//            await context.CreateResponseAsync($"Transfering `{amount}` to {discordUser.Mention}");

//            #region Validation

//            var user = _userRepository.GetByDiscordUser(context.User);
//            var recipient = _userRepository.GetByDiscordUser(discordUser);

//            string? validationError = true switch
//            {
//                _ when user is null => $"{context.User.Mention} Please {context.Client.MentionCommand<UserCommands>(nameof(Register))} before using the currency system!⚠️",
//                _ when recipient is null => $"{context.User.Mention} is not registered!⚠️",
//                _ when amount <= 0 => $"{context.User.Mention} Amount can not be 0 or below!⚠️",
//                _ when amount >= user.AvailableBalance => $"{context.User.Mention} You don't have enough **{DomainConfig.Currency.Name}**!⚠️ Your available balance is`{user.AvailableBalance.ToGuildCurrencyString()}`",
//                _ when user.LastTransactionDate.Date == DateTime.UtcNow.Date &&
//                       user.BalanceSentToday + amount > (user.Vip ? double.MaxValue
//                           : DomainConfig.Currency.DailyLimitAmount) => $"{context.User.Mention} Sending this amount would exceed your daily limit. You can still send `{(user.Vip ? double.MaxValue : DomainConfig.Currency.DailyLimitAmount) - user.BalanceSentToday}` today or try again in `{DateTime.Today.AddDays(1).Subtract(DateTime.UtcNow).Humanize(precision: 3, minUnit: TimeUnit.Second)}`!⚠️",
//                _ when user.LastTransactionDate.Date == DateTime.UtcNow.Date &&
//                       user.TransactionsToday >= (user.Vip ? int.MaxValue
//                           : DomainConfig.Currency.DailyLimitCount) => $"{context.User.Mention} You have reached your daily transaction count limit. Try again in `{DateTime.Today.AddDays(1).Subtract(DateTime.UtcNow).Humanize(precision: 3, minUnit: TimeUnit.Second)}`!⚠️",
//                _ => null
//            };

//            if (validationError is not null)
//            {
//                await context.EditResponseAsync(new DiscordWebhookBuilder()
//                    .WithContent(validationError)
//                    .AddMention(new UserMention(context.User)));
//                return;
//            }

//            #endregion

//            #region Confirmation

//            var interactivity = context.Client.GetInteractivity();

//            var embed = new DiscordEmbedBuilder()
//                .WithAuthor("Transaction Details", null, context.Guild.IconUrl)
//                .WithDescription($"""
//                                  Recipient: {recipient!.Mention}
//                                  ```{user!.Styling}
//                                  Sending:        {amount.ToGuildCurrencyString()}
//                                  Balance before: {user.AvailableBalance.ToGuildCurrencyString()}
//                                  Balance after:  {(user.AvailableBalance - amount).ToGuildCurrencyString()}
//                                  ```
//                                  Are you sure you want to complete this transaction?
//                                  React with :white_check_mark: for **YES** and :x: for **NO**.

//                                  """)
//                .WithTimestamp(DateTime.UtcNow)
//                .WithColor(new DiscordColor(user.ProfileColor))
//                .WithFooter($"Version {CoreConfig.Version}", context.Guild.IconUrl);

//            var confirmEmoji = DiscordEmoji.FromName(context.Client, ":white_check_mark:");
//            var declineEmoji = DiscordEmoji.FromName(context.Client, ":x:");

//            var confirmationPrompt = await context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));

//            confirmationPrompt.CreateReactionAsync(confirmEmoji);
//            confirmationPrompt.CreateReactionAsync(declineEmoji);

//            var validReaction = false;
//            while (!validReaction)
//            {
//                var response = await interactivity.WaitForReactionAsync(r =>
//                        r.Message.Id == confirmationPrompt.Id &&
//                        r.User.Id == context.User.Id,
//                    TimeSpan.FromMinutes(1));

//                if (response.TimedOut || response.Result.Emoji == declineEmoji)
//                {
//                    confirmationPrompt.DeleteAsync();
//                    return;
//                }
//                if (response.Result.Emoji == confirmEmoji) validReaction = true;
//            }

//            #endregion

//            if (user.LastTransactionDate.Date != DateTime.UtcNow.Date)
//            {
//                user.TransactionsToday = 0;
//                user.BalanceSentToday = 0;
//            }

//            user.LastTransactionDate = DateTime.UtcNow;
//            user.TransactionsToday++;
//            user.BalanceSentToday += amount;
//            user.ModifyBalance(-amount, "Domestic Transaction(Sender)");
//            recipient.ModifyBalance(amount, "Domestic Transaction(Recipient)");

//            await _userRepository.SaveChangesAsync();

//            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Transaction succesful."));

//            _discordEngine.LogTransaction(user, recipient.DiscordId);
//            _discordEngine.LogTransaction(recipient, user.DiscordId);
//            await context.Interaction.DeleteOriginalResponseAsync(3);
//        }
//    }
//}
