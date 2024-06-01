//﻿using MareSynchronosServer.Models;
using Microsoft.EntityFrameworkCore;

namespace GagspeakServer.Data
{
    public class GagspeakDbContext : DbContext
    {
        public GagspeakDbContext(DbContextOptions<GagspeakDbContext> options) : base(options)
        {
        }

        // public DbSet<User> Users { get; set; }
        // public DbSet<FileCache> Files { get; set; }
        // public DbSet<Whitelist> Whitelists { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // modelBuilder.Entity<User>().ToTable("Users");
            // modelBuilder.Entity<FileCache>().ToTable("FileCaches");
            // modelBuilder.Entity<Whitelist>().ToTable("Whitelists");
        }
    }
}