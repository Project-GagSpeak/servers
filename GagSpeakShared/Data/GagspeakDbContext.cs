//﻿using GagSpeakServer.Models;
using Gagspeak.API.Dto.User;
using GagspeakServer.Models;
using GagSpeakServer.Models.Permissions;
using Microsoft.EntityFrameworkCore;
using System.Security.Permissions;

namespace GagspeakServer.Data;

public class GagspeakDbContext : DbContext
{
     public GagspeakDbContext(DbContextOptions<GagspeakDbContext> options) : base(options) { }

     public DbSet<Auth> Auth { get; set; }
     public DbSet<AccountClaimAuth> AccountClaimAuth { get; set; }
     public DbSet<ClientPair> ClientPairs { get; set; }
     public DbSet<ClientPairPermissions> ClientPairPermissions { get; set; }
     public DbSet<User> Users { get; set; }
     public DbSet<UserProfileData> UserProfileData { get; set; }
     public DbSet<UserSettingsData> UserSettingsData { get; set; }
     public DbSet<UserApperanceData> UserApperanceData { get; set; }
     public DbSet<WardrobeGlobalPermissions> WardrobeGlobalPermissions { get; set; }
     public DbSet<WardrobePairPermissions> WardrobePairPermissions { get; set; }
     public DbSet<PuppeteerGlobalPermissions> PuppeteerGlobalPermissions { get; set; }
     public DbSet<PuppeteerPairPermissions> PuppeteerPairPermissions { get; set; }
     public DbSet<ToyboxGlobalPermissions> ToyboxGlobalPermissions { get; set; }
     public DbSet<ToyboxPairPermissions> ToyboxPairPermissions { get; set; }
     public DbSet<HardcorePairPermissions> HardcorePairPermissions { get; set; }

     protected override void OnModelCreating(ModelBuilder modelBuilder)
     {
          modelBuilder.Entity<Auth>().ToTable("auth");
          modelBuilder.Entity<AccountClaimAuth>().ToTable("account_claim_auth");
          modelBuilder.Entity<ClientPair>().ToTable("client_pairs");
          modelBuilder.Entity<ClientPair>().HasKey(u => new { u.UserUID, u.OtherUserUID });
          modelBuilder.Entity<ClientPair>().HasIndex(c => c.UserUID);
          modelBuilder.Entity<ClientPair>().HasIndex(c => c.OtherUserUID);
          modelBuilder.Entity<ClientPairPermissions>().ToTable("client_pair_permissions");
          modelBuilder.Entity<User>().ToTable("users");
          modelBuilder.Entity<UserProfileData>().ToTable("user_profile_data");
          modelBuilder.Entity<UserProfileData>().HasKey(c => c.UserUID);
          modelBuilder.Entity<UserSettingsData>().ToTable("user_settings_data");
          modelBuilder.Entity<UserSettingsData>().HasKey(c => c.UserUID);
          modelBuilder.Entity<UserApperanceData>().ToTable("user_apperance_data");
          modelBuilder.Entity<UserApperanceData>().HasKey(c => c.UserUID);
          modelBuilder.Entity<WardrobeGlobalPermissions>().ToTable("wardrobe_global_permissions");
          modelBuilder.Entity<WardrobeGlobalPermissions>().HasKey(c => c.UserUID);
          modelBuilder.Entity<WardrobePairPermissions>().ToTable("wardrobe_pair_permissions");
          modelBuilder.Entity<WardrobePairPermissions>().HasKey(u => new { u.UserUID, u.OtherUserUID });
          modelBuilder.Entity<WardrobePairPermissions>().HasIndex(c => c.UserUID);
          modelBuilder.Entity<WardrobePairPermissions>().HasIndex(c => c.OtherUserUID);
          modelBuilder.Entity<PuppeteerGlobalPermissions>().ToTable("puppeteer_global_permissions");
          modelBuilder.Entity<PuppeteerGlobalPermissions>().HasKey(c => c.UserUID);
          modelBuilder.Entity<PuppeteerPairPermissions>().ToTable("puppeteer_pair_permissions");
          modelBuilder.Entity<PuppeteerPairPermissions>().HasKey(u => new { u.UserUID, u.OtherUserUID });
          modelBuilder.Entity<PuppeteerPairPermissions>().HasIndex(c => c.UserUID);
          modelBuilder.Entity<PuppeteerPairPermissions>().HasIndex(c => c.OtherUserUID);
          modelBuilder.Entity<ToyboxGlobalPermissions>().ToTable("toybox_global_permissions");
          modelBuilder.Entity<ToyboxGlobalPermissions>().HasKey(c => c.UserUID);
          modelBuilder.Entity<ToyboxPairPermissions>().ToTable("toybox_pair_permissions");
          modelBuilder.Entity<ToyboxPairPermissions>().HasKey(u => new { u.UserUID, u.OtherUserUID });
          modelBuilder.Entity<ToyboxPairPermissions>().HasIndex(c => c.UserUID);
          modelBuilder.Entity<ToyboxPairPermissions>().HasIndex(c => c.OtherUserUID);
          modelBuilder.Entity<HardcorePairPermissions>().ToTable("hardcore_pair_permissions");
          modelBuilder.Entity<HardcorePairPermissions>().HasKey(u => new { u.UserUID, u.OtherUserUID });
          modelBuilder.Entity<HardcorePairPermissions>().HasIndex(c => c.UserUID);
          modelBuilder.Entity<HardcorePairPermissions>().HasIndex(c => c.OtherUserUID);
     }
}