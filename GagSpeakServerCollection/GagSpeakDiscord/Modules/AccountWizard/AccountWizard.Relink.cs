using Discord;
using Discord.Interactions;
using GagSpeakDiscord.Modules.Popups;
using GagspeakShared.Data;
using GagspeakShared.Models;
using GagspeakShared.Utils;
using Microsoft.EntityFrameworkCore;

namespace GagspeakDiscord.Modules.AccountWizard;

// NOTICE: This file has no current use, and was derived from mare's discord bot framework.
// THe file is however setup to handle the verification queue, so it may be useful in the future.
public partial class AccountWizard
{
    [ComponentInteraction("wizard-relink")]
    public async Task ComponentRelink()
    {
        if (!(await ValidateInteraction().ConfigureAwait(false))) return;

        _logger.LogInformation("{method}:{userId}", nameof(ComponentRelink), Context.Interaction.User.Id);

        EmbedBuilder eb = new();
        eb.WithTitle("Relink");
        eb.WithColor(Color.Magenta);
        eb.WithDescription("This currently serves no purpose. If you need to relink your account, please contact an administrator.");
        ComponentBuilder cb = new();
        AddHome(cb);
        //cb.WithButton("Start Relink", "wizard-relink-start", ButtonStyle.Primary, emote: new Emoji("🔗"));
        await ModifyInteraction(eb, cb).ConfigureAwait(false);
    }

    [ComponentInteraction("wizard-relink-start")]
    public async Task ComponentRelinkStart()
    {
        if (!(await ValidateInteraction().ConfigureAwait(false))) return;

        _logger.LogInformation("{method}:{userId}", nameof(ComponentRelinkStart), Context.Interaction.User.Id);

        using var db = GetDbContext();
        db.AccountClaimAuth.RemoveRange(db.AccountClaimAuth.Where(u => u.DiscordId == Context.User.Id));
        _botServices.DiscordVerifiedUsers.TryRemove(Context.User.Id, out _);
        _botServices.DiscordRelinkInitialKeyMapping.TryRemove(Context.User.Id, out _);
        await db.SaveChangesAsync().ConfigureAwait(false);

        await RespondWithModalAsync<InitialKeyModal>("wizard-relink-lodestone-modal").ConfigureAwait(false);
    }

    [ModalInteraction("wizard-relink-lodestone-modal")]
    public async Task ModalRelink(InitialKeyModal lodestoneModal)
    {
        if (!(await ValidateInteraction().ConfigureAwait(false))) return;

        /* _logger.LogInformation("{method}:{userId}:{url}", nameof(ModalRelink), Context.Interaction.User.Id, lodestoneModal.LodestoneUrl);*/

        EmbedBuilder eb = new();
        eb.WithColor(Color.Purple);
        var result = await HandleRelinkModalAsync(eb, lodestoneModal).ConfigureAwait(false);
        ComponentBuilder cb = new();
        cb.WithButton("Cancel", "wizard-relink", ButtonStyle.Secondary, emote: new Emoji("❌"));
        if (result.Success) cb.WithButton("Verify", "wizard-relink-verify:" + result.LodestoneAuth + "," + result.UID, ButtonStyle.Primary, emote: new Emoji("✅"));
        else cb.WithButton("Try again", "wizard-relink-start", ButtonStyle.Primary, emote: new Emoji("🔁"));
        await ModifyModalInteraction(eb, cb).ConfigureAwait(false);
    }

    [ComponentInteraction("wizard-relink-verify:*,*")]
    public async Task ComponentRelinkVerify(string verificationCode, string uid)
    {
        if (!(await ValidateInteraction().ConfigureAwait(false))) return;

        _logger.LogInformation("{method}:{userId}:{uid}:{verificationCode}", nameof(ComponentRelinkVerify), Context.Interaction.User.Id, uid, verificationCode);


        _botServices.VerificationQueue.Enqueue(new KeyValuePair<ulong, Func<DiscordBotServices, Task>>(Context.User.Id,
            (services) => HandleVerifyRelinkAsync(Context.User.Id, verificationCode, services)));
        EmbedBuilder eb = new();
        ComponentBuilder cb = new();
        eb.WithColor(Color.Purple);
        cb.WithButton("Cancel", "wizard-relink", ButtonStyle.Secondary, emote: new Emoji("❌"));
        cb.WithButton("Check", "wizard-relink-verify-check:" + verificationCode + "," + uid, ButtonStyle.Primary, emote: new Emoji("❓"));
        eb.WithTitle("Relink Verification Pending");
        eb.WithDescription("Please wait until the bot verifies your registration." + Environment.NewLine
            + "Press \"Check\" to check if the verification has been already processed" + Environment.NewLine + Environment.NewLine
            + "__This will not advance automatically, you need to press \"Check\".__");
        await ModifyInteraction(eb, cb).ConfigureAwait(false);
    }

    [ComponentInteraction("wizard-relink-verify-check:*,*")]
    public async Task ComponentRelinkVerifyCheck(string verificationCode, string uid)
    {
        if (!(await ValidateInteraction().ConfigureAwait(false))) return;

        _logger.LogInformation("{method}:{userId}:{uid}:{verificationCode}", nameof(ComponentRelinkVerifyCheck), Context.Interaction.User.Id, uid, verificationCode);

        EmbedBuilder eb = new();
        ComponentBuilder cb = new();
        bool stillEnqueued = _botServices.VerificationQueue.Any(k => k.Key == Context.User.Id);
        bool verificationRan = _botServices.DiscordVerifiedUsers.TryGetValue(Context.User.Id, out bool verified);
        if (!verificationRan)
        {
            if (stillEnqueued)
            {
                eb.WithColor(Color.Gold);
                eb.WithTitle("Your relink verification is still pending");
                eb.WithDescription("Please try again and click Check in a few seconds");
                cb.WithButton("Cancel", "wizard-relink", ButtonStyle.Secondary, emote: new Emoji("❌"));
                cb.WithButton("Check", "wizard-relink-verify-check:" + verificationCode + "," + uid, ButtonStyle.Primary, emote: new Emoji("❓"));
            }
            else
            {
                eb.WithColor(Color.Red);
                eb.WithTitle("Something went wrong");
                eb.WithDescription("Your relink verification was processed but did not arrive properly. Please try to start the relink process from the start.");
                cb.WithButton("Restart", "wizard-relink", ButtonStyle.Primary, emote: new Emoji("🔁"));
            }
        }
        else
        {
            if (verified)
            {
                eb.WithColor(Color.Green);
                using var db = _services.CreateScope().ServiceProvider.GetRequiredService<GagspeakDbContext>();
                var (_, key) = await HandleRelinkUser(db, uid).ConfigureAwait(false);
                eb.WithTitle($"Relink successful, your UID is again: {uid}");
                eb.WithDescription("This is your private secret key. Do not share this private secret key with anyone. **If you lose it, it is irrevocably lost.**"
                                             + Environment.NewLine + Environment.NewLine
                                             + $"**{key}**"
                                             + Environment.NewLine + Environment.NewLine
                                             + "Enter this key in GagSpeak and hit save to connect to the service."
                                             + Environment.NewLine
                                             + "Have fun.");
                AddHome(cb);
            }
            else
            {
                eb.WithColor(Color.Gold);
                eb.WithTitle("Failed to verify relink");
                eb.WithDescription("The bot was not able to find the required verification code on your Lodestone profile." + Environment.NewLine + Environment.NewLine
                    + "Please restart your relink process, make sure to save your profile _twice_ for it to be properly saved." + Environment.NewLine + Environment.NewLine
                    + "The code the bot is looking for is" + Environment.NewLine + Environment.NewLine
                    + "**" + verificationCode + "**");
                cb.WithButton("Cancel", "wizard-relink", emote: new Emoji("❌"));
                cb.WithButton("Retry", "wizard-relink-verify:" + verificationCode + "," + uid, ButtonStyle.Primary, emote: new Emoji("🔁"));
            }
        }

        await ModifyInteraction(eb, cb).ConfigureAwait(false);
    }

    private async Task<(bool Success, string LodestoneAuth, string UID)> HandleRelinkModalAsync(EmbedBuilder embed, InitialKeyModal arg)
    {
        ulong userId = Context.User.Id;
        await Task.Run(() => Console.WriteLine("Hello World"));
        // dont care about it right now
        /*     var lodestoneId = ParseCharacterIdFromLodestoneUrl(arg.LodestoneUrl);
             if (lodestoneId == null)
             {
                 embed.WithTitle("Invalid Lodestone URL");
                 embed.WithDescription("The lodestone URL was not valid. It should have following format:" + Environment.NewLine
                     + "https://eu.finalfantasyxiv.com/lodestone/character/YOUR_LODESTONE_ID/");
                 return (false, string.Empty, string.Empty);
             }
             // check if userid is already in db
             using var scope = _services.CreateScope();

             var hashedLodestoneId = StringUtils.Sha256String(lodestoneId.ToString());

             using var db = scope.ServiceProvider.GetService<GagspeakDbContext>();

             if (!db.AccountClaimAuth.Any(a => a.HashedLodestoneId == hashedLodestoneId))
             {
                 // character already in db
                 embed.WithTitle("Impossible operation");
                 embed.WithDescription("This lodestone character does not exist in the database.");
                 return (false, string.Empty, string.Empty);
             }

             var expectedUser = await db.AccountClaimAuth.Include(u => u.User).SingleAsync(u => u.HashedLodestoneId == hashedLodestoneId).ConfigureAwait(false);

             string lodestoneAuth = await GenerateLodestoneAuth(Context.User.Id, hashedLodestoneId, db).ConfigureAwait(false);
             // check if lodestone id is already in db
             embed.WithTitle("Authorize your character for relinking");
             embed.WithDescription("Add following key to your character profile at https://na.finalfantasyxiv.com/lodestone/my/setting/profile/"
                                   + Environment.NewLine + Environment.NewLine
                                   + $"**{lodestoneAuth}**"
                                   + Environment.NewLine + Environment.NewLine
                                   + $"**! THIS IS NOT THE KEY YOU HAVE TO ENTER IN GAGSPEAK !**"
                                   + Environment.NewLine
                                   + "__You can delete the entry from your profile after verification.__"
                                   + Environment.NewLine + Environment.NewLine
                                   + "The verification will expire in approximately 15 minutes. If you fail to verify the relink will be invalidated and you have to relink again.");
             _botServices.DiscordRelinkInitialKeyMapping[Context.User.Id] = lodestoneId.ToString();

             return (true, lodestoneAuth, expectedUser.User.UID);*/
        return (false, null, null);
    }

    private async Task HandleVerifyRelinkAsync(ulong userid, string authString, DiscordBotServices services)
    {
        await Task.Run(() => Console.WriteLine("Hello World"));
        /*      DONT CARE ABOUT IT RIGHT NOW
                services.DiscordVerifiedUsers.Remove(userid, out _);
                if (services.DiscordRelinkInitialKeyMapping.ContainsKey(userid))
                {
                    var randomServer = services.LodestoneServers[random.Next(services.LodestoneServers.Length)];
                    var response = await req.GetAsync($"https://{randomServer}.finalfantasyxiv.com/lodestone/character/{services.DiscordRelinkInitialKeyMapping[userid]}").ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (content.Contains(authString))
                        {
                            services.DiscordVerifiedUsers[userid] = true;
                            services.DiscordRelinkInitialKeyMapping.TryRemove(userid, out _);
                        }
                        else
                        {
                            services.DiscordVerifiedUsers[userid] = false;
                        }
                    }
                }*/
    }

    private async Task<(string, string)> HandleRelinkUser(GagspeakDbContext db, string uid)
    {
        var oldLodestoneAuth = await db.AccountClaimAuth.Include(u => u.User).SingleOrDefaultAsync(u => u.User.UID == uid && u.DiscordId != Context.User.Id).ConfigureAwait(false);
        var newLodestoneAuth = await db.AccountClaimAuth.Include(u => u.User).SingleOrDefaultAsync(u => u.DiscordId == Context.User.Id).ConfigureAwait(false);

        var user = oldLodestoneAuth.User;

        var computedHash = StringUtils.Sha256String(StringUtils.GenerateRandomString(64) + DateTime.UtcNow.ToString());
        var auth = new Auth()
        {
            HashedKey = StringUtils.Sha256String(computedHash),
            User = user,
        };

        var previousAuth = await db.Auth.SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);
        if (previousAuth != null)
        {
            db.Remove(previousAuth);
        }

        newLodestoneAuth.StartedAt = null;
        newLodestoneAuth.User = user;
        db.Update(newLodestoneAuth);
        db.Remove(oldLodestoneAuth);
        await db.Auth.AddAsync(auth).ConfigureAwait(false);

        _botServices.Logger.LogInformation("User relinked: {userUID}", user.UID);

        await db.SaveChangesAsync().ConfigureAwait(false);

        return (user.UID, computedHash);
    }
}
