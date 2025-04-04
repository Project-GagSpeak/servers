using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GagspeakShared.Data;
using GagspeakShared.Models;
using GagspeakShared.Services;
using GagspeakShared.Utils;
using GagspeakShared.Utils.Configuration;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.RegularExpressions;

namespace GagspeakDiscord.Modules.AccountWizard;

public partial class AccountWizard : InteractionModuleBase
{
    private ILogger<AccountWizard> _logger;                                            // Logger for GagspeakCommands's interactions
    private IServiceProvider _services;                                                 // Service provider for GagspeakCommands's interactions
    private DiscordBotServices _botServices;                                            // Discord bot services
    private IConfigurationService<ServerConfiguration> _gagspeakClientConfigurationService;           // Configuration service for Gagspeak client
    private IConfigurationService<DiscordConfiguration> _discordConfigService;                 // Configuration service for Discord
    private IConnectionMultiplexer _connectionMultiplexer;                              // the connection multiplexer for the discord bot.
    private Random random = new();                                                      // RANDOM WHOA

    public AccountWizard(ILogger<AccountWizard> logger, IServiceProvider services, DiscordBotServices botServices,
        IConfigurationService<ServerConfiguration> gagspeakClientConfigurationService,
        IConfigurationService<DiscordConfiguration> discordConfigService,
        IConnectionMultiplexer connectionMultiplexer)
    {
        _logger = logger;
        _services = services;
        _botServices = botServices;
        _gagspeakClientConfigurationService = gagspeakClientConfigurationService;
        _discordConfigService = discordConfigService;
        _connectionMultiplexer = connectionMultiplexer;
    }


    // The main menu display for the GagSpeak Account Management Wizard.
    // Initially called upon by the bot's pinned message, and the * is true, meaning it is to be initialized
    [ComponentInteraction("wizard-home:*")]
    public async Task StartAccountManagementWizard(bool init = false)
    {
        // if the interaction was not valid, then return.
        if (!init && !(await ValidateInteraction().ConfigureAwait(false))) return;

        // Interaction was successful, so log it.
        _logger.LogInformation("{method}:{userId}", nameof(StartAccountManagementWizard), Context.Interaction.User.Id);

        // fetch the database context to see if they already have a claimed account.
        using var gagspeakDb = GetDbContext();
        // the user has an account of they have an accountClaimAuth in the database matching their discord ID.
        // Additionally, it checks to see if the time started at is null, meaning the claiming process has finished.
        bool hasAccount = await gagspeakDb.AccountClaimAuth.AnyAsync(u => u.DiscordId == Context.User.Id && u.StartedAt == null).ConfigureAwait(false);

        EmbedBuilder eb = new();
        eb.WithTitle("Welcome to CK's GagSpeak Account Management. How may I help you today?");
        eb.WithDescription("Here is what you can do:" + Environment.NewLine + Environment.NewLine
            // if the user DOES NOT HAVE AN ACCOUNT, these options will display in place of empty strings.
            + (hasAccount ? string.Empty : ("- To Claim ownership of your Generated Account Key, select \"🎉 Claim Account\"" + Environment.NewLine))
            + (hasAccount ? string.Empty : ("- If you are using a new Discord Account, select \"🔗 Relink Account\"" + Environment.NewLine))
            // if the user DOES HAVE AN ACCOUNT, these options will display in place of empty strings.
            + (!hasAccount ? string.Empty : ("- To view your profiles in your Account, press \"📖 View Profiles\"" + Environment.NewLine))
            // + (!hasAccount ? string.Empty : ("- You lost your secret key press \"🏥 Recover\"" + Environment.NewLine)) // this wont fully work yet due to the way our system is set up
            + (!hasAccount ? string.Empty : ("- To add a new profile for an alt character, press \"🏷️ Add Profile\"" + Environment.NewLine))
            + (!hasAccount ? string.Empty : ("- To view and set your vanity perks, press \"💄 Vanity Perks\"" + Environment.NewLine))
            + (!hasAccount ? string.Empty : ("- Delete your primary or secondary accounts with \"⚠️ Remove\""))
            );
        eb.WithColor(Color.Magenta);
        // construct the buttons for the respectively displayed options.
        ComponentBuilder cb = new();
        // display if the user does not have a verified account yet
        if (!hasAccount)
        {
            cb.WithButton("Claim Account", "wizard-claim", ButtonStyle.Primary, new Emoji("🎉"));
            //cb.WithButton("Relink Account", "wizard-relink", ButtonStyle.Secondary, new Emoji("🔗"));
        }
        // display if the user has a verified account
        else
        {
            cb.WithButton("View Profiles", "wizard-profiles", ButtonStyle.Secondary, new Emoji("📖"));
            cb.WithButton("Add Profile", "wizard-alt-profile", ButtonStyle.Secondary, new Emoji("🏷️"));
            cb.WithButton("Vanity Perks", "wizard-vanity", ButtonStyle.Secondary, new Emoji("💄"));
            cb.WithButton("Remove", "wizard-remove", ButtonStyle.Danger, new Emoji("⚠️"));
        }

        // if this message is being generated in response to the user pressing "Start" on the initial message,
        // send the message as an ephemeral message, meaning a reply personalized so only the user can see it.
        if (init)
        {
            bool isBanned = await gagspeakDb.BannedRegistrations.AnyAsync(u => u.DiscordId == Context.User.Id.ToString()).ConfigureAwait(false);
            if (isBanned)
            {
                EmbedBuilder ebBanned = new();
                ebBanned.WithTitle("The CK Team has Banned This Account.");
                ebBanned.WithDescription("If you wish to be unbanned, contact one of the assistants regarding the issue.");
                ebBanned.WithColor(Color.Red);

                await RespondAsync(embed: ebBanned.Build(), ephemeral: true).ConfigureAwait(false);
                return;
            }

            await RespondAsync(embed: eb.Build(), components: cb.Build(), ephemeral: true).ConfigureAwait(false);
            var resp = await GetOriginalResponseAsync().ConfigureAwait(false);
            // store the content message of the original responce with the user's ID as the key in the concurrent dictionary of valid interactions.
            _botServices.ValidInteractions[Context.User.Id] = resp.Id;
            _logger.LogInformation("Init Msg: {id}", resp.Id);
        }
        // otherwise, if we are revisiting the homepage but the embed was already made, simply modify the interaction.
        else
        {
            await ModifyInteraction(eb, cb).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The modal for the vanity Uid modal (user ID) (a confirmation popup)
    /// likely add more modals for setting additional things for vanity perks
    /// </summary>
    public class VanityUidModal : IModal
    {
        public string Title => "Create an Alias for your UID";

        [InputLabel("Set your Vanity UID")]
        [ModalTextInput("vanity_uid", TextInputStyle.Short, "5-15 characters, underscore, dash", 5, 15)]
        public string DesiredVanityUID { get; set; }
    }


    /// <summary> The modal for the confirm deletion display (a confirmation popup) </summary>
    public class ConfirmDeletionModal : IModal
    {
        public string Title => "Confirm Account Profile Deletion";

        [InputLabel("Enter \"DELETE\" in all Caps")]
        [ModalTextInput("confirmation", TextInputStyle.Short, "Enter DELETE")]
        public string Delete { get; set; }
    }


    /// <summary> Helper function for grabbing the database context from the GagSpeak VM host. </summary>
    private GagspeakDbContext GetDbContext() => _services.CreateScope().ServiceProvider.GetService<GagspeakDbContext>();


    /// <summary> Helper function used to validate the interaction being made with the discord bot </summary>
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

    /// <summary> Helper function for adding a home button so the user has a "back button" equivalent option in the sub-menus. </summary>
    private void AddHome(ComponentBuilder cb) => cb.WithButton("Return to Home", "wizard-home:false", ButtonStyle.Secondary, new Emoji("🏠"));

    /// <summary> 
    /// Helper function used for modifying the interaction with a modal object to adjust what is being displayed.
    /// Because this is a modal interaction modification, we check against the socket modal type.
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
    /// Helper function for modifying the interaction with an embed and component builder
    /// Because this is a regular interaction, we will check against the IComponentInteraction type.
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
    /// Helper function for adding the profile selection under the inspect account section.
    /// This will allow the user to select a profile to view each of their distinct profiles registered under their account.
    /// </summary>
    private async Task AddUserSelection(GagspeakDbContext gagspeakDb, ComponentBuilder cb, string customId)
    {
        var discordId = Context.User.Id;                                                // Get the Discord ID of the current user

        var existingAuth = await gagspeakDb.AccountClaimAuth.Include(u => u.User)       // then fetch the existing auth for the primary user
            .SingleOrDefaultAsync(e => e.DiscordId == discordId).ConfigureAwait(false); // where accountClaimAuth's discord ID matches interacting discord user ID

        // If there is an existing authorization, we have found a primary user to generate secondary users for.
        if (existingAuth != null)
        {
            // create a menu builder below the embedded window that allows the user to select their UID.
            SelectMenuBuilder sb = new();
            sb.WithPlaceholder("Select a UID");
            // provide is the customID of the passed in string
            sb.WithCustomId(customId);

            // now fetch a List of Auth objects which satisfies:
            var existingUids = await gagspeakDb.Auth
                .Include(u => u.User)                             // the Auth object contains a user (they are associated)                           
                .Where(u => u.UserUID == existingAuth.User.UID    // where the user's UID in the Auth is the same as the primary user ID in the AccountClaimAuth.
                    || u.PrimaryUserUID == existingAuth.User.UID) // or where the primary user UID of the Auth object is the same as the user ID in the AccountClaimAuth.
                .OrderByDescending(u => u.PrimaryUser == null)    // order these entrys by the primary user being null, so primary is at the top.
                .ToListAsync().ConfigureAwait(false);             // put them into a list.

            // for each of our profiles, we will display their UID's in the list.
            foreach (var entry in existingUids)
            {
                // add the option to the menu, displaying the Alias over the UID if one exists.
                sb.AddOption(
                    string.IsNullOrEmpty(entry.User.Alias) ? entry.UserUID : entry.User.Alias, // set the label to the UserUID if alias is empty, or alias if it's present.
                    entry.UserUID,                                                             // put the value of the option as the UserUID   (underlying value)                                                                 
                    !string.IsNullOrEmpty(entry.User.Alias) ? entry.User.UID : null,           // if the alias is not empty, set the description to the UID, otherwise null.
                    entry.PrimaryUserUID is null ? new Emoji("🌟") : new Emoji("⭐"));         // adds emoji to left of the dropdown, displays if UID is a primary or secondary profile.
            }

            // Add the select menu to the component builder
            cb.WithSelectMenu(sb);
        }
    }

    /// <summary>
    /// Helper function to generate a new AccountClaimAuth object for the user who wishes to claim their account.
    /// </summary>
    /// <param name="discordid"> The ID of the discord user wishing to create this object. </param>
    /// <param name="initialGeneratedKey"> The initial generated key they can provide the bot with (because its generated in game) </param>
    /// <param name="dbContext"> The context from the GagSpeak database. </param>
    /// <returns></returns>
    private async Task<string> GenerateAccountClaimAuth(ulong discordid, string initialGeneratedKey, GagspeakDbContext dbContext)
    {
        // generate a verification code for this particular AccountClaimAuth object.
        var verificationCode = StringUtils.GenerateRandomString(32);

        // Create the AccountClaimAuth object
        AccountClaimAuth accountClaimAuthToAdd = new AccountClaimAuth()
        {
            DiscordId = discordid,
            InitialGeneratedKey = initialGeneratedKey,
            VerificationCode = verificationCode,
            StartedAt = DateTime.UtcNow                 // we set this so that we know we are currently verifying this authentication.
        };

        // Add the new accountclaimauth object to the database context and save the changes, then return the auth string as the secret key we have generated.
        await dbContext.AddAsync(accountClaimAuthToAdd);

        // Save the changes to the database context
        await dbContext.SaveChangesAsync().ConfigureAwait(false);
        _logger.LogInformation("Created a new account generation for the accountclaimauths");
        // Return the verification code
        return (verificationCode);
    }
}
