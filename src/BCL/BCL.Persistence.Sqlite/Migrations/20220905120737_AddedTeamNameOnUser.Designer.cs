﻿// <auto-generated />

#nullable disable

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    [DbContext(typeof(BclDbContext))]
    [Migration("20220905120737_AddedTeamNameOnUser")]
    partial class AddedTeamNameOnUser
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.4");

            modelBuilder.Entity("BCL.Domain.Entities.Queue.Champion", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("BanRate")
                        .HasColumnType("TEXT");

                    b.Property<int>("Class")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Disabled")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GamesBanned")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GamesPicked")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GamesWon")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("PickRate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Restrictions")
                        .HasColumnType("TEXT");

                    b.Property<int>("Role")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Champions");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Queue.Draft", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("Captain1DiscordId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("Captain2DiscordId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Drafts");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Queue.DraftStep", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<int>("Action")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("DraftId")
                        .HasColumnType("TEXT");

                    b.Property<int>("Index")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsCurrent")
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

            modelBuilder.Entity("BCL.Domain.Entities.Queue.Map", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAt")
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

                    b.Property<int>("Variant")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Maps");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Queue.Match", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("CancelReason")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("DraftId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<double>("EloShift")
                        .HasColumnType("REAL");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("MapId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("MatchmakingLogic")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Outcome")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Season")
                        .HasColumnType("TEXT");

                    b.Property<int>("Server")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Team1")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Team2")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("DraftId");

                    b.ToTable("Matches");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Queue.Stats", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<double>("HighestRating")
                        .HasColumnType("REAL");

                    b.Property<double>("HighestRating_Standard")
                        .HasColumnType("REAL");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<double>("LowestRating")
                        .HasColumnType("REAL");

                    b.Property<double>("LowestRating_Standard")
                        .HasColumnType("REAL");

                    b.Property<string>("Season")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("UserId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Stats");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Queue.StatsSnapshot", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<int>("GamesPlayed")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GamesPlayed_Custom")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GamesPlayed_Event")
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

                    b.Property<int>("GamesWon_Pro")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GamesWon_Standard")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GamesWon_Tournament")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<double>("Rating")
                        .HasColumnType("REAL");

                    b.Property<double>("Rating_Standard")
                        .HasColumnType("REAL");

                    b.Property<string>("StatsId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("StatsId");

                    b.ToTable("StatsSnapshots");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Users.User", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Approved")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Declined")
                        .HasColumnType("INTEGER");

                    b.Property<string>("DefaultMelee")
                        .HasColumnType("TEXT");

                    b.Property<string>("DefaultRanged")
                        .HasColumnType("TEXT");

                    b.Property<string>("DefaultSupport")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("DiscordId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Eu")
                        .HasColumnType("INTEGER");

                    b.Property<string>("InGameName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsTestUser")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("LastQueued")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Na")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("NewMember")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PlacementGamesRemaining")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PlacementGamesRemaining_Standard")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Pro")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ProfileVersion")
                        .HasColumnType("TEXT");

                    b.Property<double>("Rating")
                        .HasColumnType("REAL");

                    b.Property<double>("Rating_Standard")
                        .HasColumnType("REAL");

                    b.Property<string>("RegistrationInfo")
                        .HasColumnType("TEXT");

                    b.Property<int>("Server")
                        .HasColumnType("INTEGER");

                    b.Property<string>("TeamName")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Queue.DraftStep", b =>
                {
                    b.HasOne("BCL.Domain.Entities.Queue.Draft", null)
                        .WithMany("Steps")
                        .HasForeignKey("DraftId");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Queue.Match", b =>
                {
                    b.HasOne("BCL.Domain.Entities.Queue.Draft", "Draft")
                        .WithMany()
                        .HasForeignKey("DraftId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Draft");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Queue.Stats", b =>
                {
                    b.HasOne("BCL.Domain.Entities.Users.User", null)
                        .WithMany("SeasonStats")
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Queue.StatsSnapshot", b =>
                {
                    b.HasOne("BCL.Domain.Entities.Queue.Stats", null)
                        .WithMany("Snapshots")
                        .HasForeignKey("StatsId");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Queue.Draft", b =>
                {
                    b.Navigation("Steps");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Queue.Stats", b =>
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
