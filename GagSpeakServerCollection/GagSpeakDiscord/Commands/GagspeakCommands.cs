using Discord;
using Discord.Interactions;
using GagspeakAPI.Enums;
using GagspeakShared.Data;
using GagspeakShared.Models;
using GagspeakShared.Services;
using GagspeakShared.Utils;
using GagspeakShared.Utils.Configuration;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Data;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using DiscordConfig = GagspeakShared.Utils.Configuration.DiscordConfig;

namespace GagspeakDiscord.Commands;
#nullable enable
#pragma warning disable MA0004
#pragma warning disable CS8602
public class GagspeakCommands : InteractionModuleBase
{
    private readonly DiscordBotServices _botServices;
    private readonly ServerTokenGenerator _serverTokenGenerator;                   // the server token generator
    private readonly ILogger<GagspeakCommands> _logger;                               // the logger for the GagspeakCommands
    private readonly IServiceProvider _services;                                    // our service provider
    private readonly IConfigurationService<DiscordConfig> _discordConfigService;    // the discord configuration service
    private readonly IConnectionMultiplexer _connectionMultiplexer;                 // the connection multiplexer for the discord bot

    public GagspeakCommands(DiscordBotServices botServices, ServerTokenGenerator tokenGenerator, 
        ILogger<GagspeakCommands> logger, IServiceProvider services,
        IConfigurationService<DiscordConfig> gagspeakDiscordConfiguration,
        IConnectionMultiplexer connectionMultiplexer)
    {
        _botServices = botServices;
        _serverTokenGenerator = tokenGenerator;
        _logger = logger;
        _services = services;
        _discordConfigService = gagspeakDiscordConfiguration;
        _connectionMultiplexer = connectionMultiplexer;
    }

    // the menu displayed when the user types /userinfo, should be only allows for admins
    [SlashCommand("userinfo", "Shows you your user information")]
    public async Task UserInfo([Summary("secondary_uid", "(Optional) Your secondary UID")] string? secondaryUid = null,
        [Summary("discord_user", "ADMIN ONLY: Discord User to check for")] IUser? discordUser = null,
        [Summary("uid", "ADMIN ONLY: UID to check for")] string? uid = null)
    {
        // log the used slash command
        _logger.LogInformation("SlashCommand:{userId}:{Method}",
            Context.Interaction.User.Id, nameof(UserInfo));

        // try to get the user information
        try
        {
            EmbedBuilder eb = new();

            eb = await HandleUserInfo(eb, Context.User.Id, secondaryUid, discordUser?.Id ?? null, uid);

            await RespondAsync(embeds: new[] { eb.Build() }, ephemeral: true).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            EmbedBuilder eb = new();
            eb.WithTitle("An error occured");
            eb.WithDescription("Please report this error to bug-reports: " + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine);

            await RespondAsync(embeds: new Embed[] { eb.Build() }, ephemeral: true).ConfigureAwait(false);
        }
    }

    // admin only command for adding a user to the database (manually)
    [SlashCommand("unban", "Unbans a user from GagSpeak services.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task UserUnban([Summary("desired_uid", "Desired UID")] string desiredUid)
    {
        try
        {
            Embed embed = await HandleUserUnban(desiredUid);

            await RespondAsync(embeds: new[] { embed }, ephemeral: true).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            EmbedBuilder eb = new();
            eb.WithTitle("An error occured");
            eb.WithDescription("Please report this error to bug-reports: " + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine);

            await RespondAsync(embeds: new Embed[] { eb.Build() }, ephemeral: true).ConfigureAwait(false);
        }
    }


    // admin only process reports poll queue.
    [SlashCommand("fetchreports", "Manually process the reports queue and reset the timer.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task ProcessReports()
    {
        try
        {
            _logger.LogInformation("Processing Reports Queue! " + Context.Guild.Name);
            // Create a new CTS for the manual process
            using (CancellationTokenSource manualCts = new CancellationTokenSource())
            {
                // Call the process reports queue with the manual token
                await _botServices.ProcessReports(Context.User, manualCts.Token);
            }
            await RespondAsync("Reports queue processed and timer reset.", ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process reports queue");
            await RespondAsync("Failed to process reports queue: " + ex.ToString(), ephemeral: true);
        }
    }

    // admin only command for sending a message to clients connected to the gagspeak service.
    [SlashCommand("message", "ADMIN ONLY: sends a message to clients")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task SendMessageToClients([Summary("message", "Message to send")] string message,
        [Summary("severity", "Severity of the message")] MessageSeverity messageType = MessageSeverity.Information,
        [Summary("uid", "User ID to the person to send the message to")] string? uid = null)
    {
        // log the slash command information
        _logger.LogInformation("SlashCommand:{userId}:{Method}:{message}:{type}:{uid}", Context.Interaction.User.Id, nameof(SendMessageToClients), message, messageType, uid);

        // get the database scope
        using IServiceScope scope = _services.CreateScope();
        using GagspeakDbContext? db = scope.ServiceProvider.GetService<GagspeakDbContext>();

        // if the spesified uid doesnt exist then tell the user and return
        if (!string.IsNullOrEmpty(uid) && !await db.Users.AnyAsync(u => u.UID == uid))
        {
            await RespondAsync("Specified UID does not exist", ephemeral: true).ConfigureAwait(false);
            return;
        }

        // This block of code is responsible for sending a message
        try
        {
            // An HttpClient is created to send a POST request to a specific URI
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _serverTokenGenerator.Token);

            ClientMessage payload = new ClientMessage(messageType, message, uid ?? string.Empty);
            string jsonPayload = JsonSerializer.Serialize(payload);
            _logger.LogInformation("Sending message to {uri} with payload: {jsonPayload}", new Uri(_discordConfigService.GetValue<Uri>(nameof(DiscordConfig.MainServerAddress)), "/msgc/sendMessage"), jsonPayload);

            using HttpResponseMessage response = await client.PostAsJsonAsync(new Uri(_discordConfigService.GetValue<Uri>(nameof(DiscordConfig.MainServerAddress)), "/msgc/sendMessage"), payload).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                ulong? discordChannelForMessages = _discordConfigService.GetValueOrDefault<ulong?>(nameof(DiscordConfig.DiscordChannelForMessages), null);
                if (uid is null && discordChannelForMessages != null)
                {
                    IMessageChannel? discordChannel = await Context.Guild.GetChannelAsync(884567637529604117) as IMessageChannel;
                    if (discordChannel != null)
                    {
                        Color embedColor = messageType switch
                        {
                            MessageSeverity.Information => Color.Blue,
                            MessageSeverity.Warning => new Color(255, 255, 0),
                            MessageSeverity.Error => Color.Red,
                            _ => Color.Blue
                        };

                        EmbedBuilder eb = new();
                        eb.WithTitle(messageType + " server message");
                        eb.WithColor(embedColor);
                        eb.WithDescription(message);

                        await discordChannel.SendMessageAsync(embed: eb.Build());
                    }
                }

                await RespondAsync("Message sent", ephemeral: true).ConfigureAwait(false);
            }
            else
            {
                string errorResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _logger.LogError("Failed to send message. Status Code: {StatusCode}, Response: {Response}", response.StatusCode, errorResponse);
                await RespondAsync("Failed to send message: " + errorResponse, ephemeral: true).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            // If an exception occurs, a response is sent indicating that the message failed to be sent
            _logger.LogError(ex, "Failed to send message");
            await RespondAsync("Failed to send message: " + ex.ToString(), ephemeral: true).ConfigureAwait(false);
        }
    }

    [SlashCommand("purge", "ADMIN ONLY: purges users from the database made past the specified time")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task PurgeUsersCommand(
        [Summary("timeFrame", "Use d for days, h for hours, or say now to clear all")] string timeFrame)
    {
        TimeSpan purgeTimeSpan;
        await Context.Interaction.DeferAsync();
        if (timeFrame.Equals("now", StringComparison.OrdinalIgnoreCase))
        {
            purgeTimeSpan = TimeSpan.Zero;
        }
        else if (timeFrame.EndsWith("d", StringComparison.OrdinalIgnoreCase))
        {
            int days = int.Parse(timeFrame.TrimEnd('d', 'D'), CultureInfo.InvariantCulture);
            purgeTimeSpan = TimeSpan.FromDays(days);
        }
        else if (timeFrame.EndsWith("h", StringComparison.OrdinalIgnoreCase))
        {
            int hours = int.Parse(timeFrame.TrimEnd('h', 'H'), CultureInfo.InvariantCulture);
            purgeTimeSpan = TimeSpan.FromHours(hours);
        }
        else
        {
            await FollowupAsync("Invalid time frame. Please specify in days (e.g., '7d') or hours (e.g., '24h').");
            return;
        }

        // Assuming you have a way to get the dbContext and call the PurgeUnusedAccounts method
        using IServiceScope scope = _services.CreateScope();
        GagspeakDbContext? db = scope.ServiceProvider.GetService<GagspeakDbContext>();
        // create a list of all users from the users table whose LastLoggedIn time was greater than time current time - timespan
        var users = await db.Users.Where(u => u.LastLogin < DateTime.UtcNow - purgeTimeSpan).ToListAsync();
        // purge the user profiles from the database
        foreach(var user in users)
            await SharedDbFunctions.DeleteUserProfile(user, _logger, db).ConfigureAwait(false);
        
        await FollowupAsync($"Purge completed for users inactive for {timeFrame}.");
    }

    [SlashCommand("forcereconnect", "ADMIN ONLY: forcibly reconnects all online connected clients")]
    public async Task ForceReconnectOnlineUsers([Summary("message", "Message to send with reconnection notification")] string message,
    [Summary("severity", "Severity of the message")] MessageSeverity messageType = MessageSeverity.Information,
    [Summary("uid", "UserUID we force reconnection on.")] string uid = "")

    {
        _logger.LogInformation("SlashCommand:{userId}:{Method}:{message}:{type}:{uid}", Context.Interaction.User.Id, nameof(ForceReconnectOnlineUsers), message, messageType, uid);

        IGuildUser user = await Context.Guild.GetUserAsync(Context.User.Id);
        bool isAdminOrOwner = user.GuildPermissions.Administrator || user.GuildPermissions.ManageGuild;

        using IServiceScope scope = _services.CreateScope();
        using GagspeakDbContext? db = scope.ServiceProvider.GetService<GagspeakDbContext>();

        if (!isAdminOrOwner)
        {
            await RespondAsync("You do not have permission to use this command.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        try
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _serverTokenGenerator.Token);

            HardReconnectMessage payload = new HardReconnectMessage(messageType, message, ServerState.ForcedReconnect, uid);
            string jsonPayload = JsonSerializer.Serialize(payload);
            _logger.LogInformation("Sending message to {uri} with payload: {jsonPayload}", 
                new Uri(_discordConfigService.GetValue<Uri>(nameof(DiscordConfig.MainServerAddress)), "/msgc/forceHardReconnect"), jsonPayload);

            using HttpResponseMessage response = await client.PostAsJsonAsync(new Uri(_discordConfigService.GetValue<Uri>(nameof(DiscordConfig.MainServerAddress)), "/msgc/forceHardReconnect"), payload).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                ulong? discordChannelForMessages = _discordConfigService.GetValueOrDefault<ulong?>(nameof(DiscordConfig.DiscordChannelForMessages), null);
                if (discordChannelForMessages != null)
                {
                    IMessageChannel? discordChannel = await Context.Guild.GetChannelAsync(884567637529604117) as IMessageChannel;
                    if (discordChannel != null)
                    {
                        Color embedColor = messageType switch
                        {
                            MessageSeverity.Information => Color.Blue,
                            MessageSeverity.Warning => new Color(255, 255, 0),
                            MessageSeverity.Error => Color.Red,
                            _ => Color.Blue
                        };

                        EmbedBuilder eb = new();
                        eb.WithTitle("Force Reconnecting Players");
                        eb.WithColor(embedColor);
                        eb.WithDescription(message);

                        await discordChannel.SendMessageAsync(embed: eb.Build());
                    }
                }

                await RespondAsync("Forced Hard Reconnection to all Online Users", ephemeral: true).ConfigureAwait(false);
            }
            else
            {
                string errorResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _logger.LogError("Failed to send message. Status Code: {StatusCode}, Response: {Response}", response.StatusCode, errorResponse);
                await RespondAsync("Failed to send message: " + errorResponse, ephemeral: true).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            await RespondAsync("Failed to force reconnection message: " + ex.ToString(), ephemeral: true).ConfigureAwait(false);
        }
    }

    [SlashCommand("updateroles", "Updates roles for users with supporter roles")]
    public async Task UpdateRoles()
    {
        // log the used slash command
        _logger.LogInformation("SlashCommand:{userId}:{Method}", Context.Interaction.User.Id, nameof(UpdateRoles));
        // Check if the user has the "Assistant Role" or is an admin or owner
        IGuildUser user = await Context.Guild.GetUserAsync(Context.User.Id);
        IRole? assistantRole = Context.Guild.Roles.FirstOrDefault(r => string.Equals(r.Name, "Assistant", StringComparison.Ordinal));
        bool isAdminOrOwner = user.GuildPermissions.Administrator || user.GuildPermissions.ManageGuild;
        if ((assistantRole is null || !user.RoleIds.Contains(assistantRole.Id)) && !isAdminOrOwner)
        {
            await RespondAsync("You do not have permission to use this command.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        // Get the roles to check for
        IRole? tier3 = Context.Guild.Roles.FirstOrDefault(r => string.Equals(r.Name, "Distinguished Connoisseur", StringComparison.Ordinal));
        IRole? tier2 = Context.Guild.Roles.FirstOrDefault(r => string.Equals(r.Name, "Esteemed Patron", StringComparison.Ordinal));
        IRole? tier1 = Context.Guild.Roles.FirstOrDefault(r => string.Equals(r.Name, "Illustrious Supporter", StringComparison.Ordinal));
        IRole? booster = Context.Guild.Roles.FirstOrDefault(r => string.Equals(r.Name, "Server Booster", StringComparison.Ordinal));
        IRole? contributor = Context.Guild.Roles.FirstOrDefault(r => string.Equals(r.Name, "Contributor", StringComparison.Ordinal));
        if (tier3 is null || tier2 is null || tier1 is null || booster is null || contributor is null)
        {
            await RespondAsync("One or more roles do not exist.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        // Immediately respond with a deferred message
        await Context.Interaction.DeferAsync();
        // store a counter so we know how many roles we updated
        int rolesUpdated = 0;

        EmbedBuilder eb = new();
        eb.WithTitle($"Updating {rolesUpdated} roles...");
        eb.WithColor(Color.Magenta);
        // respond to the message with an embed letting us know that we are updating the roles
        await FollowupAsync(embed: eb.Build(), ephemeral: true).ConfigureAwait(false);
        IUserMessage resp = await GetOriginalResponseAsync().ConfigureAwait(false);

        // get the list of users.
        IEnumerable<IGuildUser> users = await Context.Guild.GetUsersAsync();

        // Create a list to store the user-role tuples
        List<(string, string)> userRoles = new List<(string, string)>();

        // Iterate over the guild users
        foreach (IGuildUser guildUser in users)
        {
            // If the user has one of the roles and does not have the Contributor role, add the Contributor role
            if ((guildUser.RoleIds.Contains(tier3.Id) || guildUser.RoleIds.Contains(tier2.Id) || guildUser.RoleIds.Contains(tier1.Id) 
                || guildUser.RoleIds.Contains(booster.Id)) && !guildUser.RoleIds.Contains(contributor.Id))
            {
                // Increment the counter
                rolesUpdated++;
                await guildUser.AddRoleAsync(contributor);
                // Update the message
                eb.WithTitle($"Updating {rolesUpdated} roles...");
                // Store the user's name and the role they had that gave them the contributor role in the list
                string? roleNames = guildUser.RoleIds.Select(roleId => Context.Guild.GetRole(roleId).Name)
                    .FirstOrDefault(roleName => roleName.Contains(tier3.Name) || roleName.Contains(tier2.Name) || roleName.Contains(tier1.Name) || roleName.Contains(booster.Name));
                // if role name is null, do not add.
                if (roleNames is null)
                    continue;

                // Use the Nickname if it's not null, otherwise use the Username
                string displayName = guildUser.DisplayName ?? guildUser.Username;
                userRoles.Add((displayName, roleNames));

                await ModifyMessageAsync(eb, resp).ConfigureAwait(false);
            }
        }

        if (rolesUpdated > 0)
        {
            eb.WithDescription("Summary of Added Users:");
            foreach ((string username, string roleName) in userRoles)
                eb.AddField($"{username}", $"Role for Contributor: __{roleName}__");
        }

        eb.WithTitle($"Update Complete! Updated {rolesUpdated} roles!");

        await ModifyMessageAsync(eb, resp).ConfigureAwait(false);
    }

    public async Task ModifyMessageAsync(EmbedBuilder eb, IUserMessage message)
    {
        await message.ModifyAsync(msg => msg.Embed = eb.Build());
    }

    // This method is responsible for adding a user
    public async Task<Embed> HandleUserUnban(string desiredUid)
    {
        // An EmbedBuilder is created to build the embed message
        EmbedBuilder embed = new EmbedBuilder();

        using IServiceScope scope = _services.CreateScope();
        using GagspeakDbContext? db = scope.ServiceProvider.GetService<GagspeakDbContext>();

        // locate the auth first, as it is linked to registered and unregistered players.
        if (await db.Auth.Include(a => a.AccountRep).Include(a => a.User).SingleOrDefaultAsync(a => a.User.UID == desiredUid).ConfigureAwait(false) is not { } auth)
        {
            // The embed message is updated to indicate that the user already exists in the database
            embed.WithTitle("Failed to add user");
            embed.WithDescription("Already in Database");
            return embed.Build();
        }

        auth.AccountRep.IsBanned = false;

        // Grab the banned user associated with this UID
        if (await db.BannedUsers.SingleOrDefaultAsync(u => u.UserUID == desiredUid).ConfigureAwait(false) is { } bannedEntry)
            db.BannedUsers.Remove(bannedEntry);

        // Remove the banned registration if it exists
        if (await db.AccountClaimAuth.SingleOrDefaultAsync(u => u.User.UID == auth.UserUID).ConfigureAwait(false) is { } claimAuth)
        {
            if (await db.BannedRegistrations.SingleOrDefaultAsync(u => u.DiscordId == claimAuth.DiscordId.ToString()).ConfigureAwait(false) is { } bannedRegistration)
                db.BannedRegistrations.Remove(bannedRegistration);
        }

        // Modify the profile data.
        if (await db.ProfileData.SingleAsync(u => u.UserUID == auth.UserUID).ConfigureAwait(false) is { } profile)
        {
            profile.FlaggedForReport = false;
            profile.ProfileDisabled = false;
            profile.Description = string.Empty;
        }

        // update all tables and save changes.
        await db.SaveChangesAsync();

        // The embed message is updated to indicate that the user was successfully added
        embed.WithTitle("Successfully Unbanned User");
        embed.WithDescription(desiredUid);

        // The embed message is built and returned
        return embed.Build();
    }

    // Reform this at some point, it can be heavily optimized now.
    private async Task<EmbedBuilder> HandleUserInfo(EmbedBuilder eb, ulong id, string? secondaryUserUid = null, ulong? optionalUser = null, string? uid = null)
    {
        // Check if a secondary user UID was provided
        bool showForSecondaryUser = secondaryUserUid != null;

        // Create a new scope for the service provider.
        using IServiceScope scope = _services.CreateScope();

        // Get the required service from the service provider.
        await using GagspeakDbContext db = scope.ServiceProvider.GetRequiredService<GagspeakDbContext>();

        // Fetch the primary user from the database.
        AccountClaimAuth? primaryUser = await db.AccountClaimAuth.Include(u => u.User).SingleOrDefaultAsync(u => u.DiscordId == id).ConfigureAwait(false);

        // Set the user to check for Discord ID as the provided ID.
        ulong userToCheckForDiscordId = id;

        // If the primary user is null, set the title and description of the embed builder and return it.
        if (primaryUser is null)
        {
            eb.WithTitle("No account");
            eb.WithDescription("No GagSpeak account was found associated to your Discord user");
            return eb;
        }

        // If an optional user or UID was provided and the primary user is not an admin or moderator, set the title and description of the embed builder and return it.
        if ((optionalUser != null || uid != null))
        {
            eb.WithTitle("Unauthorized");
            eb.WithDescription("You are not authorized to view another users' information");
            return eb;
        }
        // If an optional user or UID was provided and the primary user is an admin or moderator, fetch the user from the database.
        else if ((optionalUser != null || uid != null))
        {
            AccountClaimAuth? userInDb = null;
            if (optionalUser != null)
            {
                userInDb = await db.AccountClaimAuth.Include(u => u.User).SingleOrDefaultAsync(u => u.DiscordId == optionalUser).ConfigureAwait(false);
            }
            else if (uid != null)
            {
                userInDb = await db.AccountClaimAuth.Include(u => u.User).SingleOrDefaultAsync(u => u.User.UID == uid || u.User.Alias == uid).ConfigureAwait(false);
            }

            // If the user in the database is null, set the title and description of the embed builder and return it.
            if (userInDb is null)
            {
                eb.WithTitle("No account");
                eb.WithDescription("The Discord user has no valid GagSpeak account");
                return eb;
            }

            // Set the user to check for Discord ID as the Discord ID of the user in the database.
            userToCheckForDiscordId = userInDb.DiscordId;
        }

        // Fetch the lodestone user from the database.
        AccountClaimAuth? lodestoneUser = await db.AccountClaimAuth.Include(u => u.User).SingleOrDefaultAsync(u => u.DiscordId == userToCheckForDiscordId).ConfigureAwait(false);
        User? dbUser = lodestoneUser.User;

        // If a secondary user UID was provided, fetch the user from the database.
        if (showForSecondaryUser)
        {
            dbUser = (await db.Auth.Include(u => u.User).SingleOrDefaultAsync(u => u.PrimaryUserUID == dbUser.UID && u.UserUID == secondaryUserUid))?.User;
            if (dbUser is null)
            {
                eb.WithTitle("No such secondary UID");
                eb.WithDescription($"A secondary UID {secondaryUserUid} was not found attached to your primary UID {primaryUser.User.UID}.");
                return eb;
            }
        }

        // Fetch the auth from the database.
        Auth? auth = await db.Auth.Include(u => u.PrimaryUser).SingleOrDefaultAsync(u => u.UserUID == dbUser.UID).ConfigureAwait(false);

        // Fetch the identity from the database.
        RedisValue identity = await _connectionMultiplexer.GetDatabase().StringGetAsync("GagspeakHub:UID:" + dbUser.UID).ConfigureAwait(false);

        // Set the title and description of the embed builder.
        eb.WithTitle("User Information");
        eb.WithDescription("This is the user information for Discord User <@" + userToCheckForDiscordId + ">" + Environment.NewLine + Environment.NewLine
            + "If you want to verify your secret key is valid, go to https://emn178.github.io/online-tools/sha256.html and copy your secret key into there and compare it to the Hashed Secret Key provided below.");
        eb.AddField("UID", dbUser.UID);

        // If the user's alias is not null or empty
        if (!string.IsNullOrEmpty(dbUser.Alias))
        {
            // Add a field to the embed builder with the title "Vanity UID" and the value of the user's alias
            eb.AddField("Vanity UID", dbUser.Alias);
        }

        // If the information is being shown for a secondary user
        if (showForSecondaryUser)
        {
            // Add a field to the embed builder with the title "Primary UID for [User's UID]" and the value of the primary user's UID
            eb.AddField("Primary UID for " + dbUser.UID, auth.PrimaryUserUID);
        }
        else
        {
            // Retrieve a list of secondary UIDs where the primary UID is the user's UID
            var altProfileUids = await db.Auth.AsNoTracking().Where(p => p.PrimaryUserUID == dbUser.UID).Select(p => p.UserUID).ToListAsync();
            if (altProfileUids.Count > 0)
                eb.AddField("Secondary UIDs", string.Join(Environment.NewLine, altProfileUids));
        }

        // Add a field to the embed builder with the title "Last Online (UTC)" and the value of the user's last login time in UTC
        eb.AddField("Last Online (UTC)", dbUser.LastLogin.ToString("U", CultureInfo.InvariantCulture));

        // Add a field to the embed builder with the title "Currently online" and the value of whether the user is currently online
        eb.AddField("Currently online ", !string.IsNullOrEmpty(identity));


        // Add a field to the embed builder with the title "Hashed Secret Key" and the value of the user's hashed secret key
        eb.AddField("Hashed Secret Key", auth.HashedKey);
        // Return the embed builder
        return eb;
    }

#pragma warning restore MA0004
}
