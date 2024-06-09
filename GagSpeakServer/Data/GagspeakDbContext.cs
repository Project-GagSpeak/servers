//﻿using GagSpeakServer.Models;
using GagspeakServer.Models;
using Microsoft.EntityFrameworkCore;

namespace GagspeakServer.Data;

public class GagspeakDbContext : DbContext
{
// #if DEBUG
//     public GagspeakDbContext() { }

//     protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//     {
//         if (optionsBuilder.IsConfigured)
//         {
//             base.OnConfiguring(optionsBuilder);
//             return;
//         }

//         optionsBuilder.UseMySql("Host=localhost;Port=5432;Database=mare;Username=postgres", builder =>
//         {
//             builder.MigrationsHistoryTable("_efmigrationshistory", "public");
//             builder.MigrationsAssembly("GagSpeakShared");
//         }).UseSnakeCaseNamingConvention();
//         optionsBuilder.EnableThreadSafetyChecks(false);

//         base.OnConfiguring(optionsBuilder);
//     }
// #endif
    public GagspeakDbContext(DbContextOptions<GagspeakDbContext> options) : base(options)
    {
    }

    // discord related data tables for authentication
    public DbSet<Auth> Auth { get; set; }
    public DbSet<LodeStoneAuth> LodeStoneAuth { get; set; }
    
    public DbSet<User> Users { get; set; }
    //public DbSet<FileCache> Files { get; set; }
    // public DbSet<Whitelist> Whitelists { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Auth>().ToTable("auth");
        modelBuilder.Entity<LodeStoneAuth>().ToTable("lodestone_auth");
        modelBuilder.Entity<User>().ToTable("users");
        // modelBuilder.Entity<FileCache>().ToTable("FileCaches");
        // modelBuilder.Entity<Whitelist>().ToTable("Whitelists");
    }
}