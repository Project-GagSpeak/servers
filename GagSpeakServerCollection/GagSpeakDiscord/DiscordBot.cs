
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using GagspeakAPI.Enums;
using GagspeakDiscord.Commands;
using GagspeakDiscord.Modules.AccountWizard;
using GagspeakDiscord.Modules.KinkDispenser;
using GagspeakServer.Hubs;
using GagspeakShared.Data;
using GagspeakShared.Models;
using GagspeakShared.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System;
using System.Diagnostics;
using DiscordConfig = GagspeakShared.Utils.Configuration.DiscordConfig;

namespace GagspeakDiscord;
#nullable enable

internal partial class DiscordBot : IHostedService
{
    private readonly DiscordBotServices _botServices;
    private readonly IConfigurationService<DiscordConfig> _discordConfig;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly DiscordSocketClient _discordClient;
    private readonly ILogger<DiscordBot> _logger;
    private readonly IDbContextFactory<GagspeakDbContext> _dbContextFactory;
    private readonly IHubContext<GagspeakHub> _gagspeakHubContext;
    private readonly IServiceProvider _services;
    private InteractionService _interactionModule;
    private CancellationTokenSource _processReportQueueCts = new();
    private CancellationTokenSource _updateStatusCts = new();

    public DiscordBot(DiscordBotServices botServices, IServiceProvider services, IConfigurationService<GagspeakShared.Utils.Configuration.DiscordConfig> config,
        IDbContextFactory<GagspeakDbContext> dbContext, IHubContext<GagspeakHub> hubContext, ILogger<DiscordBot> logger, 
        IConnectionMultiplexer connectionMultiplexer)
    {
        _botServices = botServices;
        _services = services;
        _discordConfig = config;
        _dbContextFactory = dbContext;
        _gagspeakHubContext = hubContext;
        _logger = logger;
        _connectionMultiplexer = connectionMultiplexer;
        // Create a new discord client with the default retry mode
        _discordClient = new(new DiscordSocketConfig()
        {
            DefaultRetryMode = RetryMode.AlwaysRetry
        });

        _interactionModule = new InteractionService(_discordClient);
        // subscribe to the log event from discord.
        _discordClient.Log += Log;
    }

    // Map role names from both servers
    public static readonly Dictionary<string, CkSupporterTier> RoleToVanityTier = new Dictionary<string, CkSupporterTier>(StringComparer.Ordinal)
    {
        { "Corby", CkSupporterTier.KinkporiumMistress },
        { "Distinguished Connoisseur", CkSupporterTier.DistinguishedConnoisseur },
        { "Esteemed Patron", CkSupporterTier.EsteemedPatron },
        { "Server Booster", CkSupporterTier.ServerBooster },
        { "Illustrious Supporter", CkSupporterTier.IllustriousSupporter },
    };

    // the main startup function, this will run whenever the discord bot is turned on
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // get the discord bot token from the configuration
        string token = _discordConfig.GetValueOrDefault(nameof(DiscordConfig.DiscordBotToken), string.Empty);
        // if the token is not empty
        if (!string.IsNullOrEmpty(token))
        {
            // log the information that the discord bot is starting
            _logger.LogInformation("Starting DiscordBot");

            // Recreate the new interaction service for this client.
            _interactionModule?.Dispose();
            _interactionModule = new InteractionService(_discordClient);
            _interactionModule.Log += Log;

            // Append our modules to the interaction Module
            await _interactionModule.AddModuleAsync(typeof(GagspeakCommands), _services).ConfigureAwait(false);
            await _interactionModule.AddModuleAsync(typeof(AccountWizard), _services).ConfigureAwait(false);
            await _interactionModule.AddModuleAsync(typeof(KinkDispenser), _services).ConfigureAwait(false);

            // log the bot into to the discord client with the token
            await _discordClient.LoginAsync(TokenType.Bot, token).ConfigureAwait(false);
            await _discordClient.StartAsync().ConfigureAwait(false);

            // subscribe to the ready event from the discord client
            _discordClient.Ready += DiscordClient_Ready;
            _discordClient.ButtonExecuted += ButtonExecutedHandler;
            // subscribe to the interaction created event from the discord client
            // (occurs when player interacts with its posted events.)
            _discordClient.InteractionCreated += async (x) =>
            {
                SocketInteractionContext ctx = new SocketInteractionContext(_discordClient, x);
                await _interactionModule.ExecuteCommandAsync(ctx, _services).ConfigureAwait(false);
            };

            await _botServices.Start().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Stops the discord bot
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // if the discord bot token is not empty
        if (!string.IsNullOrEmpty(_discordConfig.GetValueOrDefault(nameof(DiscordConfig.DiscordBotToken), string.Empty)))
        {
            // await for all bot services to stop
            await _botServices.Stop().ConfigureAwait(false);
            _processReportQueueCts?.Cancel();
            _updateStatusCts?.Cancel();

            await _discordClient.LogoutAsync().ConfigureAwait(false);
            await _discordClient.StopAsync().ConfigureAwait(false);

            // dispose of the discord client
            _discordClient.Dispose();
            _interactionModule?.Dispose();
            _logger.LogInformation("DiscordBot Stopped");
        }
    }

    /// <summary>
    /// The ready event for the discord client
    /// </summary>
    private async Task DiscordClient_Ready()
    {
        // only obtain the guild for Cordy's Kinkporium.
        RestGuild ckGuild = (await _discordClient.Rest.GetGuildsAsync().ConfigureAwait(false)).First();
        // Register the commands for it.
        await _interactionModule.RegisterCommandsToGuildAsync(ckGuild.Id, true).ConfigureAwait(false);
        // cancel and dispose the previous update status.
        _updateStatusCts?.Cancel();
        _updateStatusCts?.Dispose();
        _updateStatusCts = new();
        _ = UpdateStatusAsync(_updateStatusCts.Token);

        // create our updated modal for the account management system
        await CreateOrUpdateModal(ckGuild).ConfigureAwait(false);
        // update the stored guild to our bot service.
        _botServices.UpdateGuild(ckGuild);
        // assign our created schedulars for the bot.
        _ = RunScheduledTask("SyncRolesDict", () => UpdateVanityRoles(ckGuild, _updateStatusCts.Token), TimeSpan.FromHours(12), _updateStatusCts.Token);
        _ = RunScheduledTask("AddDonorPerks", () => AddPerksToVanityUsers(ckGuild, _updateStatusCts.Token), TimeSpan.FromHours(6), _updateStatusCts.Token);
        _ = RunScheduledTask("RemoveDonorPerks", () => RemoveVanityPerks(_updateStatusCts.Token), TimeSpan.FromHours(6), _updateStatusCts.Token);
        // Canceled by its own token, also has its own timer.
        _ = ProcessReportsQueue(ckGuild);
    }

    private async Task RunScheduledTask(string name, Func<Task> action, TimeSpan interval, CancellationToken cts)
    {
        while (!cts.IsCancellationRequested)
        {
            try
            {
                await action().ConfigureAwait(false);
                await Task.Delay(interval, cts).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                break; // Task canceled, exit gracefully
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in repeating task {TaskName}", name);
            }
        }
    }

    /// <summary>
    ///     The primary function for creating / updating the account claim system
    /// </summary>
    private async Task CreateOrUpdateModal(RestGuild guild)
    {
        // log that we are creating the account management system channel
        _logger.LogDebug("Account Management Wizard: Getting Channel");

        // fetch the channel for the account management system message
        ulong? discordChannelForCommands = _discordConfig.GetValue<ulong?>(nameof(DiscordConfig.DiscordChannelForCommands));
        if (discordChannelForCommands is null)
        {
            _logger.LogWarning("Account Management Wizard: No channel configured");
            return;
        }

        // create the message
        IUserMessage? message = null;
        SocketTextChannel socketchannel = await _discordClient.GetChannelAsync(discordChannelForCommands.Value).ConfigureAwait(false) as SocketTextChannel 
            ?? throw new Exception("Channel not found");

        IReadOnlyCollection<RestMessage> pinnedMessages = await socketchannel.GetPinnedMessagesAsync().ConfigureAwait(false);
        
        // check if the message is already pinned
        foreach (RestMessage msg in pinnedMessages)
        {
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
        ComponentBuilder cb = new ComponentBuilder();
        // this claim your account button will trigger the customid of wizard-home:true, letting the bot deliever a personalized reply
        // that will display the account information.
        cb.WithButton("Start GagSpeak Account Management", style: ButtonStyle.Primary, customId: "wizard-home:true", emote: Emoji.Parse("🎀"));
        // if the previous message is null
        if (prevMessage is null)
        {
            // send the message to the channel
            RestUserMessage msg = await channel.SendMessageAsync(embed: eb.Build(), components: cb.Build()).ConfigureAwait(false);
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

    /// <summary>
    ///     Processes the reports queue (Should also make this manual requestable)
    /// </summary>
    private async Task ProcessReportsQueue(RestGuild guild)
    {
        // reset the CTS
        _processReportQueueCts?.Cancel();
        _processReportQueueCts?.Dispose();
        _processReportQueueCts = new();
        CancellationToken reportsToken = _processReportQueueCts.Token;

        // while the token is not cancelled,
        while (!reportsToken.IsCancellationRequested)
        {
            await _botServices.ProcessReports(_discordClient.CurrentUser, reportsToken).ConfigureAwait(false);
            // wait 30minutes before next execution.
            _logger.LogInformation("Waiting 60 minutes before next report processing");
            await Task.Delay(TimeSpan.FromMinutes(60), reportsToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Updates the vanity roles in the concurrent dictionary for the bot services to reflect the list in the appsettings.json
    /// </summary>
    private async Task UpdateVanityRoles(RestGuild guild, CancellationToken token)
    {
        _logger.LogInformation("Updating Vanity Roles From Config File");
        Dictionary<ulong, string> vanityRoles = _discordConfig.GetValueOrDefault(nameof(DiscordConfig.VanityRoles), new Dictionary<ulong, string>());
        // if the vanity roles are not the same as the list fetched from the bot service,
        if (vanityRoles.Keys.Count != _botServices.VanityRoles.Count)
        {
            // clear the roles in the bot service, and create a new list
            _botServices.VanityRoles.Clear();
            // for each role in the list of roles
            foreach (KeyValuePair<ulong, string> role in vanityRoles)
            {
                _logger.LogDebug($"Adding Role: {role.Key} => {role.Value}");
                // add the ID and the name of the role to the bot service.
                RestRole restrole = guild.GetRole(role.Key);
                if (restrole != null) _botServices.VanityRoles.Add(restrole, role.Value);
            }
        }
    }

    /// <summary>
    /// Helps assign vanity perks to any users with an appropriate vanity role, and assigns them the perks.
    /// </summary>
    private async Task AddPerksToVanityUsers(RestGuild ckGuild, CancellationToken token)
    {
        // try and clean up the vanity UID's from people no longer Supporting CK
        _logger.LogInformation("[AddVanityPerks] Adding VanityRoles to Active Supporters of CK");
        Dictionary<ulong, string> ckRoles = _discordConfig.GetValueOrDefault(nameof(DiscordConfig.VanityRoles), new Dictionary<ulong, string>());

        using var scope = _services.CreateAsyncScope();
        using var db = scope.ServiceProvider.GetRequiredService<GagspeakDbContext>();

        // Get all users in the database who have been verified by the bot but have no supporter role.
        HashSet<string> verifiedUserUids = await db.AccountReputation.Include(r => r.User)
            .AsNoTracking()
            .Where(r => r.IsVerified) // maybe they supported a higher tier so we should check.
            .Select(r => r.User.UID)
            .ToHashSetAsync()
            .ConfigureAwait(false);

        _logger.LogDebug($"[AddVanityPerks] Found {verifiedUserUids.Count} verified users without perks.");

        var claimsToCheck = await db.AccountClaimAuth.Include(a => a.User)
            .Where(a => a.User != null && verifiedUserUids.Contains(a.User.UID))
            .ToListAsync()
            .ConfigureAwait(false);

        var claimChecksDict = claimsToCheck.ToDictionary(c => c.DiscordId, c => c);
        _logger.LogDebug($"[AddVanityPerks] Found {claimsToCheck.Count} claims that match this condition.");

        // Run a loop to check each of these users with some delay to avoid triggering discord's rate limiting.
        // For each check, ensure they have a valid accountClaimAuth.
        await foreach (var ckUserList in ckGuild.GetUsersAsync(new RequestOptions { CancelToken = token }).ConfigureAwait(false))
        {
            _logger.LogInformation($"[AddVanityPerks] Processing chunk of {ckUserList.Count} users for vanity perks.");
            foreach (var ckUser in ckUserList)
            {
                // Skip if a bot.
                if (ckUser.IsBot)
                    continue;
                try
                {
                    // Only process logic if they are in our claim checks dict.
                    if (claimChecksDict.TryGetValue(ckUser.Id, out var authClaim))
                    {
                        // User has no vanity roles → skip
                        if (!ckUser.RoleIds.Any(ckRoles.ContainsKey))
                            continue;

                        // Determine highest supporter tier
                        var highestRole = ckUser.RoleIds
                            .Where(ckRoles.ContainsKey)
                            .Select(id => RoleToVanityTier[ckRoles[id]])
                            .OrderByDescending(tier => tier)
                            .First();

                        if (authClaim.User!.Tier == highestRole)
                            continue;

                        _logger.LogDebug($"[AddVanityPerks] User {ckUser.GlobalName} ({authClaim.User.UID}) has discord roles: {string.Join(", ", ckUser.RoleIds)}");

                        _logger.LogInformation($"[AddVanityPerks] User {authClaim.User.UID} assigned to tier {highestRole}");
                        authClaim.User.Tier = highestRole;

                        // Update this on all secondary accounts of this user.
                        var altProfiles = await db.Auth.Include(a => a.User).Where(a => a.PrimaryUserUID == authClaim.User.UID).ToListAsync().ConfigureAwait(false);
                        foreach (var profile in altProfiles)
                        {
                            _logger.LogDebug($"[AddVanityPerks] AltProfile [{profile.User.UID}] also given this perk!");
                            profile.User.Tier = highestRole;
                        }

                        // await for the database to save changes for each chunk.
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[AddVanityPerks] Error processing vanity perks for user {ckUser.Id}");
                }
            }

            // await a second before checking the next user
            await Task.Delay(1000, token).ConfigureAwait(false);
        }

        _logger.LogInformation("[AddVanityPerks] Completed adding VanityRoles to Active Supporters of CK");
    }

    /// <summary> 
    ///     Removes the VanityPerks from users who are no longer supporting CK 
    /// </summary>
    private async Task RemoveVanityPerks(CancellationToken token)
    {
        // set the guild to the guild ID of Cordy's Kinkporium
        RestGuild ckGuild = (await _discordClient.Rest.GetGuildsAsync().ConfigureAwait(false)).First();
        var appId = await _discordClient.GetApplicationInfoAsync().ConfigureAwait(false);

        _logger.LogInformation($"[VanityCleanup] Cleaning up Vanity UIDs from guild {ckGuild.Name}");
        Dictionary<ulong, string> ckRoles = _discordConfig.GetValueOrDefault(nameof(DiscordConfig.VanityRoles), new Dictionary<ulong, string>());

        using var scope = _services.CreateAsyncScope();
        using var db = scope.ServiceProvider.GetRequiredService<GagspeakDbContext>();

        // Get all users in the database who have been verified by the bot but have no supporter role.
        HashSet<string> usersWithperks = await db.AccountReputation.Include(r => r.User)
            .AsNoTracking()
            .Where(r => r.IsVerified && r.User.Tier != CkSupporterTier.NoRole)
            .Select(r => r.User.UID)
            .ToHashSetAsync()
            .ConfigureAwait(false);

        _logger.LogDebug($"[VanityCleanup] Found {usersWithperks.Count} verified users with perks.");

        var claimsToCheck = await db.AccountClaimAuth.Include(a => a.User)
            .Where(a => a.User != null && usersWithperks.Contains(a.User.UID))
            .ToListAsync()
            .ConfigureAwait(false);

        var claimChecksDict = claimsToCheck.ToDictionary(c => c.DiscordId, c => c);
        _logger.LogDebug($"[VanityCleanup] Identified {claimsToCheck.Count} claims that match this condition.");

        // Run a loop to check each of these users with some delay to avoid triggering discord's rate limiting.
        // For each check, ensure they have a valid accountClaimAuth.
        await foreach (var ckUserList in ckGuild.GetUsersAsync(new RequestOptions { CancelToken = token }).ConfigureAwait(false))
        {
            _logger.LogInformation($"[VanityCleanup] Processing chunk of {ckUserList.Count} users.");
            foreach (var ckUser in ckUserList)
            {
                try
                {
                    // Only process logic if they are in our claim checks dict.
                    if (!claimChecksDict.TryGetValue(ckUser.Id, out var authClaim))
                        continue;

                    // If they no longer have any roles, we should remove them from all profiles of the account.
                    if (!ckUser.RoleIds.Any(ckRoles.Keys.Contains))
                    {
                        _logger.LogInformation($"[VanityCleanup] {ckUser.DisplayName} ({ckUser.Id}) is no longer a supporting CK. Cleaning up account profile alias and roles.");
                        authClaim.User!.Alias = null;
                        authClaim.User.Tier = CkSupporterTier.NoRole;
                        // Clear out the vanity perks from their alts.
                        var secondaryUsers = await db.Auth.Include(u => u.User).Where(u => u.PrimaryUserUID == authClaim.User.UID).ToListAsync().ConfigureAwait(false);
                        foreach (var secondaryUser in secondaryUsers)
                        {
                            _logger.LogDebug($"Secondary User {secondaryUser!.User!.UID} not in allowed roles, deleting alias & resetting supporter tier");
                            secondaryUser.User.Alias = null;
                            secondaryUser.User.Tier = CkSupporterTier.NoRole;
                        }
                        // await for the database to save changes
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[VanityCleanup] Error cleaning up {ckUser.DisplayName} ({ckUser.Id})");
                }
            }

            // await a second before checking the next user
            await Task.Delay(1000, token).ConfigureAwait(false);
        }

        _logger.LogInformation("[VanityCleanup] Finished Cleaning up users no longer supporting CK");
    }

    /// <summary>
    ///     Updates the status of the bot at the interval
    /// </summary>
    private async Task UpdateStatusAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            System.Net.EndPoint endPoint = _connectionMultiplexer.GetEndPoints().First();
            // fetch the total number of online users connected to the redis server
            int onlineUsers = await _connectionMultiplexer.GetServer(endPoint).KeysAsync(pattern: "GagspeakHub:UID:*").CountAsync().ConfigureAwait(false);
            
            _logger.LogTrace($"Users online: {onlineUsers}");
            await _discordClient.SetActivityAsync(new Game($"with {onlineUsers} Kinksters")).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
        }
    }
}
#nullable disable