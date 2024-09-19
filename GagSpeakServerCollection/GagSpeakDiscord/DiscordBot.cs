
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Enums;
using GagspeakDiscord.Commands;
using GagspeakDiscord.Modules.AccountWizard;
using GagspeakDiscord.Modules.KinkDispenser;
using GagspeakServer.Hubs;
using GagspeakShared.Data;
using GagspeakShared.Models;
using GagspeakShared.Services;
using GagspeakShared.Utils.Configuration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Globalization;
using System.Text;

namespace GagspeakDiscord;

internal partial class DiscordBot : IHostedService
{
    private readonly DiscordBotServices _botServices;                               // The class for the discord bot services
    private readonly IConfigurationService<DiscordConfiguration> _discordConfigService;    // The configuration service for the discord bot
    private readonly IConnectionMultiplexer _connectionMultiplexer;                 // the connection multiplexer for the discord bot
    private readonly DiscordSocketClient _discordClient;                            // the discord client socket
    private readonly ILogger<DiscordBot> _logger;                                   // the logger for the discord bot
    private readonly IHubContext<GagspeakHub> _gagspeakHubContext;                  // the hub context for the gagspeak hub
    private readonly IServiceProvider _services;                                    // the service provider for the discord bot
    private InteractionService _interactionModule;                                  // the interaction module for the discord bot
    private CancellationTokenSource? _processReportQueueCts;                        // the CTS for the process report queues
    private CancellationTokenSource? _updateStatusCts;                              // the CTS for the update status
    private CancellationTokenSource? _vanityUpdateCts;                              // the CTS for the vanity update
    private CancellationTokenSource? _vanityAddUsersCts;                            // the CTS for the vanity add users

    public DiscordBot(DiscordBotServices botServices, IServiceProvider services, IConfigurationService<DiscordConfiguration> configuration,
        IHubContext<GagspeakHub> gagspeakHubContext, ILogger<DiscordBot> logger, IConnectionMultiplexer connectionMultiplexer)
    {
        _botServices = botServices;
        _services = services;
        _discordConfigService = configuration;
        _gagspeakHubContext = gagspeakHubContext;
        _logger = logger;
        _connectionMultiplexer = connectionMultiplexer;
        // Create a new discord client with the default retry mode
        _discordClient = new(new DiscordSocketConfig()
        {
            DefaultRetryMode = RetryMode.AlwaysRetry
        });

        // subscribe to the log event from discord.
        _discordClient.Log += Log;
    }

    // the main startup function, this will run whenever the discord bot is turned on
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // get the discord bot token from the configuration
        var token = _discordConfigService.GetValueOrDefault(nameof(DiscordConfiguration.DiscordBotToken), string.Empty);
        // if the token is not empty
        if (!string.IsNullOrEmpty(token))
        {
            // log the information that the discord bot is starting
            _logger.LogInformation("Starting DiscordBot");
            _logger.LogInformation("Using Configuration: " + _discordConfigService.ToString());

            // create a new interaction module for the discord bot
            _interactionModule = new InteractionService(_discordClient);

            // subscribe to the log event from the interaction module
            _interactionModule.Log += Log;

            // next we will want to add the modules to the interaction module.
            // These will be responsible for generating the UID's
            // (at least for now, that purpose will change later)
            await _interactionModule.AddModuleAsync(typeof(GagspeakCommands), _services).ConfigureAwait(false);
            await _interactionModule.AddModuleAsync(typeof(AccountWizard), _services).ConfigureAwait(false);
            await _interactionModule.AddModuleAsync(typeof(KinkDispenser), _services).ConfigureAwait(false);

            // log the bot into to the discord client with the token
            await _discordClient.LoginAsync(TokenType.Bot, token).ConfigureAwait(false);
            // start the discord client
            await _discordClient.StartAsync().ConfigureAwait(false);

            // subscribe to the ready event from the discord client
            _discordClient.Ready += DiscordClient_Ready;

            // subscribe to the interaction created event from the discord client (occurs when player interacts with its posted events.)
            _discordClient.InteractionCreated += async (x) =>
            {
                var ctx = new SocketInteractionContext(_discordClient, x);
                await _interactionModule.ExecuteCommandAsync(ctx, _services).ConfigureAwait(false);
            };

            // start the bot services
            await _botServices.Start().ConfigureAwait(false);

            // update the status of the bot
            _ = UpdateStatusAsync();
        }
    }

    /// <summary>
    /// Stops the discord bot
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // if the discord bot token is not empty
        if (!string.IsNullOrEmpty(_discordConfigService.GetValueOrDefault(nameof(DiscordConfiguration.DiscordBotToken), string.Empty)))
        {
            // await for all bot services to stop
            await _botServices.Stop().ConfigureAwait(false);

            // cancel the process report queue CTS, and all other CTS tokens
            _processReportQueueCts?.Cancel();
            _updateStatusCts?.Cancel();
            _vanityUpdateCts?.Cancel();

            // await for the bot to logout
            await _discordClient.LogoutAsync().ConfigureAwait(false);
            // await for the bot to stop
            await _discordClient.StopAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The ready event for the discord client
    /// </summary>
    private async Task DiscordClient_Ready()
    {
        var guilds = await _discordClient.Rest.GetGuildsAsync().ConfigureAwait(false);
        foreach (var guild in guilds)
        {
            await _interactionModule.RegisterCommandsToGuildAsync(guild.Id, true).ConfigureAwait(false);
            await CreateOrUpdateModal(guild).ConfigureAwait(false);
            _ = UpdateVanityRoles(guild);
            _ = AddPerksToUsersWithVanityRole(guild);
            _ = RemovePerksFromUsersNotInVanityRole();
            _ = ProcessReportsQueue(guild);
        }
    }

    /// <summary> The primary function for creating / updating the account claim system </summary>
    private async Task CreateOrUpdateModal(RestGuild guild)
    {
        // log that we are creating the account management system channel
        _logger.LogDebug("Account Management Wizard: Getting Channel");

        // fetch the channel for the account management system message
        var discordChannelForCommands = _discordConfigService.GetValue<ulong?>(nameof(DiscordConfiguration.DiscordChannelForCommands));
        if (discordChannelForCommands == null)
        {
            _logger.LogWarning("Account Management Wizard: No channel configured");
            return;
        }

        // create the message
        IUserMessage? message = null;
        var socketchannel = await _discordClient.GetChannelAsync(discordChannelForCommands.Value).ConfigureAwait(false) as SocketTextChannel;
        var pinnedMessages = await socketchannel.GetPinnedMessagesAsync().ConfigureAwait(false);
        // check if the message is already pinned
        foreach (var msg in pinnedMessages)
        {
            // log the information that we are checking the message id
            _logger.LogDebug("Account Management Wizard: Checking message id {id}, author is: {author}, hasEmbeds: {embeds}", msg.Id, msg.Author.Id, msg.Embeds.Any());
            // if the author of the post is the bot, and the message has embeds
            if (msg.Author.Id == _discordClient.CurrentUser.Id && msg.Embeds.Any())
            {
                // then get the message 
                message = await socketchannel.GetMessageAsync(msg.Id).ConfigureAwait(false) as IUserMessage;
                break;
            }
        }

        // and log it.
        _logger.LogInformation("Account Management Wizard: Found message id: {id}", message?.Id ?? 0);

        // then generate our accountWizard message in the channel we just inspected
        await GenerateOrUpdateWizardMessage(socketchannel, message).ConfigureAwait(false);
    }

    /// <summary> The primary account management wizard for the discord. Nessisary for claiming accounts </summary>
    private async Task GenerateOrUpdateWizardMessage(SocketTextChannel channel, IUserMessage? prevMessage)
    {
        // construct the embed builder
        EmbedBuilder eb = new EmbedBuilder();
        eb.WithTitle("Official CK GagSpeak Bot Service");
        eb.WithDescription("Press \"Start\" to interact with me!" + Environment.NewLine + Environment.NewLine
            + "You can handle all of your GagSpeak account needs in this server.\nJust follow the instructions!");
        eb.WithThumbnailUrl("https://raw.githubusercontent.com/CordeliaMist/GagSpeak-Client/main/images/iconUI.png");
        // construct the buttons
        var cb = new ComponentBuilder();
        // this claim your account button will trigger the customid of wizard-home:true, letting the bot deliever a personalized reply
        // that will display the account information.
        cb.WithButton("Start GagSpeak Account Management", style: ButtonStyle.Primary, customId: "wizard-home:true", emote: Emoji.Parse("🎀"));
        // if the previous message is null
        if (prevMessage == null)
        {
            // send the message to the channel
            var msg = await channel.SendMessageAsync(embed: eb.Build(), components: cb.Build()).ConfigureAwait(false);
            try
            {
                // pin the message
                await msg.PinAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // swallow
            }
        }
        else // if message is already generated, just modify it.
        {
            await prevMessage.ModifyAsync(p =>
            {
                p.Embed = eb.Build();
                p.Components = cb.Build();
            }).ConfigureAwait(false);
        }
        // once the button is pressed, it should display the main menu to the user.
    }

    /// <summary> Translate discords log messages into our logger format </summary>
    private Task Log(LogMessage msg)
    {
        switch (msg.Severity)
        {
            case LogSeverity.Critical:
            case LogSeverity.Error:
                _logger.LogError(msg.Exception, msg.Message); break;
            case LogSeverity.Warning:
                _logger.LogWarning(msg.Exception, msg.Message); break;
            default:
                _logger.LogInformation(msg.Message); break;
        }

        return Task.CompletedTask;
    }

    /// <summary> Processes the reports queue </summary>
    private async Task ProcessReportsQueue(RestGuild guild)
    {
        // reset the CTS
        _processReportQueueCts?.Cancel();
        _processReportQueueCts?.Dispose();
        _processReportQueueCts = new();
        var token = _processReportQueueCts.Token;

        // while the token is not cancelled,
        while (!token.IsCancellationRequested)
        {
            // wait for 30 minutes
            await Task.Delay(TimeSpan.FromMinutes(30)).ConfigureAwait(false);

            // if the discord client is not connected, continue to next cycle.
            if (_discordClient.ConnectionState != ConnectionState.Connected) continue;

            // otherwise grab our channel report ID
            var reportChannelId = _discordConfigService.GetValue<ulong?>(nameof(DiscordConfiguration.DiscordChannelForReports));
            if (reportChannelId == null) continue;

            try
            {
                // within the scope of the service provider, execute actions using the GagSpeak DbContext
                using (var scope = _services.CreateScope())
                {
                    _logger.LogInformation("Checking for Profile Reports");
                    var dbContext = scope.ServiceProvider.GetRequiredService<GagspeakDbContext>();
                    if (!dbContext.UserProfileReports.Any()) {
                        continue; // continue is no Profile Reports are found
                    }

                    // collect the list of profile reports otherwise and get the report channel
                    var reports = await dbContext.UserProfileReports.ToListAsync().ConfigureAwait(false);
                    var restChannel = await guild.GetTextChannelAsync(reportChannelId.Value).ConfigureAwait(false);

                    // for each report, generate an embed and send it to the report channel
                    foreach (var report in reports)
                    {
                        // get the user who reported
                        var reportedUser = await dbContext.Users.SingleAsync(u => u.UID == report.ReportedUserUID).ConfigureAwait(false);
                        var reportedUserAccountClaim = await dbContext.AccountClaimAuth.SingleOrDefaultAsync(u => u.User.UID == report.ReportedUserUID).ConfigureAwait(false);

                        // get the user who was reported
                        var reportingUser = await dbContext.Users.SingleAsync(u => u.UID == report.ReportingUserUID).ConfigureAwait(false);
                        var reportingUserAccountClaim = await dbContext.AccountClaimAuth.SingleOrDefaultAsync(u => u.User.UID == report.ReportingUserUID).ConfigureAwait(false);
                        
                        // get the profile data of the reported user.
                        var reportedUserProfile = await dbContext.UserProfileData.SingleAsync(u => u.UserUID == report.ReportedUserUID).ConfigureAwait(false);


                        // create an embed post to display reported profiles.
                        EmbedBuilder eb = new();
                        eb.WithTitle("GagSpeak Profile Report");

                        StringBuilder reportedUserSb = new();
                        StringBuilder reportingUserSb = new();
                        reportedUserSb.Append(reportedUser.UID);
                        reportingUserSb.Append(reportingUser.UID);
                        if (reportedUserAccountClaim != null)
                        {
                            reportedUserSb.AppendLine($" (<@{reportedUserAccountClaim.DiscordId}>)");
                        }
                        if (reportingUserAccountClaim != null)
                        {
                            reportingUserSb.AppendLine($" (<@{reportingUserAccountClaim.DiscordId}>)");
                        }
                        eb.AddField("Reported User", reportedUserSb.ToString());
                        eb.AddField("Reporting User", reportingUserSb.ToString());
                        var reportTimeUtc = new DateTimeOffset(report.ReportTime, TimeSpan.Zero);
                        var formattedTimestamp = string.Create(CultureInfo.InvariantCulture, $"<t:{reportTimeUtc.ToUnixTimeSeconds()}:F>");
                        eb.AddField("Report Time (UTC)", report.ReportTime);
                        eb.AddField("Report Time (Local)", formattedTimestamp);

                        eb.AddField("Report Reason", string.IsNullOrWhiteSpace(report.ReportReason) ? "-" : report.ReportReason);
                        eb.AddField("Reported User Profile Description", string.IsNullOrWhiteSpace(reportedUserProfile.UserDescription) ? "-" : reportedUserProfile.UserDescription);
                        eb.AddField("Reported User Profile Current Image vs reported Image", "Reported Image is shown below");

                        var cb = new ComponentBuilder();
                        cb.WithButton("Dismiss Report", customId: $"gagspeak-report-button-dismissreport-{reportedUser.UID}", style: ButtonStyle.Primary);
                        cb.WithButton("Clear Profile Image", customId: $"gagspeak-report-button-clearprofileimage-{reportedUser.UID}", style: ButtonStyle.Secondary);
                        cb.WithButton("Ban Profile Access", customId: $"gagspeak-report-button-banprofile-{reportedUser.UID}", style: ButtonStyle.Secondary);
                        cb.WithButton("Ban User", customId: $"gagspeak-report-button-banuser-{reportedUser.UID}", style: ButtonStyle.Danger);
                        cb.WithButton("Dismiss & Flag Reporting User", customId: $"gagspeak-report-button-flagreporter-{reportedUser.UID}-{reportingUser.UID}", style: ButtonStyle.Danger);
                        
                        // Create a list for FileAttachments
                        var attachments = new List<FileAttachment>();

                        // Conditionally add the current profile image
                        if (!string.IsNullOrEmpty(reportedUserProfile.Base64ProfilePic))
                        {
                            var currentImageFileName = reportedUser.UID + "_profile_current_" + Guid.NewGuid().ToString("N") + ".png";
                            using var currentImageStream = new MemoryStream(Convert.FromBase64String(reportedUserProfile.Base64ProfilePic));
                            var currentImageAttachment = new FileAttachment(currentImageStream, currentImageFileName);
                            attachments.Add(currentImageAttachment);

                            // Update embed image URL if current image is available
                            eb.WithImageUrl($"attachment://{currentImageFileName}");
                        }

                        // Conditionally add the reported image
                        if (!string.IsNullOrEmpty(report.ReportedBase64Picture))
                        {
                            var reportedImageFileName = reportedUser.UID + "_profile_reported_" + Guid.NewGuid().ToString("N") + ".png";
                            using var reportedImageStream = new MemoryStream(Convert.FromBase64String(report.ReportedBase64Picture));
                            var reportedImageAttachment = new FileAttachment(reportedImageStream, reportedImageFileName);
                            attachments.Add(reportedImageAttachment);

                            // Optionally, you can add another embed image for the reported picture
                            eb.WithImageUrl($"attachment://{reportedImageFileName}");
                        }

                        // Send files if there are any attachments
                        if (attachments.Count > 0)
                        {
                            await restChannel.SendFilesAsync(
                                attachments,
                                text: "User Report",
                                embed: eb.Build(),
                                components: cb.Build()
                            ).ConfigureAwait(false);
                        }
                        else
                        {
                            // If no attachments, send the message with only the embed and components
                            await restChannel.SendMessageAsync(embed: eb.Build(), components: cb.Build()).ConfigureAwait(false);
                        }

                        // remove the report from the dbcontext now that it has been processed by the server.
                        dbContext.Remove(report);
                    }

                    await dbContext.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process reports");
            }
        }
    }

    /// <summary>
    /// Updates the vanity roles in the concurrent dictionary for the bot services to reflect the list in the appsettings.json
    /// </summary>
    private async Task UpdateVanityRoles(RestGuild guild)
    {
        // while the update status CTS is not requested
        while (!_updateStatusCts.IsCancellationRequested)
        {
            try
            {
                // begin to update the vanity roles. 
                _logger.LogInformation("Updating Vanity Roles From Config File");
                // fetch the roles from the configuration list.
                Dictionary<ulong, string> vanityRoles = _discordConfigService.GetValueOrDefault(nameof(DiscordConfiguration.VanityRoles), new Dictionary<ulong, string>());
                // if the vanity roles are not the same as the list fetched from the bot service,
                if (vanityRoles.Keys.Count != _botServices.VanityRoles.Count)
                {
                    // clear the roles in the bot service, and create a new list
                    _botServices.VanityRoles.Clear();
                    // for each role in the list of roles
                    foreach (var role in vanityRoles)
                    {
                        _logger.LogDebug("Adding Role: {id} => {desc}", role.Key, role.Value);
                        // add the ID and the name of the role to the bot service.
                        var restrole = guild.GetRole(role.Key);
                        if (restrole != null) _botServices.VanityRoles.Add(restrole, role.Value);
                    }
                }

                // schedule this task every 5 minutes. (since i dont think we will need it often or ever change it.
                await Task.Delay(TimeSpan.FromHours(6), _updateStatusCts.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during UpdateVanityRoles");
            }
        }
    }

    /// <summary>
    /// Helps assign vanity perks to any users with an appropriate vanity role, and assigns them the perks.
    /// </summary>
    private async Task AddPerksToUsersWithVanityRole(RestGuild CKrestGuild)
    {
        _vanityAddUsersCts?.Cancel();
        _vanityAddUsersCts?.Dispose();
        _vanityAddUsersCts = new();
        var token = _vanityAddUsersCts.Token;

        // while the cancellation token is not requested
        while (!token.IsCancellationRequested)
        {
            // try and clean up the vanity UID's from people no longer Supporting CK
            try
            {
                _logger.LogInformation("Adding VanityRoles to Active Supporters of CK");

                // get the list of allowed roles that should have vanity UID's from the Vanity Roles in the discord configuration.
                Dictionary<ulong, string> allowedRoleIds = _discordConfigService.GetValueOrDefault(nameof(DiscordConfiguration.VanityRoles), new Dictionary<ulong, string>());

                // await the creation of a scope for this service
                await using var scope = _services.CreateAsyncScope();
                // fetch the gagspeakDatabaseConext from the database
                await using (var db = scope.ServiceProvider.GetRequiredService<GagspeakDbContext>())
                {
                    // Create a dictionary to map role names to CkSupporterTier values
                    var roleNameToTier = new Dictionary<string, CkSupporterTier>
                    {
                        { "Kinkporium Mistress", CkSupporterTier.KinkporiumMistress },
                        { "Distinguished Connoisseur", CkSupporterTier.DistinguishedConnoisseur },
                        { "Esteemed Patron", CkSupporterTier.EsteemedPatron },
                        { "Server Booster", CkSupporterTier.ServerBooster },
                        { "Illustrious Supporter", CkSupporterTier.IllustriousSupporter }
                    };

                    // narrow the list down to only the users with valid accounts with no active role.
                    var validClaimedAccounts = await db.AccountClaimAuth.Include("User")
                        .Where(c => c.StartedAt != DateTime.MinValue && c.User != null && c.User.VanityTier == CkSupporterTier.NoRole)
                        .ToListAsync().ConfigureAwait(false);

                    // Check to see if any valid accounts currently have any discord roles.
                    foreach (var validAccount in validClaimedAccounts)
                    {
                        // grab the discord user.
                        var discordUser = await CKrestGuild.GetUserAsync(validAccount.DiscordId).ConfigureAwait(false);

                        // check to see if the user has any of the roles.
                        if (discordUser != null && discordUser.RoleIds.Any(u => allowedRoleIds.Keys.Contains(u)))
                        {
                            // fetch the roles they have, and output them.
                            _logger.LogInformation($"User {validAccount.User.UID} has roles: {string.Join(", ", discordUser.RoleIds)}");
                            // Determine the highest priority role
                            var highestRole = discordUser.RoleIds
                                .Where(roleId => allowedRoleIds.ContainsKey(roleId))
                                .Select(roleId => roleNameToTier[allowedRoleIds[roleId]])
                                .OrderByDescending(tier => tier)
                                .FirstOrDefault();

                            // Assign the highest priority role
                            validAccount.User.VanityTier = highestRole;
                            _logger.LogInformation($"User {validAccount.User.UID} assigned to tier {highestRole} (highest role)");

                            // Update the primary user in the DB
                            db.Update(validAccount.User);

                            // Locate any secondary users of the primary user this account belongs to, and clear the perks from these as well.
                            var secondaryUsers = await db.Auth.Include(u => u.User).Where(u => u.PrimaryUserUID == validAccount.User.UID).ToListAsync().ConfigureAwait(false);
                            foreach (var secondaryUser in secondaryUsers)
                            {
                                _logger.LogDebug($"Secondary User {secondaryUser.User.UID} assigned to tier {highestRole} (highest role)");
                                secondaryUser.User.VanityTier = highestRole;
                                // Update the secondary user in the database
                                db.Update(secondaryUser.User);
                            }
                        }
                        // await for the database to save changes
                        await db.SaveChangesAsync().ConfigureAwait(false);
                        // await a second before checking the next user
                        await Task.Delay(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something failed during checking vanity user UID's");
            }

            // log the completition, and execute it again in 12 hours.
            _logger.LogInformation("Supporter Perks for UID's Assigned");
            await Task.Delay(TimeSpan.FromMinutes(30), _vanityAddUsersCts.Token).ConfigureAwait(false);
        }

    }

    /// <summary> 
    /// Removes the VanityPerks from users who are no longer supporting CK 
    /// </summary>
    private async Task RemovePerksFromUsersNotInVanityRole()
    {
        // refresh the CTS for our vanity updates.
        _vanityUpdateCts?.Cancel();
        _vanityUpdateCts?.Dispose();
        _vanityUpdateCts = new();
        // set the token to the token
        var token = _vanityUpdateCts.Token;
        // set the guild to the guild ID of Cordy's Kinkporium
        var restGuild = await _discordClient.Rest.GetGuildAsync(878511238764720129);
        // set the application ID to the application ID of the bot
        var appId = await _discordClient.GetApplicationInfoAsync().ConfigureAwait(false);

        // while the cancellation token is not requested
        while (!token.IsCancellationRequested)
        {
            // try and clean up the vanity UID's from people no longer Supporting CK
            try
            {
                _logger.LogInformation("Cleaning up Vanity UIDs from guild {guildName}", restGuild.Name);

                // get the list of allowed roles that should have vanity UID's from the Vanity Roles in the discord configuration.
                Dictionary<ulong, string> allowedRoleIds = _discordConfigService.GetValueOrDefault(nameof(DiscordConfiguration.VanityRoles), new Dictionary<ulong, string>());
                // display the list of allowed role ID's
                _logger.LogDebug($"Allowed role ids: {string.Join(", ", allowedRoleIds)}");

                // if there are not any allowed roles for vanity perks, output it.
                if (!allowedRoleIds.Any())
                {
                    _logger.LogInformation("No roles for command defined, no cleanup performed");
                }
                // otherwise, handle it.
                else
                {
                    // await the creation of a scope for this service
                    await using var scope = _services.CreateAsyncScope();
                    // fetch the gagspeakDatabaseConext from the database
                    await using (var db = scope.ServiceProvider.GetRequiredService<GagspeakDbContext>())
                    {
                        // search the database under the AccountClaimAuth table where a user is included...
                        var aliasedUsers = await db.AccountClaimAuth.Include("User")
                            // where the user is not null and the alias is not null (they have an alias)
                            .Where(c => c.User != null && !string.IsNullOrEmpty(c.User.Alias))
                            // arranged into a list
                            .ToListAsync().ConfigureAwait(false);

                        // then, for each of the aliased users in the list...
                        foreach (var accountClaimAuth in aliasedUsers)
                        {
                            // fetch the discord user they belong to by grabbing the discord ID from the accountClaimAuth
                            var discordUser = await restGuild.GetUserAsync(accountClaimAuth.DiscordId).ConfigureAwait(false);
                            _logger.LogInformation($"Checking User: {accountClaimAuth.DiscordId}, {accountClaimAuth.User.UID} " +
                            $"({accountClaimAuth.User.Alias}), User in Roles: {string.Join(", ", discordUser?.RoleIds ?? new List<ulong>())}");

                            // if the discord user no longer exists, or no longer has any of the allowed role ID's for these benifits....
                            if (discordUser == null || !discordUser.RoleIds.Any(u => allowedRoleIds.Keys.Contains(u)))
                            {
                                // we should clear the user's alias "and their other vanity benifits, but add those later)
                                _logger.LogInformation($"User {accountClaimAuth.User.UID} not in allowed roles, deleting alias");
                                accountClaimAuth.User.Alias = null;
                                accountClaimAuth.User.VanityTier = CkSupporterTier.NoRole;

                                // locate any secondary user's of the primary user this account belongs to, and clear the perks from these as well.
                                var secondaryUsers = await db.Auth.Include(u => u.User).Where(u => u.PrimaryUserUID == accountClaimAuth.User.UID).ToListAsync().ConfigureAwait(false);
                                foreach (var secondaryUser in secondaryUsers)
                                {
                                    _logger.LogDebug($"Secondary User {secondaryUser.User.UID} not in allowed roles, deleting alias & resetting supporter tier");
                                    secondaryUser.User.Alias = null;
                                    secondaryUser.User.VanityTier = CkSupporterTier.NoRole;
                                    // update the secondary user in the database
                                    db.Update(secondaryUser.User);
                                }
                                // update the primary user in the DB
                                db.Update(accountClaimAuth.User);
                            }

                            // await for the database to save changes
                            await db.SaveChangesAsync().ConfigureAwait(false);
                            // await a second before checking the next user
                            await Task.Delay(1000);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something failed during checking vanity user UID's");
            }

            // log the completition, and execute it again in 12 hours.
            _logger.LogInformation("Vanity UID cleanup complete");
            await Task.Delay(TimeSpan.FromHours(12), _vanityUpdateCts.Token).ConfigureAwait(false);
        }
    }

    /// <summary> Updates the status of the bot at the interval </summary>
    private async Task UpdateStatusAsync()
    {
        _updateStatusCts = new();
        while (!_updateStatusCts.IsCancellationRequested)
        {
            // grab the endpoint from the connection multiplexer
            var endPoint = _connectionMultiplexer.GetEndPoints().First();
            // fetch the total number of online users connected to the redis server
            var onlineUsers = await _connectionMultiplexer.GetServer(endPoint).KeysAsync(pattern: "GagspeakHub:UID:*").CountAsync();
            var toyboxUsers = await _connectionMultiplexer.GetServer(endPoint).KeysAsync(pattern: "ToyboxHub:UID:*").CountAsync();

            // log the status
            _logger.LogTrace("Kinksters online: {onlineUsers}", onlineUsers);
            _logger.LogTrace("Toybox users online: {toyboxUsers}", toyboxUsers);
            // change the activity
            await _discordClient.SetActivityAsync(new Game("with " + onlineUsers + " Kinksters")).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
        }
    }
}