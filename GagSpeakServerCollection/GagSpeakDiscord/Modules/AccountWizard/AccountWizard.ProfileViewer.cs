using System.Globalization;
using Discord;
using Discord.Interactions;
using GagspeakAPI.Enums;
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

        using var gagspeakDb = await GetDbContext().ConfigureAwait(false);
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

        using var gagspeakDb = await GetDbContext().ConfigureAwait(false);
        var dbUser = await gagspeakDb.Auth.SingleOrDefaultAsync(u => u.UserUID == uid).ConfigureAwait(false);
        EmbedBuilder eb = new();
        string title = string.Equals(dbUser.UserUID, dbUser.PrimaryUserUID, StringComparison.Ordinal) ? "Primary Account Profile" : "Alt Character Profile";
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
        var dbUserAuth = await db.Auth.SingleOrDefaultAsync(u => u.UserUID == uid).ConfigureAwait(false);

        var identity = await _connectionMultiplexer.GetDatabase().StringGetAsync("GagspeakHub:UID:" + dbUser.UID).ConfigureAwait(false);

        // display the user's set Alias if they have one
        eb.AddField("Vanity UID", dbUser?.Alias ?? "No Vanity UID Set");
        eb.AddField("Secret Key", dbUserAuth?.HashedKey ?? "No Secret Key");

        // Last login UTC & Last login Local
        var lastOnlineUtc = new DateTimeOffset(dbUser.LastLogin, TimeSpan.Zero);
        eb.AddField("Last Online (UTC)", lastOnlineUtc.ToString("u", CultureInfo.InvariantCulture));
        var formattedTimestamp = string.Create(CultureInfo.InvariantCulture, $"<t:{lastOnlineUtc.ToUnixTimeSeconds()}:F>");
        eb.AddField("Last Online (Local)", formattedTimestamp);

        // display this accounts vanity tier if they have one
        if (dbUser.Tier != CkSupporterTier.NoRole)
        {
            eb.AddField("Supporter Tier", dbUser.Tier.ToString());
        }
        else
        {
            eb.AddField("Supporter Tier", "Not Currently any Supporter Role");
        }

        // Show if they are currently online or not.
        eb.AddField("Currently online ", !string.IsNullOrEmpty(identity));
    }

}
