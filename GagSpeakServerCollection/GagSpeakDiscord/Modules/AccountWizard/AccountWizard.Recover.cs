using Discord;
using Discord.Interactions;
using GagspeakShared.Data;
using GagspeakShared.Models;
using GagspeakShared.Utils;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace GagspeakDiscord.Modules.AccountWizard;

// NOTICE: This file has no current use, and was derived from mare's discord bot framework.
// Because GagSpeak embeds its profiles directly and does not allow the modification of keys, this serves no current purpose.
// Keep it here however, in the case we may eventually need to use this one day.
public partial class AccountWizard
{
    [ComponentInteraction("wizard-recover")]
    public async Task ComponentRecover()
    {
        if (!(await ValidateInteraction().ConfigureAwait(false))) return;

        _logger.LogInformation("{method}:{userId}", nameof(ComponentRecover), Context.Interaction.User.Id);

        using var gagspeakDb = await GetDbContext().ConfigureAwait(false);
        EmbedBuilder eb = new();
        eb.WithColor(Color.Magenta);
        eb.WithTitle("Recover");
        eb.WithDescription("In case you have lost your secret key you can recover it here." + Environment.NewLine + Environment.NewLine
            + "## ⚠️ **Once you recover your key, the previously used key will be invalidated. If you use GagSpeak on multiple devices you will have to update the key everywhere you use it.** ⚠️" + Environment.NewLine + Environment.NewLine
            + "Use the selection below to select the user account you want to recover." + Environment.NewLine + Environment.NewLine
            + "- 1️⃣ is your primary account/UID" + Environment.NewLine
            + "- 2️⃣ are all your secondary accounts/UIDs" + Environment.NewLine
            + "If you are using Vanity UIDs the original UID is displayed in the second line of the account selection.");
        ComponentBuilder cb = new();
        await AddUserSelection(gagspeakDb, cb, "wizard-recover-select").ConfigureAwait(false);
        AddHome(cb);
        await ModifyInteraction(eb, cb).ConfigureAwait(false);
    }

    [ComponentInteraction("wizard-recover-select")]
    public async Task SelectionRecovery(string uid)
    {
        if (!(await ValidateInteraction().ConfigureAwait(false))) return;

        _logger.LogInformation("{method}:{userId}:{uid}", nameof(SelectionRecovery), Context.Interaction.User.Id, uid);

        using var gagspeakDb = await GetDbContext().ConfigureAwait(false);
        EmbedBuilder eb = new();
        eb.WithColor(Color.Green);
        await HandleRecovery(gagspeakDb, eb, uid).ConfigureAwait(false);
        ComponentBuilder cb = new();
        AddHome(cb);
        await ModifyInteraction(eb, cb).ConfigureAwait(false);
    }

    private async Task HandleRecovery(GagspeakDbContext db, EmbedBuilder embed, string uid)
    {
        string computedHash = string.Empty;
        Auth auth;
        var previousAuth = await db.Auth.Include(u => u.User).FirstOrDefaultAsync(u => u.UserUID == uid).ConfigureAwait(false);
        if (previousAuth != null)
        {
            db.Auth.Remove(previousAuth);
        }

        computedHash = StringUtils.Sha256String(StringUtils.GenerateRandomString(64) + DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
        auth = new Auth()
        {
            HashedKey = StringUtils.Sha256String(computedHash),
            User = previousAuth.User,
            PrimaryUserUID = previousAuth.PrimaryUserUID
        };

        await db.Auth.AddAsync(auth).ConfigureAwait(false);

        embed.WithTitle($"Recovery for {uid} complete");
        embed.WithDescription("This is your new private secret key. Do not share this private secret key with anyone. **If you lose it, it is irrevocably lost.**"
                              + Environment.NewLine + Environment.NewLine
                              + $"**{computedHash}**"
                              + Environment.NewLine + Environment.NewLine
                              + "Enter this key in the GagSpeak Service Settings and reconnect to the service.");

        await db.Auth.AddAsync(auth).ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);
    }
}
