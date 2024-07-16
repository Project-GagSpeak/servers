using Discord;
using Discord.Interactions;
using GagspeakShared.Data;
using Microsoft.EntityFrameworkCore;

namespace GagspeakDiscord.Modules.AccountWizard;

public partial class AccountWizard
{
    [ComponentInteraction("wizard-userinfo")]
    public async Task ComponentUserinfo()
    {
        if (!(await ValidateInteraction().ConfigureAwait(false))) return;

        _logger.LogInformation("{method}:{userId}", nameof(ComponentUserinfo), Context.Interaction.User.Id);

        using var gagspeakDb = GetDbContext();
        EmbedBuilder eb = new();
        eb.WithTitle("User Info");
        eb.WithColor(Color.Magenta);
        eb.WithDescription("You can see information about your user account(s) here." + Environment.NewLine
            + "Use the selection below to select a user account to see info for." + Environment.NewLine + Environment.NewLine
            + "- 1️⃣ is your primary account/UID" + Environment.NewLine
            + "- 2️⃣ are all your secondary accounts/UIDs" + Environment.NewLine
            + "If you are using Vanity UIDs the original UID is displayed in the second line of the account selection.");
        ComponentBuilder cb = new();
        await AddUserSelection(gagspeakDb, cb, "wizard-userinfo-select").ConfigureAwait(false);
        AddHome(cb);
        await ModifyInteraction(eb, cb).ConfigureAwait(false);
    }

    [ComponentInteraction("wizard-userinfo-select")]
    public async Task SelectionUserinfo(string uid)
    {
        if (!(await ValidateInteraction().ConfigureAwait(false))) return;

        _logger.LogInformation("{method}:{userId}:{uid}", nameof(SelectionUserinfo), Context.Interaction.User.Id, uid);

        using var gagspeakDb = GetDbContext();
        EmbedBuilder eb = new();
        eb.WithTitle($"User Info for {uid}");
        await HandleUserInfo(eb, gagspeakDb, uid).ConfigureAwait(false);
        eb.WithColor(Color.Green);
        ComponentBuilder cb = new();
        await AddUserSelection(gagspeakDb, cb, "wizard-userinfo-select").ConfigureAwait(false);
        AddHome(cb);
        await ModifyInteraction(eb, cb).ConfigureAwait(false);
    }

    private async Task HandleUserInfo(EmbedBuilder eb, GagspeakDbContext db, string uid)
    {
        ulong userToCheckForDiscordId = Context.User.Id;

        var dbUser = await db.Users.SingleOrDefaultAsync(u => u.UID == uid).ConfigureAwait(false);

        var identity = await _connectionMultiplexer.GetDatabase().StringGetAsync("GagspeakHub:UID:" + dbUser.UID).ConfigureAwait(false);

        eb.WithDescription("This is the user info for your selected UID. You can check other UIDs or go back using the menu below.");
        if (!string.IsNullOrEmpty(dbUser.Alias))
        {
            eb.AddField("Vanity UID", dbUser.Alias);
        }
        eb.AddField("Last Online (UTC)", dbUser.LastLoggedIn.ToString("U"));
        eb.AddField("Currently online ", !string.IsNullOrEmpty(identity));
    }

}
