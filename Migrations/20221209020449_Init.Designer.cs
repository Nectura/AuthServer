﻿// <auto-generated />
using System;
using AuthServer.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace AuthServer.Migrations
{
    [DbContext(typeof(EntityContext))]
    [Migration("20221209020449_Init")]
    partial class Init
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("AuthServer.Database.Models.LocalUser", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("EmailAddress")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<byte[]>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.Property<string>("ProfilePicture")
                        .HasColumnType("longtext");

                    b.Property<byte[]>("SaltHash")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.ToTable("LocalUsers");
                });

            modelBuilder.Entity("AuthServer.Database.Models.LocalUserRefreshToken", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("AbsoluteExpirationTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("PreviousRefreshToken")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("RefreshToken")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<byte>("Scopes")
                        .HasColumnType("tinyint unsigned");

                    b.Property<Guid>("UserId")
                        .HasColumnType("char(36)");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.HasIndex("RefreshToken", "PreviousRefreshToken");

                    b.ToTable("LocalUserRefreshTokens");
                });

            modelBuilder.Entity("AuthServer.Database.Models.SocialUser", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<int>("AuthProvider")
                        .HasColumnType("int");

                    b.Property<string>("AuthProviderUserId")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("EmailAddress")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.ToTable("SocialUsers");
                });

            modelBuilder.Entity("AuthServer.Database.Models.SocialUserAuthProviderToken", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("char(36)");

                    b.Property<int>("AuthProvider")
                        .HasColumnType("int");

                    b.Property<string>("AccessToken")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("RefreshToken")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Scopes")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("UserId", "AuthProvider");

                    b.ToTable("SocialUserAuthProviderTokens");
                });

            modelBuilder.Entity("AuthServer.Database.Models.SocialUserRefreshToken", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("AbsoluteExpirationTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("PreviousRefreshToken")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("RefreshToken")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("char(36)");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.HasIndex("RefreshToken", "PreviousRefreshToken");

                    b.ToTable("SocialUserRefreshTokens");
                });

            modelBuilder.Entity("AuthServer.Database.Models.LocalUserRefreshToken", b =>
                {
                    b.HasOne("AuthServer.Database.Models.LocalUser", "User")
                        .WithMany("RefreshTokens")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("AuthServer.Database.Models.SocialUserAuthProviderToken", b =>
                {
                    b.HasOne("AuthServer.Database.Models.SocialUser", "User")
                        .WithMany("AuthProviderTokens")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("AuthServer.Database.Models.SocialUserRefreshToken", b =>
                {
                    b.HasOne("AuthServer.Database.Models.SocialUser", "User")
                        .WithMany("RefreshTokens")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("AuthServer.Database.Models.LocalUser", b =>
                {
                    b.Navigation("RefreshTokens");
                });

            modelBuilder.Entity("AuthServer.Database.Models.SocialUser", b =>
                {
                    b.Navigation("AuthProviderTokens");

                    b.Navigation("RefreshTokens");
                });
#pragma warning restore 612, 618
        }
    }
}