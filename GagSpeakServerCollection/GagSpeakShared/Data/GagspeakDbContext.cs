using GagspeakShared.Models;
using Microsoft.EntityFrameworkCore;
#pragma warning disable MA0051 // Method is too long
namespace GagspeakShared.Data;

public class GagspeakDbContext : DbContext
{
#if DEBUG
    public GagspeakDbContext() { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            base.OnConfiguring(optionsBuilder);
            return;
        }

        optionsBuilder.UseNpgsql("Host=15.204.93.107;Port=5432;Database=gagspeak;Username=ckgagspeak", builder =>
        {
            builder.MigrationsHistoryTable("_efmigrationshistory", "public");
            builder.MigrationsAssembly("GagSpeakShared");
        }).UseSnakeCaseNamingConvention();
        optionsBuilder.EnableThreadSafetyChecks(false);

        base.OnConfiguring(optionsBuilder);
    }
#endif
    public GagspeakDbContext(DbContextOptions<GagspeakDbContext> options) : base(options) { }

    // Account Handling.
    public DbSet<AccountClaimAuth> AccountClaimAuth { get; set; }
    public DbSet<Auth> Auth { get; set; }
    public DbSet<AccountReputation> AccountReputation { get; set; }

    // Naughty users timeout corner management tables.
    public DbSet<BannedRegistrations> BannedRegistrations { get; set; }
    public DbSet<Banned> BannedUsers { get; set; }

    // Kinkster management.
    public DbSet<ClientPair> ClientPairs { get; set; }
    public DbSet<PairPermissions> PairPermissions { get; set; }
    public DbSet<PairPermissionAccess> PairAccess { get; set; }
    public DbSet<CollarOwner> CollarOwners { get; set; }

    // Requests
    public DbSet<PairingRequest> PairRequests { get; set; }
    public DbSet<CollaringRequest> CollarRequests { get; set; }

    // ShareHub related tables.
    public DbSet<PatternEntry> Patterns { get; set; } // Pattern Sharehub
    public DbSet<LociStatus> LociStatuses { get; set; } // Loci Sharehub
    public DbSet<PatternKeyword> PatternKeywords { get; set; } // Linker between tags and Patterns.
    public DbSet<LociKeyword> LociKeywords { get; set; } // Linker between tags and LociData.
    public DbSet<Keyword> Keywords { get; set; } // the tags that can be associated with the patterns.
    public DbSet<LikesPatterns> PatternLikes { get; set; } // tracks the patterns a user has liked.
    public DbSet<LikesLoci> LociStatusLikes { get; set; } // tracks the locidata a user has liked.

    // Reporting
    public DbSet<ReportedUserProfile> ProfileReports { get; set; } // Holds info about reported profiles for assistants to overview.
    public DbSet<ReportedChat> ChatReports { get; set; }
    // User Information
    public DbSet<User> Users { get; set; }
    public DbSet<GlobalPermissions> GlobalPermissions { get; set; }
    public DbSet<UserHardcoreState> HardcoreState { get; set; }
    public DbSet<UserProfile> UserProfileData { get; set; }
    public DbSet<UserAchievementData> AchievementData { get; set; } // tracks the achievements a user has unlocked.

    // User Active State Data
    public DbSet<UserGagData> ActiveGagData { get; set; }
    public DbSet<UserRestrictionData> ActiveRestrictionData { get; set; }
    public DbSet<UserRestraintData> ActiveRestraintData { get; set; }
    public DbSet<UserCollarData> ActiveCollarData { get; set; }
    
    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<AccountClaimAuth>().ToTable("account_claim_auth");
        mb.Entity<Auth>().ToTable("auth");
        mb.Entity<Auth>().HasIndex(a => a.UserUID);
        mb.Entity<Auth>().HasIndex(a => a.PrimaryUserUID);
        mb.Entity<AccountReputation>().ToTable("account_reputation");

        mb.Entity<Banned>().ToTable("banned_users");
        mb.Entity<BannedRegistrations>().ToTable("banned_registrations");
        
        mb.Entity<ClientPair>().ToTable("client_pairs");
        mb.Entity<ClientPair>().HasKey(u => new { u.UserUID, u.OtherUserUID });
        mb.Entity<ClientPair>().HasIndex(c => c.UserUID);
        mb.Entity<ClientPair>().HasIndex(c => c.OtherUserUID);
        mb.Entity<PairPermissions>().ToTable("client_pair_permissions");
        mb.Entity<PairPermissions>().HasKey(u => new { u.UserUID, u.OtherUserUID });
        mb.Entity<PairPermissions>().HasIndex(c => c.UserUID);
        mb.Entity<PairPermissions>().HasIndex(c => c.OtherUserUID);
        mb.Entity<PairPermissionAccess>().ToTable("client_pair_permissions_access");
        mb.Entity<PairPermissionAccess>().HasKey(u => new { u.UserUID, u.OtherUserUID });
        mb.Entity<PairPermissionAccess>().HasIndex(c => c.UserUID);
        mb.Entity<PairPermissionAccess>().HasIndex(c => c.OtherUserUID);
        mb.Entity<CollarOwner>().ToTable("collar_owner");
        mb.Entity<CollarOwner>().HasKey(u => new { u.OwnerUID, u.CollaredUserUID });
        mb.Entity<CollarOwner>().HasIndex(c => c.OwnerUID);
        mb.Entity<CollarOwner>().HasIndex(c => c.CollaredUserUID);

        mb.Entity<PairingRequest>().ToTable("kinkster_pair_requests");
        mb.Entity<PairingRequest>().HasKey(u => new { u.UserUID, u.OtherUserUID });
        mb.Entity<PairingRequest>().HasIndex(c => c.UserUID);
        mb.Entity<PairingRequest>().HasIndex(c => c.OtherUserUID);
        mb.Entity<CollaringRequest>().ToTable("kinkster_collar_requests");
        mb.Entity<CollaringRequest>().HasKey(u => new { u.UserUID, u.OtherUserUID });
        mb.Entity<CollaringRequest>().HasIndex(c => c.UserUID);
        mb.Entity<CollaringRequest>().HasIndex(c => c.OtherUserUID);

        // should probably rework this at some point
        mb.Entity<Keyword>().ToTable("keywords");
        mb.Entity<Keyword>().HasIndex(c => c.Word).IsUnique();

        mb.Entity<PatternKeyword>().ToTable("pattern_keywords");
        mb.Entity<PatternKeyword>().HasKey(pk => new { pk.PatternEntryId, pk.KeywordWord });
        mb.Entity<PatternKeyword>().HasOne(pk => pk.PatternEntry).WithMany(pe => pe.PatternKeywords).HasForeignKey(pk => pk.PatternEntryId);
        mb.Entity<PatternKeyword>().HasOne(pk => pk.Keyword).WithMany(k => k.PatternKeywords).HasForeignKey(pk => pk.KeywordWord);
        mb.Entity<PatternKeyword>().HasIndex(c => c.PatternEntryId);
        mb.Entity<PatternKeyword>().HasIndex(c => c.KeywordWord);
        mb.Entity<LociKeyword>().ToTable("loci_keywords");
        mb.Entity<LociKeyword>().HasKey(mk => new { mk.MoodleStatusId, mk.KeywordWord });
        mb.Entity<LociKeyword>().HasOne(mk => mk.MoodleStatus).WithMany(ms => ms.LociKeywords).HasForeignKey(mk => mk.MoodleStatusId);
        mb.Entity<LociKeyword>().HasOne(mk => mk.Keyword).WithMany(k => k.MoodleKeywords).HasForeignKey(mk => mk.KeywordWord);
        mb.Entity<LociKeyword>().HasIndex(c => c.MoodleStatusId);
        mb.Entity<LociKeyword>().HasIndex(c => c.KeywordWord);

        mb.Entity<LociStatus>().ToTable("loci_status");
        mb.Entity<LociStatus>().HasKey(ms => ms.Identifier);
        mb.Entity<LociStatus>().HasIndex(ms => ms.Title);
        mb.Entity<LociStatus>().HasIndex(ms => ms.Author);
        mb.Entity<PatternEntry>().ToTable("pattern_entry");
        mb.Entity<PatternEntry>().HasKey(pe => pe.Identifier);
        mb.Entity<PatternEntry>().HasIndex(pe => pe.Name);
        mb.Entity<PatternEntry>().HasIndex(pe => pe.Author);

        // This stuff should be handled in redis but idk.
        mb.Entity<LikesPatterns>().ToTable("likes_patterns");
        mb.Entity<LikesPatterns>().HasKey(upl => new { upl.UserUID, upl.PatternEntryId });
        mb.Entity<LikesPatterns>().HasIndex(upl => upl.UserUID);
        mb.Entity<LikesPatterns>().HasIndex(upl => upl.PatternEntryId);
        mb.Entity<LikesLoci>().ToTable("likes_loci");
        mb.Entity<LikesLoci>().HasKey(uml => new { uml.UserUID, uml.LociStatusId });
        mb.Entity<LikesLoci>().HasIndex(uml => uml.UserUID);
        mb.Entity<LikesLoci>().HasIndex(uml => uml.LociStatusId);

        mb.Entity<User>().ToTable("users");
        mb.Entity<GlobalPermissions>().ToTable("user_global_permissions");
        mb.Entity<GlobalPermissions>().HasKey(c => c.UserUID);
        mb.Entity<GlobalPermissions>().HasIndex(c => c.UserUID);
        mb.Entity<UserHardcoreState>().ToTable("user_hardcore_state");
        mb.Entity<UserHardcoreState>().HasKey(c => c.UserUID);
        mb.Entity<UserHardcoreState>().HasIndex(c => c.UserUID);

        // Revise this profile data shortly.
        mb.Entity<UserProfile>().ToTable("user_profile_data");
        mb.Entity<UserProfile>().HasOne(c => c.CollarData).WithOne().HasForeignKey<UserProfile>(c => c.UserUID).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<UserProfile>().HasKey(c => c.UserUID);
        mb.Entity<UserProfile>().HasIndex(c => c.UserUID);

        // Really wish that this was done better but I am a slave to my old work.
        mb.Entity<UserAchievementData>().ToTable("user_achievement_data");
        mb.Entity<UserAchievementData>().HasIndex(c => c.UserUID);

        mb.Entity<ReportedUserProfile>().ToTable("reported_profiles");
        mb.Entity<ReportedUserProfile>().HasIndex(c => c.ReportedUserUID);
        mb.Entity<ReportedUserProfile>().HasIndex(c => c.ReportingUserUID);
        mb.Entity<ReportedChat>().ToTable("reported_chats");
        mb.Entity<ReportedChat>().HasIndex(c => c.Chat);
        mb.Entity<ReportedChat>().HasIndex(c => c.ChatLogId);
        mb.Entity<ReportedChat>().HasIndex(c => c.ReporterUID);

        // Kinda wish i could revise this but idk anymore.
        mb.Entity<UserGagData>().ToTable("user_gag_data");
        mb.Entity<UserGagData>().HasKey(u => new { u.UserUID, u.Layer });
        mb.Entity<UserGagData>().HasIndex(u => new { u.UserUID, u.Layer }).IsUnique(); // Ensures no duplicates for UserUID + Layer
        mb.Entity<UserRestrictionData>().ToTable("user_restriction_data");
        mb.Entity<UserRestrictionData>().HasKey(u => new { u.UserUID, u.Layer });
        mb.Entity<UserRestrictionData>().HasIndex(u => new { u.UserUID, u.Layer }).IsUnique(); // Ensures no duplicates for UserUID + Layer
        mb.Entity<UserRestraintData>().ToTable("user_restraintset_data");
        mb.Entity<UserRestraintData>().HasIndex(u => u.UserUID);
        // Collars can have one or more owners, that link back to the collar's UserUID, via the foreign key CollaredUserUID.
        mb.Entity<UserCollarData>().ToTable("user_collar_data");
        mb.Entity<UserCollarData>().HasMany(c => c.Owners).WithOne(o => o.CollaredUserData).HasForeignKey(o => o.CollaredUserUID);
        mb.Entity<UserCollarData>().HasKey(c => c.UserUID);
        mb.Entity<UserCollarData>().HasIndex(c => c.UserUID);
    }
}