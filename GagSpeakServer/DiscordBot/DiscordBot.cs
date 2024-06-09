
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Gagspeak.API.Data.Enum;
using Gagspeak.API.Dto.User;
using Gagspeak.API.SignalR;
using GagspeakServer.Hubs;
using GagspeakServer.Data;
using GagspeakServer.Models;
using GagspeakServer.Services;
using GagspeakServer.Utils.Configuration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text;
using System.Threading.Channels;
using GagspeakServer.Discord.Configuration;

namespace GagspeakServer.Discord;

internal class DiscordBot : IHostedService
{
    private readonly DiscordBotServices _botServices;                               // The class for the discord bot services
    private readonly IConfigService<DiscordConfiguration> _discordConfigService;    // The configuration service for the discord bot
    private readonly IConnectionMultiplexer _connectionMultiplexer;                 // the connection multiplexer for the discord bot
    private readonly DiscordSocketClient _discordClient;                            // the discord client socket
    private readonly ILogger<DiscordBot> _logger;                                   // the logger for the discord bot
    private readonly IHubContext<GagspeakHub> _gagspeakHubContext;                  // the hub context for the gagspeak hub
    private readonly IServiceProvider _services;                                    // the service provider for the discord bot
    private InteractionService _interactionModule;                                  // the interaction module for the discord bot
    private CancellationTokenSource? _processReportQueueCts;                        // the CTS for the process report queues
    private CancellationTokenSource? _updateStatusCts;                              // the CTS for the update status
    private CancellationTokenSource? _vanityUpdateCts;                              // the CTS for the vanity update

    public DiscordBot(DiscordBotServices botServices, IServiceProvider services, IConfigService<DiscordConfiguration> configuration,
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
            // These will be respondible for generating the UID's
            // (at least for now, that purpose will change later)
            await _interactionModule.AddModuleAsync(typeof(GagspeakModule), _services).ConfigureAwait(false);
            await _interactionModule.AddModuleAsync(typeof(GagspeakWizardModule), _services).ConfigureAwait(false);
            await _interactionModule.AddModuleAsync(typeof(KinkDispenserModule), _services).ConfigureAwait(false);

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
            _ = RemoveUsersNotInVanityRole();
        }
    }

    /// <summary>
    /// Updates the vanity roles
    /// </summary>
    private async Task UpdateVanityRoles(RestGuild guild)
    {
        while (!_updateStatusCts.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Updating Vanity Roles");
                Dictionary<ulong, string> vanityRoles = _discordConfigService.GetValueOrDefault(nameof(DiscordConfiguration.VanityRoles), new Dictionary<ulong, string>());
                if (vanityRoles.Keys.Count != _botServices.VanityRoles.Count)
                {
                    _botServices.VanityRoles.Clear();
                    foreach (var role in vanityRoles)
                    {
                        _logger.LogInformation("Adding Role: {id} => {desc}", role.Key, role.Value);

                        var restrole = guild.GetRole(role.Key);
                        if (restrole != null) _botServices.VanityRoles.Add(restrole, role.Value);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(120), _updateStatusCts.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during UpdateVanityRoles");
            }
        }
    }

    /// <summary>
    /// Creates or updates the modal
    /// </summary>
    private async Task CreateOrUpdateModal(RestGuild guild)
    {
        _logger.LogInformation("Creating Wizard: Getting Channel");

        var discordChannelForCommands = _discordConfigService.GetValue<ulong?>(nameof(DiscordConfiguration.DiscordChannelForCommands));
        if (discordChannelForCommands == null)
        {
            _logger.LogWarning("Creating Wizard: No channel configured");
            return;
        }

        IUserMessage? message = null;
        var socketchannel = await _discordClient.GetChannelAsync(discordChannelForCommands.Value).ConfigureAwait(false) as SocketTextChannel;
        var pinnedMessages = await socketchannel.GetPinnedMessagesAsync().ConfigureAwait(false);
        foreach (var msg in pinnedMessages)
        {
            _logger.LogInformation("Creating Wizard: Checking message id {id}, author is: {author}, hasEmbeds: {embeds}", msg.Id, msg.Author.Id, msg.Embeds.Any());
            if (msg.Author.Id == _discordClient.CurrentUser.Id
                && msg.Embeds.Any())
            {
                message = await socketchannel.GetMessageAsync(msg.Id).ConfigureAwait(false) as IUserMessage;
                break;
            }
        }

        _logger.LogInformation("Creating Wizard: Found message id: {id}", message?.Id ?? 0);

        await GenerateOrUpdateWizardMessage(socketchannel, message).ConfigureAwait(false);
    }

    /// <summary>
    /// Generates or the primary bot message that players will use to interact with their gagspeak account.
    /// </summary>
    private async Task GenerateOrUpdateWizardMessage(SocketTextChannel channel, IUserMessage? prevMessage)
    {
        // display the UI
        EmbedBuilder eb = new EmbedBuilder();
        eb.WithTitle("Official CK GagSpeak Bot Service");
        eb.WithDescription("Press \"Start\" to interact with me!" + Environment.NewLine + Environment.NewLine
            + "You can handle all of your GagSpeak account needs in this server.\nJust follow the instructions!");
        eb.WithThumbnailUrl("https://raw.githubusercontent.com/CordeliaMist/GagSpeak-Client/main/images/iconUI.png");
        var cb = new ComponentBuilder();
        cb.WithButton("Coming Soon! ♥", style: ButtonStyle.Primary, customId: "wizard-home:true", emote: Emoji.Parse("➡️"));
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
        else
        {
            await prevMessage.ModifyAsync(p =>
            {
                p.Embed = eb.Build();
                p.Components = cb.Build();
            }).ConfigureAwait(false);
        }
        // once the button is pressed, it should display the main menu to the user.
    }

    /// <summary>
    /// Logs the message
    /// </summary>
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
    /// Filter out users no longer in the vanity role.
    /// </summary>
    private async Task RemoveUsersNotInVanityRole()
    {
        _vanityUpdateCts?.Cancel();
        _vanityUpdateCts?.Dispose();
        _vanityUpdateCts = new();
        var token = _vanityUpdateCts.Token;
        var guild = (await _discordClient.Rest.GetGuildsAsync()).First();
        var appId = await _discordClient.GetApplicationInfoAsync().ConfigureAwait(false);

        while (!token.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation($"Cleaning up Vanity UIDs");
                _logger.LogInformation("Getting application commands from guild {guildName}", guild.Name);
                var restGuild = await _discordClient.Rest.GetGuildAsync(guild.Id);

                Dictionary<ulong, string> allowedRoleIds = _discordConfigService.GetValueOrDefault(nameof(DiscordConfiguration.VanityRoles), new Dictionary<ulong, string>());
                _logger.LogInformation($"Allowed role ids: {string.Join(", ", allowedRoleIds)}");

                if (allowedRoleIds.Any())
                {
                    await using var scope = _services.CreateAsyncScope();
                    await using (var db = scope.ServiceProvider.GetRequiredService<GagspeakDbContext>())
                    {
                        var aliasedUsers = await db.LodeStoneAuth.Include("User")
                            .Where(c => c.User != null && !string.IsNullOrEmpty(c.User.Alias)).ToListAsync().ConfigureAwait(false);

                        foreach (var lodestoneAuth in aliasedUsers)
                        {
                            var discordUser = await restGuild.GetUserAsync(lodestoneAuth.DiscordId).ConfigureAwait(false);
                            _logger.LogInformation($"Checking User: {lodestoneAuth.DiscordId}, {lodestoneAuth.User.UID} ({lodestoneAuth.User.Alias}), User in Roles: {string.Join(", ", discordUser?.RoleIds ?? new List<ulong>())}");

                            if (discordUser == null || !discordUser.RoleIds.Any(u => allowedRoleIds.Keys.Contains(u)))
                            {
                                _logger.LogInformation($"User {lodestoneAuth.User.UID} not in allowed roles, deleting alias");
                                lodestoneAuth.User.Alias = null;
                                var secondaryUsers = await db.Auth.Include(u => u.User).Where(u => u.PrimaryUserUID == lodestoneAuth.User.UID).ToListAsync().ConfigureAwait(false);
                                foreach (var secondaryUser in secondaryUsers)
                                {
                                    _logger.LogInformation($"Secondary User {secondaryUser.User.UID} not in allowed roles, deleting alias");

                                    secondaryUser.User.Alias = null;
                                    db.Update(secondaryUser.User);
                                }
                                db.Update(lodestoneAuth.User);
                            }

                            await db.SaveChangesAsync().ConfigureAwait(false);
                            await Task.Delay(1000);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("No roles for command defined, no cleanup performed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something failed during checking vanity user uids");
            }

            _logger.LogInformation("Vanity UID cleanup complete");
            await Task.Delay(TimeSpan.FromHours(12), _vanityUpdateCts.Token).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Updates the status of the bot
    /// </summary>
    private async Task UpdateStatusAsync()
    {
        _updateStatusCts = new();
        while (!_updateStatusCts.IsCancellationRequested)
        {
            var endPoint = _connectionMultiplexer.GetEndPoints().First();
            var onlineUsers = await _connectionMultiplexer.GetServer(endPoint).KeysAsync(pattern: "UID:*").CountAsync();

            _logger.LogInformation("Kinksters online: {onlineUsers}", onlineUsers);
            await _discordClient.SetActivityAsync(new Game("with " + onlineUsers + " Kinksters")).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(120)).ConfigureAwait(false);
        }
    }
}