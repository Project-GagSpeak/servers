using Discord;
using Discord.Interactions;
using GagspeakAPI.Data.Enum;
using GagspeakShared.Data;
using Microsoft.EntityFrameworkCore;

namespace GagspeakDiscord.Modules.AccountWizard;

public partial class AccountWizard
{
    [ComponentInteraction("wizard-profiles")]
    public async Task ComponentProfiles()
    {
        if (!(await ValidateInteraction().ConfigureAwait(false))) return;

        _logger.LogInformation("{method}:{userId}", nameof(ComponentProfiles), Context.Interaction.User.Id);

        using var gagspeakDb = GetDbContext();
        EmbedBuilder eb = new();
        eb.WithTitle("Your Account Profiles");
        eb.WithColor(Color.Magenta);
        eb.WithDescription("Your Account has 1 Primary Profile." + Environment.NewLine
            + "You also have a Secondary Profile for each registered alt character." + Environment.NewLine
            + "🌟 Your Account's Primary Profile" + Environment.NewLine
            + "⭐ Your registered Alt Character Profiles");
        ComponentBuilder cb = new();
        await AddUserSelection(gagspeakDb, cb, "wizard-profiles-select").ConfigureAwait(false);
        AddHome(cb);
        await ModifyInteraction(eb, cb).ConfigureAwait(false);
    }

    [ComponentInteraction("wizard-profiles-select")]
    public async Task SelectionProfiles(string uid)
    {
        if (!(await ValidateInteraction().ConfigureAwait(false))) return;

        _logger.LogInformation("{method}:{userId}:{uid}", nameof(SelectionProfiles), Context.Interaction.User.Id, uid);

        using var gagspeakDb = GetDbContext();
        var dbUser = await gagspeakDb.Auth.SingleOrDefaultAsync(u => u.UserUID == uid).ConfigureAwait(false);
        EmbedBuilder eb = new();
        string title = (dbUser.UserUID == dbUser.PrimaryUserUID) ? "Primary Account Profile" : "Alt Character Profile";
        eb.WithTitle($"{title} - {uid}");
        await HandleProfiles(eb, gagspeakDb, uid).ConfigureAwait(false);
        eb.WithColor(Color.Magenta);
        ComponentBuilder cb = new();
        await AddUserSelection(gagspeakDb, cb, "wizard-profiles-select").ConfigureAwait(false);
        AddHome(cb);
        await ModifyInteraction(eb, cb).ConfigureAwait(false);
    }

    private async Task HandleProfiles(EmbedBuilder eb, GagspeakDbContext db, string uid)
    {
        ulong userToCheckForDiscordId = Context.User.Id;

        var dbUser = await db.Users.SingleOrDefaultAsync(u => u.UID == uid).ConfigureAwait(false);

        var identity = await _connectionMultiplexer.GetDatabase().StringGetAsync("GagspeakHub:UID:" + dbUser.UID).ConfigureAwait(false);

        // display the user's set Alias if they have one
        if (!string.IsNullOrEmpty(dbUser.Alias))
        {
            eb.AddField("Vanity UID", dbUser.Alias);
        }
        else
        {
            eb.AddField("Vanity UID", "No Vanity UID Set");
        }

        // Last login UTC & Last login Local
        var lastOnlineUtc = dbUser.LastLoggedIn;
        var lastOnlineLocal = lastOnlineUtc.ToLocalTime();
        eb.AddField("Last Online (UTC)", lastOnlineUtc.ToString("U"));
        eb.AddField("Last Online (Local)", lastOnlineLocal.ToString("f"));

        // display this accounts vanity tier if they have one
        if (dbUser.VanityTier != CkSupporterTier.NoRole)
        {
            eb.AddField("Supporter Tier", dbUser.VanityTier.ToString());
        }
        else
        {
            eb.AddField("Supporter Tier", "Not Currently any Supporter Role");
        }

        // Show if they are currently online or not.
        eb.AddField("Currently online ", !string.IsNullOrEmpty(identity));
    }

}
