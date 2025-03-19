using BCL.Core.Services;
using BCL.Core;
using BCL.Core.Services.Queue;
using BCL.Domain.Enums;
using BCL.Persistence.Sqlite.Repositories.Abstract;

using DSharpPlus.Entities;

using Microsoft.Extensions.DependencyInjection;
using BCL.Discord.Utils;

namespace BCL.Discord.Components.Dashboards;

public partial class QueueTracker
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public static class LeagueFields
    {
        private static IServiceProvider _services;
        private static IUserRepository _userRepository;
        //private static ITeamRepository _teamRepository;

        private const string QueueKingDefault = "You could be #1";
        private static bool _updatePending;

        public static void Setup(IServiceProvider services)
        {
            _services = services;
            IServiceScope scope = _services.CreateScope();
            _userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            //_teamRepository = scope.ServiceProvider.GetRequiredService<ITeamRepository>();

            MatchService.MatchFinished += TriggerUpdate;
        }

        private static Task TriggerUpdate()
        {
            if (_updatePending) return Task.CompletedTask;

            _updatePending = true;

            _ = Task.Run(() =>
            {
                Refresh();
                _updatePending = false;
            });

            return Task.CompletedTask;
        }

        public static void Refresh()
        {
            _proQueueKing = GetProKing();
            _standardQueueKing = GetStandardKing();
            //_premade3V3QueueKing = GetPremade3V3King();
        }

        public static DiscordMessageBuilder CreateBuilder() =>
            new DiscordMessageBuilder().WithContent($"{Pro}\n{Standard}");

        private static string _proQueueKing = QueueKingDefault;
        private static string _standardQueueKing = QueueKingDefault;
        //private static string _premade3V3QueueKing = QueueKingDefault;

        private static string GetProKing()
        {
            return _userRepository
                .Get(u => u.Pro && u.Approved && u.PlacementGamesRemaining <= 0)
                .OrderByDescending(u => u.Rating)
                .Select(u => u.InGameName)
                .FirstOrDefault() ?? QueueKingDefault;
        }

        private static string GetStandardKing()
        {
            return _userRepository
                .Get(u => !u.Pro && u.Approved && u.PlacementGamesRemaining_Standard <= 0)
                .OrderByDescending(u => u.Rating_Standard)
                .Select(u => u.InGameName)
                .FirstOrDefault() ?? QueueKingDefault;
        }

        //private static string GetPremade3V3King()
        //{
        //    return _teamRepository
        //        .Get(t => t.Size == 3 && t.IsRated)
        //        .OrderByDescending(t => t.Rating)
        //        .Select(t => t.Name)
        //        .FirstOrDefault() ?? QueueKingDefault;
        //}

        //private static string Premade3v3 =>
        //    GenerateQueueStatus("Premade 3v3 League",
        //        QueueService.EuPremade3V3Count,
        //        QueueService.NaPremade3V3Count,
        //        QueueService.SaPremade3V3Count,
        //        MatchmakingService.ProLogic,
        //        MatchmakingService.ProDraftType,
        //        CoreConfig.Draft.ProFormat,
        //        _premade3V3QueueKing);

        private static string Pro =>
            GenerateQueueStatus("Pro League",
                QueueService.EuProCount,
                QueueService.NaProCount,
                QueueService.SaProCount,
                MatchmakingService.ProLogic,
                MatchmakingService.ProDraftType,
                CoreConfig.Draft.ProFormat,
                _proQueueKing,
                5);

        private static string Standard =>
            GenerateQueueStatus("Standard League",
                QueueService.EuStandardCount,
                QueueService.NaStandardCount,
                QueueService.SaStandardCount,
                MatchmakingService.StandardLogic,
                MatchmakingService.StandardDraftType,
                CoreConfig.Draft.StandardFormat,
                _standardQueueKing);

        private static string GenerateQueueStatus(
            string name,
            int euCount,
            int naCount,
            int saCount,
            MatchmakingLogic logic,
            DraftType draftType,
            List<string> format,
            string king,
            int extraPaddingForLabel = 0)
        {
            string euColor = GetColor(euCount);
            string naColor = GetColor(naCount);
            string saColor = GetColor(saCount);

            string detailsDecorator = $"{ANSIColors.Reset}{ANSIColors.Background.Yellow}{ANSIColors.White}> ";
            string eu = $"{ANSIColors.Background.Black}{ANSIColors.White} EU {euColor}{euCount} {ANSIColors.Background.Green} ";
            string na = $"{ANSIColors.Background.Black}{ANSIColors.White} NA {naColor}{naCount} {ANSIColors.Background.Green} ";
            string sa = $"{ANSIColors.Background.Black}{ANSIColors.White} SA {saColor}{saCount} {ANSIColors.Background.Green} {ANSIColors.Background.Black} ";

            string nameInfo = $"{StatusIndicator}{ANSIColors.Black}{ANSIColors.Bold}{name} {ANSIColors.Background.Black}{ANSIColors.White}{new string(' ', extraPaddingForLabel)}   ♔ {king} ♔ ";
            string draftInfo = $"{ANSIColors.Background.Black}{ANSIColors.White}{ANSIColors.Underline} {logic} Teams with {draftType} Draft {new string(' ', 12)}";
            string formatInfo = $"{ANSIColors.Background.Green} {ANSIColors.Black}Format{ANSIColors.LightGray}: {ANSIColors.Cyan}{string.Join($"{ANSIColors.LightGray} - {ANSIColors.Cyan}", format)} ";
            string details = detailsDecorator + eu + na + sa;

            string[] lines = [nameInfo, draftInfo, formatInfo, details];
            int[] lengths = lines.Select(s => AnsiUtils.GetVisibleLength(s)).ToArray();
            int length = lengths.Max();

            int nameVisibleLength = AnsiUtils.GetVisibleLength(nameInfo);
            if (nameVisibleLength < length)
                nameInfo += new string(' ', length - nameVisibleLength);

            int draftVisibleLength = AnsiUtils.GetVisibleLength(draftInfo);
            if (draftVisibleLength < length)
                draftInfo += new string(' ', length - draftVisibleLength);

            int formatVisibleLength = AnsiUtils.GetVisibleLength(formatInfo);
            if (formatVisibleLength < length)
                formatInfo += new string(' ', length - formatVisibleLength);

            int detailsVisibleLength = AnsiUtils.GetVisibleLength(details);
            if (detailsVisibleLength < length)
                details += new string(' ', length - detailsVisibleLength);

            string content =
                $"""
                {nameInfo}
                {draftInfo}
                {formatInfo}
                {details}
                """;

            return Wrap(content);
        }

        private static string GetColor(int count) => count switch
        {
            0 or 1 => ANSIColors.Red,
            2 or 3 => ANSIColors.Yellow,
            4 or 5 => ANSIColors.Green,
            _ => ANSIColors.Magenta
        };
    }
}
