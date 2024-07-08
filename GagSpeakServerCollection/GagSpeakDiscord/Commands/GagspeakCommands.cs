using Discord;
using Discord.Interactions;
using Gagspeak.API.Data.Enum;
using GagspeakShared.Data;
using GagspeakShared.Models;
using GagspeakShared.Services;
using GagspeakShared.Utils;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace GagspeakDiscord.Commands;
#nullable enable
#pragma warning disable MA0004
#pragma warning disable CS8602
public class GagspeakCommands : InteractionModuleBase
{
    private readonly ILogger<GagspeakCommands> _logger;                               // the logger for the GagspeakCommands
    private readonly IServiceProvider _services;                                    // our service provider
    private readonly IConfigurationService<DiscordConfiguration> _discordConfigService;    // the discord configuration service
    private readonly IConnectionMultiplexer _connectionMultiplexer;                 // the connection multiplexer for the discord bot

    public GagspeakCommands(ILogger<GagspeakCommands> logger, IServiceProvider services,
        IConfigurationService<DiscordConfiguration> gagspeakDiscordConfiguration,
        IConnectionMultiplexer connectionMultiplexer)
    {
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
    [SlashCommand("useradd", "ADMIN ONLY: add a user unconditionally to the Database")]
    public async Task UserAdd([Summary("desired_uid", "Desired UID")] string desiredUid)
    {
        _logger.LogInformation("SlashCommand:{userId}:{Method}:{params}",
            Context.Interaction.User.Id, nameof(UserAdd),
            string.Join(",", new[] { $"{nameof(desiredUid)}:{desiredUid}" }));

        try
        {
            var embed = await HandleUserAdd(desiredUid, Context.User.Id);

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

    // admin only command for sending a message to clients connected to the gagspeak service.
    [SlashCommand("message", "ADMIN ONLY: sends a message to clients")]
    public async Task SendMessageToClients([Summary("message", "Message to send")] string message,
        [Summary("severity", "Severity of the message")] MessageSeverity messageType = MessageSeverity.Information,
        [Summary("uid", "User ID to the person to send the message to")] string? uid = null)
    {
        // log the slash command information
        _logger.LogInformation("SlashCommand:{userId}:{Method}:{message}:{type}:{uid}", Context.Interaction.User.Id, nameof(SendMessageToClients), message, messageType, uid);

        // get the database scope
        using var scope = _services.CreateScope();
        using var db = scope.ServiceProvider.GetService<GagspeakDbContext>();

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
            using HttpClient c = new HttpClient();
            // The POST request is sent asynchronously with a JSON payload containing a new ClientMessage
            await c.PostAsJsonAsync(new Uri(_discordConfigService.GetValue<Uri>
                (nameof(DiscordConfiguration.MainServerAddress)), "/msgc/sendMessage"), new ClientMessage(messageType, message, uid ?? string.Empty))
                .ConfigureAwait(false);

            // The Discord channel for messages is retrieved from the configuration service
            var discordChannelForMessages = _discordConfigService.GetValueOrDefault<ulong?>(nameof(DiscordConfiguration.DiscordChannelForMessages), null);
            // If the user ID is null and a Discord channel for messages exists
            if (uid == null && discordChannelForMessages != null)
            {
                // The context of the channel the command was used in is retrieved to respond in the same channel
                var discordChannel = await Context.Guild.GetChannelAsync(discordChannelForMessages.Value) as IMessageChannel;
                // If the Discord channel exists
                if (discordChannel != null)
                {
                    // The color of the embed message is determined based on the message severity
                    var embedColor = messageType switch
                    {
                        MessageSeverity.Information => Color.Blue,
                        MessageSeverity.Warning => new Color(255, 255, 0),
                        MessageSeverity.Error => Color.Red,
                        _ => Color.Blue
                    };

                    // An EmbedBuilder is created to build the embed message
                    EmbedBuilder eb = new();
                    eb.WithTitle(messageType + " server message");
                    eb.WithColor(embedColor);
                    eb.WithDescription(message);

                    // The embed message is sent to the Discord channel
                    await discordChannel.SendMessageAsync(embed: eb.Build());
                }
            }

            // A response is sent indicating that the message was sent
            await RespondAsync("Message sent", ephemeral: true).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // If an exception occurs, a response is sent indicating that the message failed to be sent
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
            int days = int.Parse(timeFrame.TrimEnd('d', 'D'));
            purgeTimeSpan = TimeSpan.FromDays(days);
        }
        else if (timeFrame.EndsWith("h", StringComparison.OrdinalIgnoreCase))
        {
            int hours = int.Parse(timeFrame.TrimEnd('h', 'H'));
            purgeTimeSpan = TimeSpan.FromHours(hours);
        }
        else
        {
            await FollowupAsync("Invalid time frame. Please specify in days (e.g., '7d') or hours (e.g., '24h').");
            return;
        }

        // Assuming you have a way to get the dbContext and call the PurgeUnusedAccounts method
        using var db = _services.CreateScope().ServiceProvider.GetService<GagspeakDbContext>();
        // create a list of all users from the users table whose LastLoggedIn time was greater than time current time - timespan
        var users = await db.Users.Where(u => u.LastLoggedIn < DateTime.UtcNow - purgeTimeSpan).ToListAsync();

        // purge the users from the database
        foreach(var user in users)
        {
            await SharedDbFunctions.PurgeUser(_logger, user, db);
        }
        await FollowupAsync($"Purge completed for users inactive for {timeFrame}.");
    }



    [SlashCommand("updateroles", "Updates roles for users with 'Distinguished Conisuir', 'Server Booster', or 'Esteemed Patron' roles")]
    public async Task UpdateRoles()
    {
        // log the used slash command
        _logger.LogInformation("SlashCommand:{userId}:{Method}", Context.Interaction.User.Id, nameof(UpdateRoles));
        // Check if the user has the "Assistant Role" or is an admin or owner
        var user = await Context.Guild.GetUserAsync(Context.User.Id);
        var assistantRole = Context.Guild.Roles.FirstOrDefault(r => string.Equals(r.Name, "Assistant", StringComparison.Ordinal));
        var isAdminOrOwner = user.GuildPermissions.Administrator || user.GuildPermissions.ManageGuild;
        if ((assistantRole == null || !user.RoleIds.Contains(assistantRole.Id)) && !isAdminOrOwner)
        {
            await RespondAsync("You do not have permission to use this command.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        // Get the roles to check for
        var distinguishedConnoisseurRole = Context.Guild.Roles.FirstOrDefault(r => r.Name == "Distinguished Connoisseur");
        var esteemedPatreonRole = Context.Guild.Roles.FirstOrDefault(r => r.Name == "Esteemed Patron");
        var illustriousSupporterRole = Context.Guild.Roles.FirstOrDefault(r => r.Name == "Illustrious Supporter");
        var serverBoosterRole = Context.Guild.Roles.FirstOrDefault(r => r.Name == "Server Booster");
        var contributorRole = Context.Guild.Roles.FirstOrDefault(r => r.Name == "Contributor");

        if (distinguishedConnoisseurRole == null || serverBoosterRole == null
        || esteemedPatreonRole == null || illustriousSupporterRole == null || contributorRole == null)
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
        var resp = await GetOriginalResponseAsync().ConfigureAwait(false);

        // get the list of users.
        IEnumerable<IGuildUser> users = await Context.Guild.GetUsersAsync();

        // Create a list to store the user-role tuples
        List<(string, string)> userRoles = new List<(string, string)>();

        // Iterate over the guild users
        foreach (var guildUser in users)
        {
            // If the user has one of the roles and does not have the Contributor role, add the Contributor role
            if ((guildUser.RoleIds.Contains(distinguishedConnoisseurRole.Id) || guildUser.RoleIds.Contains(esteemedPatreonRole.Id)
                || guildUser.RoleIds.Contains(illustriousSupporterRole.Id) || guildUser.RoleIds.Contains(serverBoosterRole.Id))
                && !guildUser.RoleIds.Contains(contributorRole.Id))
            {
                // Increment the counter
                rolesUpdated++;
                await guildUser.AddRoleAsync(contributorRole);
                // Update the message
                eb.WithTitle($"Updating {rolesUpdated} roles...");
                // Store the user's name and the role they had that gave them the contributor role in the list
                var roleNames = guildUser.RoleIds.Select(roleId => Context.Guild.GetRole(roleId).Name)
                    .FirstOrDefault(roleName =>
                        roleName.Contains(distinguishedConnoisseurRole.Name) ||
                        roleName.Contains(esteemedPatreonRole.Name) ||
                        roleName.Contains(illustriousSupporterRole.Name) ||
                        roleName.Contains(serverBoosterRole.Name));

                // Use the Nickname if it's not null, otherwise use the Username
                var displayName = guildUser.DisplayName ?? guildUser.Username;
                userRoles.Add((displayName, roleNames));

                await ModifyMessageAsync(eb, resp).ConfigureAwait(false);
            }
        }

        if (rolesUpdated > 0)
        {
            eb.WithDescription("Summary of Added Users:");
            foreach (var (username, roleName) in userRoles)
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
    public async Task<Embed> HandleUserAdd(string desiredUid, ulong discordUserId)
    {
        // An EmbedBuilder is created to build the embed message
        var embed = new EmbedBuilder();

        // A scope is created to resolve services
        using var scope = _services.CreateScope();
        // The GagspeakDbContext is retrieved from the service provider
        using var db = scope.ServiceProvider.GetService<GagspeakDbContext>();
        // If the user already exists in the database
        if (db.Users.Any(u => u.UID == desiredUid || u.Alias == desiredUid))
        {
            // The embed message is updated to indicate that the user already exists in the database
            embed.WithTitle("Failed to add user");
            embed.WithDescription("Already in Database");
        }
        else
        {
            // A new user is created
            User newUser = new()
            {
                LastLoggedIn = DateTime.UtcNow,
                UID = desiredUid,
                VanityTier = CkSupporterTier.NoRole,
            };

            // A new auth is created with a hashed key
            var computedHash = StringUtils.Sha256String(StringUtils.GenerateRandomString(64) + DateTime.UtcNow.ToString());
            var auth = new Auth()
            {
                HashedKey = StringUtils.Sha256String(computedHash),
                User = newUser,
            };

            // The new user and auth are added to the database
            await db.Users.AddAsync(newUser);
            await db.Auth.AddAsync(auth);

            // The changes are saved to the database
            await db.SaveChangesAsync();

            // The embed message is updated to indicate that the user was successfully added
            embed.WithTitle("Successfully added " + desiredUid);
            embed.WithDescription("Secret Key: " + computedHash);
        }

        // The embed message is built and returned
        return embed.Build();
    }

    private async Task<EmbedBuilder> HandleUserInfo(EmbedBuilder eb, ulong id, string? secondaryUserUid = null, ulong? optionalUser = null, string? uid = null)
    {
        // Check if a secondary user UID was provided
        bool showForSecondaryUser = secondaryUserUid != null;

        // Create a new scope for the service provider.
        using var scope = _services.CreateScope();

        // Get the required service from the service provider.
        await using var db = scope.ServiceProvider.GetRequiredService<GagspeakDbContext>();

        // Fetch the primary user from the database.
        var primaryUser = await db.AccountClaimAuth.Include(u => u.User).SingleOrDefaultAsync(u => u.DiscordId == id).ConfigureAwait(false);

        // Set the user to check for Discord ID as the provided ID.
        ulong userToCheckForDiscordId = id;

        // If the primary user is null, set the title and description of the embed builder and return it.
        if (primaryUser == null)
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
            AccountClaimAuth userInDb = null;
            if (optionalUser != null)
            {
                userInDb = await db.AccountClaimAuth.Include(u => u.User).SingleOrDefaultAsync(u => u.DiscordId == optionalUser).ConfigureAwait(false);
            }
            else if (uid != null)
            {
                userInDb = await db.AccountClaimAuth.Include(u => u.User).SingleOrDefaultAsync(u => u.User.UID == uid || u.User.Alias == uid).ConfigureAwait(false);
            }

            // If the user in the database is null, set the title and description of the embed builder and return it.
            if (userInDb == null)
            {
                eb.WithTitle("No account");
                eb.WithDescription("The Discord user has no valid GagSpeak account");
                return eb;
            }

            // Set the user to check for Discord ID as the Discord ID of the user in the database.
            userToCheckForDiscordId = userInDb.DiscordId;
        }

        // Fetch the lodestone user from the database.
        var lodestoneUser = await db.AccountClaimAuth.Include(u => u.User).SingleOrDefaultAsync(u => u.DiscordId == userToCheckForDiscordId).ConfigureAwait(false);
        var dbUser = lodestoneUser.User;

        // If a secondary user UID was provided, fetch the user from the database.
        if (showForSecondaryUser)
        {
            dbUser = (await db.Auth.Include(u => u.User).SingleOrDefaultAsync(u => u.PrimaryUserUID == dbUser.UID && u.UserUID == secondaryUserUid))?.User;
            if (dbUser == null)
            {
                eb.WithTitle("No such secondary UID");
                eb.WithDescription($"A secondary UID {secondaryUserUid} was not found attached to your primary UID {primaryUser.User.UID}.");
                return eb;
            }
        }

        // Fetch the auth from the database.
        var auth = await db.Auth.Include(u => u.PrimaryUser).SingleOrDefaultAsync(u => u.UserUID == dbUser.UID).ConfigureAwait(false);

        // Fetch the identity from the database.
        var identity = await _connectionMultiplexer.GetDatabase().StringGetAsync("UID:" + dbUser.UID).ConfigureAwait(false);

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
            var secondaryUIDs = await db.Auth.Where(p => p.PrimaryUserUID == dbUser.UID).Select(p => p.UserUID).ToListAsync();

            // If there are any secondary UIDs
            if (secondaryUIDs.Any())
            {
                // Add a field to the embed builder with the title "Secondary UIDs" and the value of the secondary UIDs separated by new lines
                eb.AddField("Secondary UIDs", string.Join(Environment.NewLine, secondaryUIDs));
            }
        }

        // Add a field to the embed builder with the title "Last Online (UTC)" and the value of the user's last login time in UTC
        eb.AddField("Last Online (UTC)", dbUser.LastLoggedIn.ToString("U"));

        // Add a field to the embed builder with the title "Currently online" and the value of whether the user is currently online
        eb.AddField("Currently online ", !string.IsNullOrEmpty(identity));

        // Add a field to the embed builder with the title "Hashed Secret Key" and the value of the user's hashed secret key
        eb.AddField("Hashed Secret Key", auth.HashedKey);

        // Return the embed builder
        return eb;
    }

#pragma warning restore MA0004
}
