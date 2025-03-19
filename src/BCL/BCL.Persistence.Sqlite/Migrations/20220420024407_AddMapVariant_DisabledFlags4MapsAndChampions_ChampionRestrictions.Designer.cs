﻿// <auto-generated />

#nullable disable

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BCL.Persistence.Sqlite.Migrations
{
    [DbContext(typeof(BclDbContext))]
    [Migration("20220420024407_AddMapVariant_DisabledFlags4MapsAndChampions_ChampionRestrictions")]
    partial class AddMapVariant_DisabledFlags4MapsAndChampions_ChampionRestrictions
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.4");

            modelBuilder.Entity("BCL.Domain.Entities.Queue.Champion", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<int>("ChampionRole")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Class")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Disabled")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Restrictions")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Champions");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Queue.Draft", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("Captain1Id")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Captain2Id")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Captain1Id");

                    b.HasIndex("Captain2Id");

                    b.ToTable("Drafts");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Queue.DraftStep", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<int>("Action")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ChampionId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("DraftId")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("MapId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ChampionId");

                    b.HasIndex("DraftId");

                    b.HasIndex("MapId");

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

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("DraftId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("DraftId");

                    b.ToTable("Matches");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Users.User", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("DiscordId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("InGameName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("MatchId")
                        .HasColumnType("TEXT");

                    b.Property<string>("MatchId1")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("MatchId");

                    b.HasIndex("MatchId1");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Queue.Draft", b =>
                {
                    b.HasOne("BCL.Domain.Entities.Users.User", "Captain1")
                        .WithMany()
                        .HasForeignKey("Captain1Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BCL.Domain.Entities.Users.User", "Captain2")
                        .WithMany()
                        .HasForeignKey("Captain2Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Captain1");

                    b.Navigation("Captain2");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Queue.DraftStep", b =>
                {
                    b.HasOne("BCL.Domain.Entities.Queue.Champion", "Champion")
                        .WithMany()
                        .HasForeignKey("ChampionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BCL.Domain.Entities.Queue.Draft", null)
                        .WithMany("Steps")
                        .HasForeignKey("DraftId");

                    b.HasOne("BCL.Domain.Entities.Queue.Map", "Map")
                        .WithMany()
                        .HasForeignKey("MapId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Champion");

                    b.Navigation("Map");
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

            modelBuilder.Entity("BCL.Domain.Entities.Users.User", b =>
                {
                    b.HasOne("BCL.Domain.Entities.Queue.Match", null)
                        .WithMany("Team1")
                        .HasForeignKey("MatchId");

                    b.HasOne("BCL.Domain.Entities.Queue.Match", null)
                        .WithMany("Team2")
                        .HasForeignKey("MatchId1");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Queue.Draft", b =>
                {
                    b.Navigation("Steps");
                });

            modelBuilder.Entity("BCL.Domain.Entities.Queue.Match", b =>
                {
                    b.Navigation("Team1");

                    b.Navigation("Team2");
                });
#pragma warning restore 612, 618
        }
    }
}
