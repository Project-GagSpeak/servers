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

        optionsBuilder.UseNpgsql("Host=167.71.108.254;Port=5432;Database=gagspeak;Username=ckgagspeak", builder =>
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
    public DbSet<PairRequest> PairRequests { get; set; }
    public DbSet<CollaringRequest> CollarRequests { get; set; }

    // ShareHub related tables.
    public DbSet<PatternEntry> Patterns { get; set; } // Toybox Pattern Sharing
    public DbSet<MoodleStatus> Moodles { get; set; } // Moodle Sharing
    public DbSet<PatternKeyword> PatternKeywords { get; set; } // Linker between tags and Patterns.
    public DbSet<MoodleKeyword> MoodleKeywords { get; set; } // Linker between tags and Moodles.
    public DbSet<Keyword> Keywords { get; set; } // the tags that can be associated with the patterns.
    public DbSet<LikesPatterns> LikesPatterns { get; set; } // tracks the patterns a user has liked.
    public DbSet<LikesMoodles> LikesMoodles { get; set; } // tracks the moodles a user has liked.

    // Reporting
    public DbSet<ReportedProfile> ReportedProfiles { get; set; } // Holds info about reported profiles for assistants to overview.
    public DbSet<ReportedChat> ReportedChats { get; set; }
    // User Information
    public DbSet<User> Users { get; set; }
    public DbSet<GlobalPermissions> GlobalPermissions { get; set; }
    public DbSet<HardcoreState> HardcoreState { get; set; }
    public DbSet<UserProfileData> ProfileData { get; set; }
    public DbSet<UserAchievementData> AchievementData { get; set; } // tracks the achievements a user has unlocked.

    // User Active State Data
    public DbSet<UserGagData> ActiveGagData { get; set; }
    public DbSet<UserRestrictionData> ActiveRestrictionData { get; set; }
    public DbSet<UserRestraintData> ActiveRestraintData { get; set; }
    public DbSet<UserCollarData> ActiveCollarData { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountClaimAuth>().ToTable("account_claim_auth");
        modelBuilder.Entity<Auth>().ToTable("auth");
        modelBuilder.Entity<Auth>().HasIndex(a => a.UserUID);
        modelBuilder.Entity<Auth>().HasIndex(a => a.PrimaryUserUID);
        modelBuilder.Entity<AccountReputation>().ToTable("account_reputation");

        modelBuilder.Entity<Banned>().ToTable("banned_users");
        modelBuilder.Entity<BannedRegistrations>().ToTable("banned_registrations");
        
        modelBuilder.Entity<ClientPair>().ToTable("client_pairs");
        modelBuilder.Entity<ClientPair>().HasKey(u => new { u.UserUID, u.OtherUserUID });
        modelBuilder.Entity<ClientPair>().HasIndex(c => c.UserUID);
        modelBuilder.Entity<ClientPair>().HasIndex(c => c.OtherUserUID);
        modelBuilder.Entity<PairPermissions>().ToTable("client_pair_permissions");
        modelBuilder.Entity<PairPermissions>().HasKey(u => new { u.UserUID, u.OtherUserUID });
        modelBuilder.Entity<PairPermissions>().HasIndex(c => c.UserUID);
        modelBuilder.Entity<PairPermissions>().HasIndex(c => c.OtherUserUID);
        modelBuilder.Entity<PairPermissionAccess>().ToTable("client_pair_permissions_access");
        modelBuilder.Entity<PairPermissionAccess>().HasKey(u => new { u.UserUID, u.OtherUserUID });
        modelBuilder.Entity<PairPermissionAccess>().HasIndex(c => c.UserUID);
        modelBuilder.Entity<PairPermissionAccess>().HasIndex(c => c.OtherUserUID);
        modelBuilder.Entity<CollarOwner>().ToTable("collar_owner");
        modelBuilder.Entity<CollarOwner>().HasKey(u => new { u.OwnerUID, u.CollaredUserUID });
        modelBuilder.Entity<CollarOwner>().HasIndex(c => c.OwnerUID);
        modelBuilder.Entity<CollarOwner>().HasIndex(c => c.CollaredUserUID);

        modelBuilder.Entity<PairRequest>().ToTable("kinkster_pair_requests");
        modelBuilder.Entity<PairRequest>().HasKey(u => new { u.UserUID, u.OtherUserUID });
        modelBuilder.Entity<PairRequest>().HasIndex(c => c.UserUID);
        modelBuilder.Entity<PairRequest>().HasIndex(c => c.OtherUserUID);
        modelBuilder.Entity<CollaringRequest>().ToTable("kinkster_collar_requests");
        modelBuilder.Entity<CollaringRequest>().HasKey(u => new { u.UserUID, u.OtherUserUID });
        modelBuilder.Entity<CollaringRequest>().HasIndex(c => c.UserUID);
        modelBuilder.Entity<CollaringRequest>().HasIndex(c => c.OtherUserUID);

        // should probably rework this at some point
        modelBuilder.Entity<Keyword>().ToTable("keywords");
        modelBuilder.Entity<Keyword>().HasIndex(c => c.Word).IsUnique();

        modelBuilder.Entity<PatternKeyword>().ToTable("pattern_keywords");
        modelBuilder.Entity<PatternKeyword>().HasKey(pk => new { pk.PatternEntryId, pk.KeywordWord });
        modelBuilder.Entity<PatternKeyword>().HasOne(pk => pk.PatternEntry).WithMany(pe => pe.PatternKeywords).HasForeignKey(pk => pk.PatternEntryId);
        modelBuilder.Entity<PatternKeyword>().HasOne(pk => pk.Keyword).WithMany(k => k.PatternKeywords).HasForeignKey(pk => pk.KeywordWord);
        modelBuilder.Entity<PatternKeyword>().HasIndex(c => c.PatternEntryId);
        modelBuilder.Entity<PatternKeyword>().HasIndex(c => c.KeywordWord);
        modelBuilder.Entity<MoodleKeyword>().ToTable("moodle_keywords");
        modelBuilder.Entity<MoodleKeyword>().HasKey(mk => new { mk.MoodleStatusId, mk.KeywordWord });
        modelBuilder.Entity<MoodleKeyword>().HasOne(mk => mk.MoodleStatus).WithMany(ms => ms.MoodleKeywords).HasForeignKey(mk => mk.MoodleStatusId);
        modelBuilder.Entity<MoodleKeyword>().HasOne(mk => mk.Keyword).WithMany(k => k.MoodleKeywords).HasForeignKey(mk => mk.KeywordWord);
        modelBuilder.Entity<MoodleKeyword>().HasIndex(c => c.MoodleStatusId);
        modelBuilder.Entity<MoodleKeyword>().HasIndex(c => c.KeywordWord);

        modelBuilder.Entity<MoodleStatus>().ToTable("moodle_status");
        modelBuilder.Entity<MoodleStatus>().HasKey(ms => ms.Identifier);
        modelBuilder.Entity<MoodleStatus>().HasIndex(ms => ms.Title);
        modelBuilder.Entity<MoodleStatus>().HasIndex(ms => ms.Author);
        modelBuilder.Entity<PatternEntry>().ToTable("pattern_entry");
        modelBuilder.Entity<PatternEntry>().HasKey(pe => pe.Identifier);
        modelBuilder.Entity<PatternEntry>().HasIndex(pe => pe.Name);
        modelBuilder.Entity<PatternEntry>().HasIndex(pe => pe.Author);

        modelBuilder.Entity<LikesPatterns>().ToTable("likes_patterns");
        modelBuilder.Entity<LikesPatterns>().HasKey(upl => new { upl.UserUID, upl.PatternEntryId });
        modelBuilder.Entity<LikesPatterns>().HasIndex(upl => upl.UserUID);
        modelBuilder.Entity<LikesPatterns>().HasIndex(upl => upl.PatternEntryId);
        modelBuilder.Entity<LikesMoodles>().ToTable("likes_moodles");
        modelBuilder.Entity<LikesMoodles>().HasKey(uml => new { uml.UserUID, uml.MoodleStatusId });
        modelBuilder.Entity<LikesMoodles>().HasIndex(uml => uml.UserUID);
        modelBuilder.Entity<LikesMoodles>().HasIndex(uml => uml.MoodleStatusId);

        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<GlobalPermissions>().ToTable("user_global_permissions");
        modelBuilder.Entity<GlobalPermissions>().HasKey(c => c.UserUID);
        modelBuilder.Entity<GlobalPermissions>().HasIndex(c => c.UserUID);
        modelBuilder.Entity<HardcoreState>().ToTable("user_hardcore_state");
        modelBuilder.Entity<HardcoreState>().HasKey(c => c.UserUID);
        modelBuilder.Entity<HardcoreState>().HasIndex(c => c.UserUID);
        modelBuilder.Entity<UserProfileData>().ToTable("user_profile_data");
        modelBuilder.Entity<UserProfileData>().HasOne(c => c.CollarData).WithOne().HasForeignKey<UserProfileData>(c => c.UserUID).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UserProfileData>().HasKey(c => c.UserUID);
        modelBuilder.Entity<UserProfileData>().HasIndex(c => c.UserUID);
        modelBuilder.Entity<UserAchievementData>().ToTable("user_achievement_data");
        modelBuilder.Entity<UserAchievementData>().HasIndex(c => c.UserUID);

        modelBuilder.Entity<ReportedProfile>().ToTable("reported_profiles");
        modelBuilder.Entity<ReportedChat>().ToTable("reported_chats");

        modelBuilder.Entity<UserGagData>().ToTable("user_gag_data");
        modelBuilder.Entity<UserGagData>().HasKey(u => new { u.UserUID, u.Layer });
        modelBuilder.Entity<UserGagData>().HasIndex(u => new { u.UserUID, u.Layer }).IsUnique(); // Ensures no duplicates for UserUID + Layer
        modelBuilder.Entity<UserRestrictionData>().ToTable("user_restriction_data");
        modelBuilder.Entity<UserRestrictionData>().HasKey(u => new { u.UserUID, u.Layer });
        modelBuilder.Entity<UserRestrictionData>().HasIndex(u => new { u.UserUID, u.Layer }).IsUnique(); // Ensures no duplicates for UserUID + Layer
        modelBuilder.Entity<UserRestraintData>().ToTable("user_restraintset_data");
        modelBuilder.Entity<UserRestraintData>().HasIndex(u => u.UserUID);
        // Collars can have one or more owners, that link back to the collar's UserUID, via the foreign key CollaredUserUID.
        modelBuilder.Entity<UserCollarData>().ToTable("user_collar_data");
        modelBuilder.Entity<UserCollarData>().HasMany(c => c.Owners).WithOne(o => o.CollaredUserData).HasForeignKey(o => o.CollaredUserUID);
        modelBuilder.Entity<UserCollarData>().HasKey(c => c.UserUID);
        modelBuilder.Entity<UserCollarData>().HasIndex(c => c.UserUID);
    }
}