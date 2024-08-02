using GagspeakShared.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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

    
    /* Tables that structure the toybox Private Room System */
    public DbSet<PrivateRoom> PrivateRooms { get; set; } // The set of created private rooms.
    public DbSet<PrivateRoomPair> PrivateRoomPairs { get; set; } // users who exist in a particular private room.

    
    /*  Tables that handle what defines a user, their profile, the information, and settings associated with them.    */
    public DbSet<User> Users { get; set; } // Reflects a User profile. UID, last login time, timestamp of creation, alias, and vanity tier are defined here.
    public DbSet<UserGlobalPermissions> UserGlobalPermissions { get; set; } // permissions that when changed are globally modified
    public DbSet<UserGagAppearanceData> UserAppearanceData { get; set; } // appearance data should be stored server side, as even when offline, it should display to your profile data, or be accessible to be viewed.
    public DbSet<UserActiveStateData> UserActiveStateData { get; set; } // contains generic info about the user's current state that should be stored in the database for reference.
    public DbSet<UserProfileData> UserProfileData { get; set; } // every user has a profile associated with them, this contains information unique to the profile.
    public DbSet<UserProfileDataReport> UserProfileReports { get; set; } // Holds info about reported profiles for assistancts to overview.

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
        modelBuilder.Entity<UserActiveStateData>().ToTable("user_active_state_data");
        modelBuilder.Entity<UserActiveStateData>().HasKey(c => c.UserUID);
        modelBuilder.Entity<UserProfileData>().ToTable("user_profile_data");
        modelBuilder.Entity<UserProfileData>().HasKey(c => c.UserUID);
        modelBuilder.Entity<UserProfileDataReport>().ToTable("user_profile_data_reports");
    }
}