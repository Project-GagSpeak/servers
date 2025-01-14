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

    /*  Tables responsible for handling account authentication(validation) and account claiming(verification).   */
    public DbSet<Auth> Auth { get; set; }
    public DbSet<AccountClaimAuth> AccountClaimAuth { get; set; }

    /*  Tables handling bans and ban management.    */
    public DbSet<BannedRegistrations> BannedRegistrations { get; set; }
    public DbSet<Banned> BannedUsers { get; set; }

    /*  Tables that handle what defines a client pair, the permissions associated with them, and the settings associated with them.    */
    public DbSet<ClientPair> ClientPairs { get; set; } // a table storing all of the pairs a client has made.
    public DbSet<ClientPairPermissions> ClientPairPermissions { get; set; } // the unique permissions a user has for each of their client pairs.
    public DbSet<ClientPairPermissionAccess> ClientPairPermissionAccess { get; set; } // determines what permissions the client pair can change on the client.

    /* Format used to go from no-sided pairing to two-sided pairing directly. */
    public DbSet<KinksterRequest> KinksterPairRequests { get; set; }

    /* Tables that structure the toybox Private Room System */
    public DbSet<PrivateRoom> PrivateRooms { get; set; } // The set of created private rooms.
    public DbSet<PrivateRoomPair> PrivateRoomPairs { get; set; } // users who exist in a particular private room.

    /* Tables for the Share Hubs */
    public DbSet<PatternEntry> Patterns { get; set; } // Toybox Pattern Sharing
    public DbSet<MoodleStatus> Moodles { get; set; } // Moodle Sharing
    public DbSet<PatternKeyword> PatternKeywords { get; set; } // Linker between tags and Patterns.
    public DbSet<MoodleKeyword> MoodleKeywords { get; set; } // Linker between tags and Moodles.
    public DbSet<Keyword> Keywords { get; set; } // the tags that can be associated with the patterns.
    public DbSet<LikesPatterns> LikesPatterns { get; set; } // tracks the patterns a user has liked.
    public DbSet<LikesMoodles> LikesMoodles { get; set; } // tracks the moodles a user has liked.

    /*  Tables that handle what defines a user, their profile, the information, and settings associated with them.    */
    public DbSet<User> Users { get; set; } // Reflects a User profile. UID, last login time, timestamp of creation, alias, and vanity tier are defined here.
    public DbSet<UserGlobalPermissions> UserGlobalPermissions { get; set; } // permissions that when changed are globally modified
    public DbSet<UserGagAppearanceData> UserAppearanceData { get; set; } // appearance data should be stored server side, as even when offline, it should display to your profile data, or be accessible to be viewed.
    public DbSet<UserActiveSetData> UserActiveSetData { get; set; } // contains generic info about the user's current state that should be stored in the database for reference.
    public DbSet<UserAchievementData> UserAchievementData { get; set; } // tracks the achievements a user has unlocked.
    public DbSet<UserProfileData> UserProfileData { get; set; } // every user has a profile associated with them, this contains information unique to the profile.
    public DbSet<UserProfileDataReport> UserProfileReports { get; set; } // Holds info about reported profiles for assistants to overview.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Auth>().ToTable("auth");
        modelBuilder.Entity<AccountClaimAuth>().ToTable("account_claim_auth");
        modelBuilder.Entity<BannedRegistrations>().ToTable("banned_registrations");
        modelBuilder.Entity<Banned>().ToTable("banned_users");
        
        modelBuilder.Entity<ClientPair>().ToTable("client_pairs");
        modelBuilder.Entity<ClientPair>().HasKey(u => new { u.UserUID, u.OtherUserUID });
        modelBuilder.Entity<ClientPair>().HasIndex(c => c.UserUID);
        modelBuilder.Entity<ClientPair>().HasIndex(c => c.OtherUserUID);
        modelBuilder.Entity<ClientPairPermissions>().ToTable("client_pair_permissions");
        modelBuilder.Entity<ClientPairPermissions>().HasKey(u => new { u.UserUID, u.OtherUserUID });
        modelBuilder.Entity<ClientPairPermissions>().HasIndex(c => c.UserUID);
        modelBuilder.Entity<ClientPairPermissions>().HasIndex(c => c.OtherUserUID);
        modelBuilder.Entity<ClientPairPermissionAccess>().ToTable("client_pair_permissions_access");
        modelBuilder.Entity<ClientPairPermissionAccess>().HasKey(u => new { u.UserUID, u.OtherUserUID });
        modelBuilder.Entity<ClientPairPermissionAccess>().HasIndex(c => c.UserUID);
        modelBuilder.Entity<ClientPairPermissionAccess>().HasIndex(c => c.OtherUserUID);
        
        modelBuilder.Entity<KinksterRequest>().ToTable("kinkster_pair_requests");
        modelBuilder.Entity<KinksterRequest>().HasKey(u => new { u.UserUID, u.OtherUserUID });
        modelBuilder.Entity<KinksterRequest>().HasIndex(c => c.UserUID);
        modelBuilder.Entity<KinksterRequest>().HasIndex(c => c.OtherUserUID);

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
        modelBuilder.Entity<LikesMoodles>().ToTable("likes_moodles");
        modelBuilder.Entity<LikesMoodles>().HasKey(uml => new { uml.UserUID, uml.MoodleStatusId });

        modelBuilder.Entity<PrivateRoom>().ToTable("private_rooms"); // Key == RoomName
        modelBuilder.Entity<PrivateRoom>().HasIndex(c => c.NameID).IsUnique(true); // insure only one room of the same ID can exist.
        modelBuilder.Entity<PrivateRoom>().HasIndex(c => c.HostUID); // index by the room host when searching for searching hosts.
        modelBuilder.Entity<PrivateRoomPair>().ToTable("private_room_users");
        modelBuilder.Entity<PrivateRoomPair>().HasKey(u => new { u.PrivateRoomNameID, u.PrivateRoomUserUID });
        modelBuilder.Entity<PrivateRoomPair>().HasIndex(c => c.PrivateRoomNameID);
        modelBuilder.Entity<PrivateRoomPair>().HasIndex(c => c.PrivateRoomUserUID);

        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<UserGlobalPermissions>().ToTable("user_global_permissions");
        modelBuilder.Entity<UserGlobalPermissions>().HasKey(c => c.UserUID);
        modelBuilder.Entity<UserGagAppearanceData>().ToTable("user_appearance_data");
        modelBuilder.Entity<UserGagAppearanceData>().HasKey(c => c.UserUID);
        modelBuilder.Entity<UserActiveSetData>().ToTable("user_active_state_data");
        modelBuilder.Entity<UserActiveSetData>().HasKey(c => c.UserUID);
        modelBuilder.Entity<UserAchievementData>().ToTable("user_achievement_data");
        modelBuilder.Entity<UserAchievementData>().HasKey(c => c.UserUID);
        modelBuilder.Entity<UserProfileData>().ToTable("user_profile_data");
        modelBuilder.Entity<UserProfileData>().HasKey(c => c.UserUID);
        modelBuilder.Entity<UserProfileDataReport>().ToTable("user_profile_data_reports");
    }
}