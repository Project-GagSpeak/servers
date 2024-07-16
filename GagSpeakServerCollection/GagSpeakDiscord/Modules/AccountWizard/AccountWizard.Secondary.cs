using Discord;
using Discord.Interactions;
using GagspeakAPI.Data.Enum;
using GagspeakShared.Data;
using GagspeakShared.Models;
using GagspeakShared.Utils;
using Microsoft.EntityFrameworkCore;

namespace GagspeakDiscord.Modules.AccountWizard;

public partial class AccountWizard
{
    [ComponentInteraction("wizard-secondary")]
    public async Task ComponentSecondary()
    {
        if (!(await ValidateInteraction().ConfigureAwait(false))) return;

        _logger.LogInformation("{method}:{userId}", nameof(ComponentSecondary), Context.Interaction.User.Id);

        using var gagspeakDb = GetDbContext();
        var primaryUID = (await gagspeakDb.AccountClaimAuth.Include(u => u.User).SingleAsync(u => u.DiscordId == Context.User.Id).ConfigureAwait(false)).User.UID;
        var secondaryUids = await gagspeakDb.Auth.CountAsync(p => p.PrimaryUserUID == primaryUID).ConfigureAwait(false);
        EmbedBuilder eb = new();
        eb.WithColor(Color.Magenta);
        eb.WithTitle("Secondary UID");
        eb.WithDescription("You can create secondary UIDs here. " + Environment.NewLine + Environment.NewLine
            + "Secondary UIDs act as completely separate Gagspeak accounts with their own pair list, joined syncshells, UID and so on." + Environment.NewLine
            + "Use this to create UIDs if you want to use Gagspeak on two separate game instances at once or keep your alts private." + Environment.NewLine + Environment.NewLine
            + "__Note:__ Creating a Secondary UID is _not_ necessary to use Gagspeak for alts." + Environment.NewLine + Environment.NewLine
            + $"You currently have {secondaryUids} Secondary UIDs out of a maximum of 25.");
        ComponentBuilder cb = new();
        AddHome(cb);
        cb.WithButton("Create Secondary UID", "wizard-secondary-create:" + primaryUID, ButtonStyle.Primary, emote: new Emoji("2️⃣"), disabled: secondaryUids >= 25);
        await ModifyInteraction(eb, cb).ConfigureAwait(false);
    }

    [ComponentInteraction("wizard-secondary-create:*")]
    public async Task ComponentSecondaryCreate(string primaryUid)
    {
        if (!(await ValidateInteraction().ConfigureAwait(false))) return;

        _logger.LogInformation("{method}:{userId}:{primary}", nameof(ComponentSecondaryCreate), Context.Interaction.User.Id, primaryUid);

        using var gagspeakDb = GetDbContext();
        EmbedBuilder eb = new();
        eb.WithTitle("Secondary UID created");
        eb.WithColor(Color.Green);
        ComponentBuilder cb = new();
        AddHome(cb);
        await HandleAddSecondary(gagspeakDb, eb, primaryUid).ConfigureAwait(false);
        await ModifyInteraction(eb, cb).ConfigureAwait(false);
    }

    public async Task HandleAddSecondary(GagspeakDbContext db, EmbedBuilder embed, string primaryUID)
    {
        User newUser = new()
        {
            LastLoggedIn = DateTime.UtcNow,
            VanityTier = CkSupporterTier.NoRole
        };

        var hasValidUid = false;
        while (!hasValidUid)
        {
            var uid = StringUtils.GenerateRandomString(10);
            if (await db.Users.AnyAsync(u => u.UID == uid || u.Alias == uid).ConfigureAwait(false)) continue;
            newUser.UID = uid;
            hasValidUid = true;
        }

        var computedHash = StringUtils.Sha256String(StringUtils.GenerateRandomString(64) + DateTime.UtcNow.ToString());
        var auth = new Auth()
        {
            HashedKey = StringUtils.Sha256String(computedHash),
            User = newUser,
            PrimaryUserUID = primaryUID
        };

        await db.Users.AddAsync(newUser).ConfigureAwait(false);
        await db.Auth.AddAsync(auth).ConfigureAwait(false);

        await db.SaveChangesAsync().ConfigureAwait(false);

        embed.WithDescription("A secondary UID for you was created, use the information below and add the secret key to the GagSpeak setings in the Service Settings tab.");
        embed.AddField("UID", newUser.UID);
        embed.AddField("Secret Key", computedHash);
    }

}
