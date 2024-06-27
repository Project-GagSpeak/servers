using GagspeakShared.Models;
using Microsoft.EntityFrameworkCore;

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


    /*  Tables that handle what defines a user, their profile, the information, and settings associated with them.    */
    public DbSet<User> Users { get; set; } // Reflects a User profile. UID, last login time, timestamp of creation, alias, and vanity tier are defined here.
    public DbSet<UserGlobalPermissions> UserGlobalPermissions { get; set; } // permissions that when changed are globally modified
    public DbSet<UserGagAppearanceData> UserAppearanceData { get; set; } // appearance data should be stored server side, as even when offline, it should display to your profile data, or be accessible to be viewed.
    public DbSet<UserProfileData> UserProfileData { get; set; } // every user has a profile associated with them, this contains information unique to the profile.

    /* Information regarding patterns and triggers are sent over DTO's and not stored on the server as it would require too much. 
     * When or if we ever get to alias lists and how we can store them on the client, will get to it when it comes to it */

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
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<UserGlobalPermissions>().ToTable("user_global_permissions");
        modelBuilder.Entity<UserGlobalPermissions>().HasKey(c => c.UserUID);
        modelBuilder.Entity<UserGagAppearanceData>().ToTable("user_appearance_data");
        modelBuilder.Entity<UserGagAppearanceData>().HasKey(c => c.UserUID);
        modelBuilder.Entity<UserProfileData>().ToTable("user_profile_data");
        modelBuilder.Entity<UserProfileData>().HasKey(c => c.UserUID);
    }
}