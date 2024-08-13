using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;

namespace GagspeakDiscord.Modules.AccountWizard;

public partial class AccountWizard
{
    [ComponentInteraction("wizard-vanity")]
    public async Task ComponentVanity()
    {
        if (!(await ValidateInteraction().ConfigureAwait(false))) return;

        _logger.LogInformation("{method}:{userId}", nameof(ComponentVanity), Context.Interaction.User.Id);

        StringBuilder sb = new();
        var user = await Context.Guild.GetUserAsync(Context.User.Id).ConfigureAwait(false);
        bool userIsInVanityRole = _botServices.VanityRoles.Keys.Any(u => user.RoleIds.Contains(u.Id)) || !_botServices.VanityRoles.Any();
        _logger.LogInformation("{method}:{userId}:{userIsInVanityRole}", nameof(ComponentVanity), Context.Interaction.User.Id, userIsInVanityRole);
        if (!userIsInVanityRole)
        {
            sb.AppendLine("To be able to set Vanity IDs you must have one of the following roles:");
            foreach (var role in _botServices.VanityRoles)
            {
                sb.Append("- ").Append(role.Key.Mention).Append(" (").Append(role.Value).AppendLine(")");
            }
        }
        else
        {
            sb.AppendLine("Your current roles on this server allow you to set Vanity IDs.");
            // display the role that allows us to get here.
            foreach (var role in _botServices.VanityRoles)
            {
                if (user.RoleIds.Contains(role.Key.Id))
                {
                    sb.AppendLine("You are currently in the role: " + role.Key.Mention + " (" + role.Value + ")");
                }
            }
        }

        EmbedBuilder eb = new();
        eb.WithTitle("Supporter Vanity Perks");
        eb.WithDescription("As a supporter, you will have a unique icon beside your name in the pair list & profile." + Environment.NewLine
            + "Additionally, you will be able to set a UID Alias." + Environment.NewLine
            + "UID Alias's are a customizable label for your profiles UID that others will be able to see!." + Environment.NewLine + Environment.NewLine
            + sb.ToString());
        eb.WithColor(Color.Magenta);
        ComponentBuilder cb = new();
        AddHome(cb);
        if (userIsInVanityRole)
        {
            using var db = GetDbContext();
            await AddUserSelection(db, cb, "wizard-vanity-uid").ConfigureAwait(false);
        }

        await ModifyInteraction(eb, cb).ConfigureAwait(false);
    }

    [ComponentInteraction("wizard-vanity-uid")]
    public async Task SelectionVanityUid(string uid)
    {
        if (!(await ValidateInteraction().ConfigureAwait(false))) return;

        _logger.LogInformation("{method}:{userId}:{uid}", nameof(SelectionVanityUid), Context.Interaction.User.Id, uid);

        using var db = GetDbContext();
        var user = db.Users.Single(u => u.UID == uid);
        EmbedBuilder eb = new();
        eb.WithColor(Color.Red);
        eb.WithTitle($"Set Vanity UID for {uid}");
        eb.WithDescription($"You are about to change the Vanity UID for {uid}" + Environment.NewLine + Environment.NewLine
            + "The current Vanity UID is set to: **" + (user.Alias == null ? "No Vanity UID set" : user.Alias) + "**");
        ComponentBuilder cb = new();
        cb.WithButton("Cancel", "wizard-vanity", ButtonStyle.Secondary, emote: new Emoji("❌"));
        cb.WithButton("Set Vanity ID", "wizard-vanity-uid-set:" + uid, ButtonStyle.Primary, new Emoji("💅"));

        await ModifyInteraction(eb, cb).ConfigureAwait(false);
    }

    [ComponentInteraction("wizard-vanity-uid-set:*")]
    public async Task SelectionVanityUidSet(string uid)
    {
        if (!(await ValidateInteraction().ConfigureAwait(false))) return;

        _logger.LogInformation("{method}:{userId}:{uid}", nameof(SelectionVanityUidSet), Context.Interaction.User.Id, uid);

        await RespondWithModalAsync<VanityUidModal>("wizard-vanity-uid-modal:" + uid).ConfigureAwait(false);
    }

    [ModalInteraction("wizard-vanity-uid-modal:*")]
    public async Task ConfirmVanityUidModal(string uid, VanityUidModal modal)
    {
        if (!(await ValidateInteraction().ConfigureAwait(false))) return;

        _logger.LogInformation("{method}:{userId}:{uid}:{vanity}", nameof(ConfirmVanityUidModal), Context.Interaction.User.Id, uid, modal.DesiredVanityUID);

        EmbedBuilder eb = new();
        ComponentBuilder cb = new();
        var desiredVanityUid = modal.DesiredVanityUID;
        using var db = GetDbContext();
        bool canAddVanityId = !db.Users.Any(u => u.UID == modal.DesiredVanityUID || u.Alias == modal.DesiredVanityUID);

        Regex rgx = new(@"^[_\-a-zA-Z0-9]{5,15}$", RegexOptions.ECMAScript);
        if (!rgx.Match(desiredVanityUid).Success)
        {
            eb.WithColor(Color.Red);
            eb.WithTitle("Invalid Vanity UID");
            eb.WithDescription("A Vanity UID must be between 5 and 15 characters long and only contain the letters A-Z, numbers 0-9, dashes (-) and underscores (_).");
            cb.WithButton("Cancel", "wizard-vanity", ButtonStyle.Secondary, emote: new Emoji("❌"));
            cb.WithButton("Pick Different UID", "wizard-vanity-uid-set:" + uid, ButtonStyle.Primary, new Emoji("💅"));
        }
        else if (!canAddVanityId)
        {
            eb.WithColor(Color.Red);
            eb.WithTitle("Vanity UID already taken");
            eb.WithDescription($"The Vanity UID {desiredVanityUid} has already been claimed. Please pick a different one.");
            cb.WithButton("Cancel", "wizard-vanity", ButtonStyle.Secondary, emote: new Emoji("❌"));
            cb.WithButton("Pick Different UID", "wizard-vanity-uid-set:" + uid, ButtonStyle.Primary, new Emoji("💅"));
        }
        else
        {
            var user = await db.Users.SingleAsync(u => u.UID == uid).ConfigureAwait(false);
            user.Alias = desiredVanityUid;
            db.Update(user);
            await db.SaveChangesAsync().ConfigureAwait(false);
            eb.WithColor(Color.Green);
            eb.WithTitle("Vanity UID successfully set");
            eb.WithDescription($"Your Vanity UID for \"{uid}\" was successfully changed to \"{desiredVanityUid}\"." + Environment.NewLine + Environment.NewLine
                + "For changes to take effect you need to reconnect to the GagSpeak service.");
            AddHome(cb);
        }

        await ModifyModalInteraction(eb, cb).ConfigureAwait(false);
    }
}
