using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GagspeakServer.Data;
using GagspeakServer.Discord.Configuration;
using GagspeakServer.Models;
using GagspeakServer.Services;
using GagspeakServer.Utils;
using GagspeakServer.Utils.Configuration;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.RegularExpressions;

namespace GagspeakServer.Discord;

public partial class GagspeakWizardModule : InteractionModuleBase
{
    private ILogger<GagspeakModule> _logger;                                            // Logger for GagspeakModule's interactions
    private IServiceProvider _services;                                                 // Service provider for GagspeakModule's interactions
    private DiscordBotServices _botServices;                                            // Discord bot services
    private IConfigService<ServerConfiguration> _gagspeakClientConfigurationService;    // Configuration service for Gagspeak client
    private IConfigService<DiscordConfiguration> _discordConfigService;                 // Configuration service for Discord
    private IConnectionMultiplexer _connectionMultiplexer;                              // the connection multiplexer for the discord bot.
    private Random random = new();                                                      // RANDOM WHOA

    public GagspeakWizardModule(ILogger<GagspeakModule> logger, IServiceProvider services, DiscordBotServices botServices,
        IConfigService<ServerConfiguration> gagspeakClientConfigurationService,
        IConfigService<DiscordConfiguration> discordConfigService,
        IConnectionMultiplexer connectionMultiplexer)
    {
        _logger = logger;
        _services = services;
        _botServices = botServices;
        _gagspeakClientConfigurationService = gagspeakClientConfigurationService;
        _discordConfigService = discordConfigService;
        _connectionMultiplexer = connectionMultiplexer;
    }


    // The wizard home interaction
    [ComponentInteraction("wizard-home:*")]
    public async Task StartWizard(bool init = false)
    {
        // if the interaction is not valid, return
        if (!init && !(await ValidateInteraction().ConfigureAwait(false))) return;

        // otherwise, log the interaction
        _logger.LogInformation("{method}:{userId}", nameof(StartWizard), Context.Interaction.User.Id);

        // using the gagspeak db, get its context.
        using var gagspeakDb = GetDbContext();
        // see if the user already has an acccount
        bool hasAccount = await gagspeakDb.LodeStoneAuth.AnyAsync(u => u.DiscordId == Context.User.Id && u.StartedAt == null).ConfigureAwait(false);

        // if the user has an account, get the user's account, and display the main menu for them.
        EmbedBuilder eb = new();
        eb.WithTitle("CK welcomes you to the GagSpeak Service Bot. How may we help you?");
        eb.WithDescription("Here is what you can do:" + Environment.NewLine + Environment.NewLine
            + (!hasAccount ? string.Empty : ("- Check your account status press \"ℹ️ User Info\"" + Environment.NewLine))
            + (hasAccount ? string.Empty : ("- To claim ownership of your Account press \"🌒 Claim\"" + Environment.NewLine))
            + (!hasAccount ? string.Empty : ("- You lost your secret key press \"🏥 Recover\"" + Environment.NewLine))
            + (hasAccount ? string.Empty : ("- If you have changed your Discord account press \"🔗 Relink\"" + Environment.NewLine))
            + (!hasAccount ? string.Empty : ("- Create additional profiles for alts \"2️⃣ Secondary UID\"" + Environment.NewLine))
            + (!hasAccount ? string.Empty : ("- To set a Personalized UID, press \"💅 Personalized IDs\"" + Environment.NewLine))
            + (!hasAccount ? string.Empty : ("- Delete your primary or secondary accounts with \"⚠️ Delete\""))
            );
        eb.WithColor(Color.Blue);
        ComponentBuilder cb = new();
        if (!hasAccount) // buttons that show up if you have not yet claimed ownership of your account
        {
            cb.WithButton("Register", "wizard-claim", ButtonStyle.Primary, new Emoji("🌒"));
            cb.WithButton("Relink", "wizard-relink", ButtonStyle.Secondary, new Emoji("🔗"));
        }
        else             // buttons that show up once you have already claimed ownership of your account
        {
            cb.WithButton("User Info", "wizard-userinfo", ButtonStyle.Secondary, new Emoji("ℹ️"));
            cb.WithButton("Recover", "wizard-recover", ButtonStyle.Secondary, new Emoji("🏥"));
            cb.WithButton("Secondary UID", "wizard-secondary", ButtonStyle.Secondary, new Emoji("2️⃣"));
            cb.WithButton("Vanity IDs", "wizard-vanity", ButtonStyle.Secondary, new Emoji("💅"));
            cb.WithButton("Delete", "wizard-delete", ButtonStyle.Danger, new Emoji("⚠️"));
        }
        // unsure what this means but ill figure it out.
        if (init)
        {
            await RespondAsync(embed: eb.Build(), components: cb.Build(), ephemeral: true).ConfigureAwait(false);
            var resp = await GetOriginalResponseAsync().ConfigureAwait(false);
            _botServices.ValidInteractions[Context.User.Id] = resp.Id;
            _logger.LogInformation("Init Msg: {id}", resp.Id);
        }
        else // modify the interaction????? IM SO CONFUSED LOL
        {
            await ModifyInteraction(eb, cb).ConfigureAwait(false);
        }
    }

    // the modal for the vanity Uid modal (user ID) (a confirmation popup)
    public class VanityUidModal : IModal
    {
        public string Title => "Set Vanity UID";

        [InputLabel("Set your Vanity UID")]
        [ModalTextInput("vanity_uid", TextInputStyle.Short, "5-15 characters, underscore, dash", 5, 15)]
        public string DesiredVanityUID { get; set; }
    }

    // the modal for the vanity Gid Modal (syncshell ID) (a confirmation popup)
    public class VanityGidModal : IModal
    {
        public string Title => "Set Vanity Syncshell ID";

        [InputLabel("Set your Vanity Syncshell ID")]
        [ModalTextInput("vanity_gid", TextInputStyle.Short, "5-20 characters, underscore, dash", 5, 20)]
        public string DesiredVanityGID { get; set; }
    }

    // the modal for the confirm deletion display (a confirmation popup)
    public class ConfirmDeletionModal : IModal
    {
        public string Title => "Confirm Account Deletion";

        [InputLabel("Enter \"DELETE\" in all Caps")]
        [ModalTextInput("confirmation", TextInputStyle.Short, "Enter DELETE")]
        public string Delete { get; set; }
    }

    /// <summary>
    /// Grab the database context nessisary for the user account information
    /// </summary>
    private GagspeakDbContext GetDbContext()
    {
        return _services.CreateScope().ServiceProvider.GetService<GagspeakDbContext>();
    }

    /// <summary>
    /// Validate the interaction being made with the discord bot
    /// </summary>
    private async Task<bool> ValidateInteraction()
    {
        // if the context of the interaction is not an interaction component, return true
        if (Context.Interaction is not IComponentInteraction componentInteraction) return true;

        // otherwise, if the user is in the valid interactions list, and the interaction id is the same as the message id, return true
        if (_botServices.ValidInteractions.TryGetValue(Context.User.Id, out ulong interactionId) && interactionId == componentInteraction.Message.Id)
        {
            return true;
        }

        // otherwise, modify the interaction to show that the session has expired
        EmbedBuilder eb = new();
        eb.WithTitle("Session expired");
        eb.WithDescription("This session has expired since you have either again pressed \"Start\" on the initial message or the bot has been restarted." + Environment.NewLine + Environment.NewLine
            + "Please use the newly started interaction or start a new one.");
        eb.WithColor(Color.Red);
        ComponentBuilder cb = new();
        await ModifyInteraction(eb, cb).ConfigureAwait(false);

        return false;
    }

    /// <summary>
    /// The return to home button which displays whenever you are not on the main menu.
    /// </summary>
    private void AddHome(ComponentBuilder cb)
    {
        cb.WithButton("Return to Home", "wizard-home:false", ButtonStyle.Secondary, new Emoji("🏠"));
    }

    /// <summary>
    /// Modifies the modal interaction currently being displayed
    /// </summary>
    private async Task ModifyModalInteraction(EmbedBuilder eb, ComponentBuilder cb)
    {
        await (Context.Interaction as SocketModal).UpdateAsync(m =>
        {
            m.Embed = eb.Build();
            m.Components = cb.Build();
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Modifies the interaction currently being displayed
    /// </summary>
    private async Task ModifyInteraction(EmbedBuilder eb, ComponentBuilder cb)
    {
        await ((Context.Interaction) as IComponentInteraction).UpdateAsync(m =>
        {
            m.Embed = eb.Build();
            m.Components = cb.Build();
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds user selection to the component builder
    /// </summary>
    private async Task AddUserSelection(GagspeakDbContext gagspeakDb, ComponentBuilder cb, string customId)
    {
        // Get the Discord ID of the current user
        var discordId = Context.User.Id;

        // Get the existing authorization for the current user
        var existingAuth = await gagspeakDb.LodeStoneAuth.Include(u => u.User).SingleOrDefaultAsync(e => e.DiscordId == discordId).ConfigureAwait(false);

        // If there is an existing authorization
        if (existingAuth != null)
        {
            // Create a new select menu builder
            SelectMenuBuilder sb = new();
            sb.WithPlaceholder("Select a UID");
            sb.WithCustomId(customId);

            // Get the existing UIDs for the current user
            var existingUids = await gagspeakDb.Auth.Include(u => u.User).Where(u => u.UserUID == existingAuth.User.UID || u.PrimaryUserUID == existingAuth.User.UID)
                .OrderByDescending(u => u.PrimaryUser == null).ToListAsync().ConfigureAwait(false);

            // For each existing UID
            foreach (var entry in existingUids)
            {
                // Add an option to the select menu builder with the user's alias (if it exists) or UID as the label, the UID as the value, the user's UID (if the alias exists) as the description, and an emoji depending on whether the UID is primary or secondary
                sb.AddOption(string.IsNullOrEmpty(entry.User.Alias) ? entry.UserUID : entry.User.Alias,
                    entry.UserUID,
                    !string.IsNullOrEmpty(entry.User.Alias) ? entry.User.UID : null,
                    entry.PrimaryUserUID == null ? new Emoji("1️⃣") : new Emoji("2️⃣"));
            }

            // Add the select menu to the component builder
            cb.WithSelectMenu(sb);
        }
    }

    private async Task AddGroupSelection(GagspeakDbContext db, ComponentBuilder cb, string customId)
    {
        // Get the primary user for the current Discord ID
        var primary = (await db.LodeStoneAuth.Include(u => u.User).SingleAsync(u => u.DiscordId == Context.User.Id).ConfigureAwait(false)).User;

        // Get the secondary users for the primary user's UID
        var secondary = await db.Auth.Include(u => u.User).Where(u => u.PrimaryUserUID == primary.UID).Select(u => u.User).ToListAsync().ConfigureAwait(false);
    }

    private async Task<string> GenerateLodestoneAuth(ulong discordid, string hashedLodestoneId, GagspeakDbContext dbContext)
    {
        // Generate a random string for the authorization
        var auth = StringUtils.GenerateRandomString(32);

        // Create a new Lodestone authorization with the Discord ID, hashed Lodestone ID, authorization string, and current UTC time
        LodeStoneAuth lsAuth = new LodeStoneAuth()
        {
            DiscordId = discordid,
            HashedLodestoneId = hashedLodestoneId,
            LodestoneAuthString = auth,
            StartedAt = DateTime.UtcNow
        };

        // Add the new Lodestone authorization to the database context
        dbContext.Add(lsAuth);

        // Save the changes to the database context
        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        // Return the authorization string
        return (auth);
    }

    private int? ParseCharacterIdFromLodestoneUrl(string lodestoneUrl)
    {
        // Create a regex to match the Lodestone URL
        var regex = new Regex(@"https:\/\/(na|eu|de|fr|jp)\.finalfantasyxiv\.com\/lodestone\/character\/\d+");

        // Match the Lodestone URL with the regex
        var matches = regex.Match(lodestoneUrl);

        // If the Lodestone URL does not match the regex or there are no groups in the match, return null
        var isLodestoneUrl = matches.Success;
        if (!isLodestoneUrl || matches.Groups.Count < 1) return null;

        // Get the matched Lodestone URL
        lodestoneUrl = matches.Groups[0].ToString();

        // Split the Lodestone URL by '/' and get the last element
        var stringId = lodestoneUrl.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();

        // Try to parse the last element of the Lodestone URL as an integer
        if (!int.TryParse(stringId, out int lodestoneId))
        {
            // If the parsing fails, return null
            return null;
        }

        // Return the parsed integer
        return lodestoneId;
    }
}
