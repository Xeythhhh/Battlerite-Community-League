﻿// <auto-generated />
using System;
using BCL.Persistence.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BCL.Persistence.Sqlite.Migrations
{
    [DbContext(typeof(BclDbContext))]
    [Migration("20231116212224_Add_ProApplications_On_User")]
    partial class Add_ProApplications_On_User
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.4");

            modelBuilder.Entity("BCL.Domain.Entities.Analytics.MigrationInfo", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DateOverride")
                        .HasColumnType("TEXT");

                    b.Property<string>("ExistingSeasons")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("LastBotVersionUsed")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("Version")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("MigrationInfo");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Analytics.RegionStats", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<long>("Average")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DateOverride")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("LongestLink")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("LongestTime")
                        .HasColumnType("INTEGER");

                    b.Property<string>("LongestUserId")
                        .HasColumnType("TEXT");

                    b.Property<int>("Region")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Season")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ShortestLink")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("ShortestTime")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ShortestUserId")
                        .HasColumnType("TEXT");

                    b.Property<int>("TimedDrafts")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("LongestUserId");

                    b.HasIndex("ShortestUserId");

                    b.ToTable("RegionDraftTimes");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Analytics.StatsSnapshot", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<int>("CaptainGames")
                        .HasColumnType("INTEGER");

                    b.Property<int>("CaptainWins")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DateOverride")
                        .HasColumnType("TEXT");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<double?>("Eloshift")
                        .HasColumnType("REAL");

                    b.Property<int>("GamesPlayed")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GamesPlayed_Custom")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GamesPlayed_Event")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GamesPlayed_Premade3v3")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GamesPlayed_Pro")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GamesPlayed_Standard")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GamesPlayed_Tournament")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GamesWon")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GamesWon_Custom")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GamesWon_Event")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GamesWon_Premade3v3")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GamesWon_Pro")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GamesWon_Standard")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GamesWon_Tournament")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<int>("League")
                        .HasColumnType("INTEGER");

                    b.Property<string>("MatchId")
                        .HasColumnType("TEXT");

                    b.Property<double>("Rating")
                        .HasColumnType("REAL");

                    b.Property<double>("Rating_Standard")
                        .HasColumnType("REAL");

                    b.Property<string>("Season")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("StatsId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("StatsId");

                    b.ToTable("StatsSnapshots");

                    b.HasDiscriminator<string>("Discriminator").HasValue("StatsSnapshot");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Matches.Draft", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("Captain1DiscordId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("Captain2DiscordId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DateOverride")
                        .HasColumnType("TEXT");

                    b.Property<int>("DraftType")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsTeam1Turn")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<int>("RemainingActions")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Drafts");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Matches.DraftStep", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<int>("Action")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DateOverride")
                        .HasColumnType("TEXT");

                    b.Property<string>("DraftId")
                        .HasColumnType("TEXT");

                    b.Property<int>("Index")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsCurrent")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsLastPick")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("TokenId1")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("TokenId2")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("DraftId");

                    b.ToTable("DraftStep");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Matches.Match", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("BotVersion")
                        .HasColumnType("TEXT");

                    b.Property<string>("CancelReason")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DateOverride")
                        .HasColumnType("TEXT");

                    b.Property<string>("DraftId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<double>("EloShift")
                        .HasColumnType("REAL");

                    b.Property<string>("JumpLink")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<int>("League")
                        .HasColumnType("INTEGER");

                    b.Property<string>("MapId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("MatchmakingLogic")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Outcome")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Region")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Season")
                        .HasColumnType("TEXT");

                    b.Property<string>("Team1")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Team2")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("_discordUserIds")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("DraftId");

                    b.ToTable("Matches");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Queue.Champion", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<int>("Class")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DateOverride")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Disabled")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("LatestMatch")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("LatestMatch_Label")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Restrictions")
                        .HasColumnType("TEXT");

                    b.Property<int>("Role")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("StandardBanned")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Champions");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Queue.ChampionStats", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("ChampionId")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DateOverride")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("Season")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ChampionId");

                    b.ToTable("ChampionStats");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Queue.Map", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DateOverride")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Disabled")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Frequency")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GamesPlayed")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("Pro")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("StandardBanned")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Variant")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Maps");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Users.PremadeTeam", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DateOverride")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsRated")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("MemberIds")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<double>("Rating")
                        .HasColumnType("REAL");

                    b.Property<int>("Size")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Teams");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Users.Stats", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<long>("AverageDraftTime")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DateOverride")
                        .HasColumnType("TEXT");

                    b.Property<string>("Disclaimer")
                        .HasColumnType("TEXT");

                    b.Property<double>("HighestRating")
                        .HasColumnType("REAL");

                    b.Property<double>("HighestRating_Standard")
                        .HasColumnType("REAL");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("LongestDraftLink")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("LongestDraftTime")
                        .HasColumnType("INTEGER");

                    b.Property<double>("LowestRating")
                        .HasColumnType("REAL");

                    b.Property<double>("LowestRating_Standard")
                        .HasColumnType("REAL");

                    b.Property<int>("Membership")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Season")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ShortestDraftLink")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("ShortestDraftTime")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TimedDrafts")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UserId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Stats");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Users.User", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Approved")
                        .HasColumnType("INTEGER");

                    b.Property<double>("Balance")
                        .HasColumnType("REAL");

                    b.Property<string>("BalanceHistory")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<double>("BalanceSentToday")
                        .HasColumnType("REAL");

                    b.Property<double>("BetAmount")
                        .HasColumnType("REAL");

                    b.Property<string>("Bio")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<ulong>("ChannelId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ChannelName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<double>("ChartAlpha")
                        .HasColumnType("REAL");

                    b.Property<string>("Chart_MainRatingColor")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Chart_MainWinrateColor")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Chart_SecondaryRatingColor")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Chart_SecondaryWinrateColor")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<bool>("CrossRegion")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("DailyBonusClaimed")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DateOverride")
                        .HasColumnType("TEXT");

                    b.Property<string>("DefaultMelee")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("DefaultRanged")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("DefaultSupport")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<ulong>("DiscordId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("DisplayBothCharts")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("EmojiId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Eu")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("FirstWinClaimed")
                        .HasColumnType("TEXT");

                    b.Property<double>("Frozen")
                        .HasColumnType("REAL");

                    b.Property<string>("InGameName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsTestUser")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("LastPlayed")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("LastQueued")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastTransactionDate")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("LatestMatchLink")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("LatestMatch_DiscordLink_Label")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("LatestMatch_DiscordLink_ToolTip")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("MatchHistory_DisplayCustom")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("MatchHistory_DisplayEvent")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("MatchHistory_DisplayTournament")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Na")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("NewMatchDm")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PlacementGamesRemaining")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PlacementGamesRemaining_Standard")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Pro")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("ProApplicationTimeout")
                        .HasColumnType("TEXT");

                    b.Property<string>("ProApplications")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("ProQueue")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ProfileColor")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ProfileVersion")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("PurgeAfter")
                        .HasColumnType("INTEGER");

                    b.Property<double>("Rating")
                        .HasColumnType("REAL");

                    b.Property<double>("Rating_Standard")
                        .HasColumnType("REAL");

                    b.Property<string>("RegistrationInfo")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("RoleColor")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("RoleIconUrl")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<ulong>("RoleId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("RoleName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("RoleSuffix")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("Sa")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("SecondaryRoleId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Server")
                        .HasColumnType("INTEGER");

                    b.Property<int>("SessionEloChange")
                        .HasColumnType("INTEGER");

                    b.Property<int>("SessionEloChange_Pro")
                        .HasColumnType("INTEGER");

                    b.Property<int>("SessionGames")
                        .HasColumnType("INTEGER");

                    b.Property<int>("SessionWins")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("StandardQueue")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Styling")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("SubbedIn")
                        .HasColumnType("INTEGER");

                    b.Property<string>("TeamId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("TeamName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("Timeout")
                        .HasColumnType("TEXT");

                    b.Property<string>("TimeoutReason")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("TransactionsToday")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Vip")
                        .HasColumnType("INTEGER");

                    b.Property<int>("WinStreak")
                        .HasColumnType("INTEGER");

                    b.Property<int>("WinStreak_Pro")
                        .HasColumnType("INTEGER");

                    b.Property<int>("WinStreak_Standard")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Analytics.ChampionStatsSnapshot", b =>
                {
                    b.HasBaseType("BCL.Domain.Entities.Analytics.StatsSnapshot");

                    b.Property<int>("Banned")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Banned_Custom")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Banned_Event")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Banned_Premade3v3")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Banned_Pro")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Banned_Standard")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Banned_Tournament")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ChampionStatsId")
                        .HasColumnType("TEXT");

                    b.Property<int>("MatchCount")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MatchCount_Custom")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MatchCount_Event")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MatchCount_Premade3v3")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MatchCount_Pro")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MatchCount_Standard")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MatchCount_Tournament")
                        .HasColumnType("INTEGER");

                    b.HasIndex("ChampionStatsId");

                    b.HasDiscriminator().HasValue("ChampionStatsSnapshot");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Analytics.RegionStats", b =>
                {
                    b.HasOne("BCL.Domain.Entities.Users.User", "LongestUser")
                        .WithMany()
                        .HasForeignKey("LongestUserId");

                    b.HasOne("BCL.Domain.Entities.Users.User", "ShortestUser")
                        .WithMany()
                        .HasForeignKey("ShortestUserId");

                    b.Navigation("LongestUser");

                    b.Navigation("ShortestUser");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Analytics.StatsSnapshot", b =>
                {
                    b.HasOne("BCL.Domain.Entities.Users.Stats", null)
                        .WithMany("Snapshots")
                        .HasForeignKey("StatsId");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Matches.DraftStep", b =>
                {
                    b.HasOne("BCL.Domain.Entities.Matches.Draft", null)
                        .WithMany("Steps")
                        .HasForeignKey("DraftId");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Matches.Match", b =>
                {
                    b.HasOne("BCL.Domain.Entities.Matches.Draft", "Draft")
                        .WithMany()
                        .HasForeignKey("DraftId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Draft");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Queue.ChampionStats", b =>
                {
                    b.HasOne("BCL.Domain.Entities.Queue.Champion", null)
                        .WithMany("Stats")
                        .HasForeignKey("ChampionId");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Users.Stats", b =>
                {
                    b.HasOne("BCL.Domain.Entities.Users.User", null)
                        .WithMany("SeasonStats")
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Analytics.ChampionStatsSnapshot", b =>
                {
                    b.HasOne("BCL.Domain.Entities.Queue.ChampionStats", null)
                        .WithMany("Snapshots")
                        .HasForeignKey("ChampionStatsId");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Matches.Draft", b =>
                {
                    b.Navigation("Steps");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Queue.Champion", b =>
                {
                    b.Navigation("Stats");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Queue.ChampionStats", b =>
                {
                    b.Navigation("Snapshots");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Users.Stats", b =>
                {
                    b.Navigation("Snapshots");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Users.User", b =>
                {
                    b.Navigation("SeasonStats");
                });
#pragma warning restore 612, 618
        }
    }
}
