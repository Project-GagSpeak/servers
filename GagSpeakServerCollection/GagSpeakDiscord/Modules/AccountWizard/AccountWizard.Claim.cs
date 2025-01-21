using Discord.Interactions;
using Discord;
using GagspeakShared.Data;
using Microsoft.EntityFrameworkCore;
using GagspeakShared.Utils;
using GagspeakShared.Models;
using GagSpeakDiscord.Modules.Popups;

namespace GagspeakDiscord.Modules.AccountWizard;

/// <summary>
/// This class will be heavily modified to remove lodestone linking entirely, and be replaced with a verification modal.
/// </summary>
public partial class AccountWizard
{
    /// <summary>
    /// The component interaction for what will display when we press the interaction button for claiming an account.
    /// </summary>
    [ComponentInteraction("wizard-claim")]
    public async Task ComponentRegister()
    {
        // as always, validate the interaction. If its valid, log it, if it isnt, return.
        if (!(await ValidateInteraction().ConfigureAwait(false))) return;

        _logger.LogInformation("{method}:{userId}", nameof(ComponentRegister), Context.Interaction.User.Id);

        // create a new embed builder to update the current one with the new menu display.
        EmbedBuilder eb = new();
        eb.WithColor(Color.Magenta);
        eb.WithTitle("Start Claim Process");
        eb.WithDescription("The shop Mistress has put in extra effort to make sure end users put in less work!\n"+
            "In other words, you wont need to login and mess with your lodestone page for verification!" + Environment.NewLine + Environment.NewLine
            + "**To claim your account, please make sure:**" + Environment.NewLine + Environment.NewLine
            + " 🔘 You are logged into FFXIV and connected to the GagSpeak Server" + Environment.NewLine + Environment.NewLine
            + " 🔘 You located your primary account's secret key (Under Account Management in the settings window of GagSpeak's UI)");
        ComponentBuilder cb = new();
        AddHome(cb); // add the home button so we can go back at any point
        cb.WithButton("Begin the Claim Process", "wizard-register-start", ButtonStyle.Primary, emote: new Emoji("💌")); // button to start claim process.
        await ModifyInteraction(eb, cb).ConfigureAwait(false); // modify the message currently displaying the homepage details.
    }

    /// <summary>
    /// Called upon whenever the user hits the "Begin the Account Claim Process" button in the claim account menu.
    /// </summary>
    [ComponentInteraction("wizard-register-start")]
    public async Task ComponentRegisterStart()
    {
        if (!(await ValidateInteraction().ConfigureAwait(false))) return;

        _logger.LogInformation("{method}:{userId}", nameof(ComponentRegisterStart), Context.Interaction.User.Id);

        // grab the database context
        using var db = GetDbContext();
        // if we enter this menu at all, for whatever reason, we should remove the user from the claimauth table, and the initial key mapping.
        var entry = await db.AccountClaimAuth.SingleOrDefaultAsync(u => u.DiscordId == Context.User.Id).ConfigureAwait(false);
        if (entry != null)
            db.AccountClaimAuth.Remove(entry);
        _botServices.DiscordInitialKeyMapping.TryRemove(Context.User.Id, out _);
        _botServices.DiscordVerifiedUsers.TryRemove(Context.User.Id, out _);

        // save changes to the DB and fire the initial Key modal.
        await db.SaveChangesAsync().ConfigureAwait(false);

        // display the popup model asking for the initial secret key the plugin generated for the user.
        await RespondWithModalAsync<InitialKeyModal>("wizard-claim-account-modal").ConfigureAwait(false);
    }

    /// <summary>
    /// Called upon by the registration start model, and will prompt the user to provide them with the initial key generated for them.
    /// </summary>
    /// <param name="initialkeymodal"> the modal to display the initial key prompt</param>
    [ModalInteraction("wizard-claim-account-modal")]
    public async Task ModalRegister(InitialKeyModal initialKeyModal)
    {
        if (!(await ValidateInteraction().ConfigureAwait(false))) return;

        _logger.LogInformation("{method}:{userId}:{initial_key}", nameof(ModalRegister), Context.Interaction.User.Id, initialKeyModal.InitialKeyStr);

        // create the embed builder where we make the color purple, and then prompt the user with the registration modal.
        EmbedBuilder eb = new();
        eb.WithColor(Color.Magenta);
        // provide the registration modal and await the response, returns if the registration was successful or not, and the verification code.
        bool success = await HandleRegisterModalAsync(eb, initialKeyModal).ConfigureAwait(false);
        // while we handle the registration for the modal, construct the component builder allowing the user to cancel, verify, or try again.
        ComponentBuilder cb = new();
        cb.WithButton("Cancel", "wizard-claim", ButtonStyle.Secondary, emote: new Emoji("❌"));

        // if the modal returned sucessful, allow them to verify (pass in item2, the verification)
        if (success) cb.WithButton("Send Verification Code to Client", "wizard-claim-verify-start:"+initialKeyModal.InitialKeyStr, ButtonStyle.Primary, emote: new Emoji("✅"));
        // otherwise, ask them to try again, stepping back to where we ask them for the initial key. Often we get here is the key is not correct.
        else cb.WithButton("Try again", "wizard-claim-start", ButtonStyle.Primary, emote: new Emoji("🔁"));
        await ModifyModalInteraction(eb, cb).ConfigureAwait(false);
    }

    /// <summary>
    /// Fired once we hit the verify button in our registration modal screen. Displays a new model for inserting the verification code.
    /// </summary>
    /// <param name="verificationCode"> the code passed in that we will be comparing against the prompt requesting it to validate our account. </param>
    [ComponentInteraction("wizard-claim-verify-start:*")]
    public async Task ComponentRegisterVerify(string initialKeyStr)
    {
        if (!(await ValidateInteraction().ConfigureAwait(false))) return;

        _logger.LogInformation("{method}:{userId}:{verificationcode}", nameof(ComponentRegisterVerify), Context.Interaction.User.Id, initialKeyStr);

        // contain logic for sending the updated information to the client here
        using var scope = _services.CreateScope();
        using var db = scope.ServiceProvider.GetService<GagspeakDbContext>();

        // now that we had ensured we have a valid initial key, we can generate a accountclaimauth row in the database for our user
        string verificationCode = await GenerateAccountClaimAuth(Context.User.Id, initialKeyStr, db).ConfigureAwait(false);
        // store the verification code
        var tuple = _botServices.DiscordInitialKeyMapping[Context.User.Id];
        tuple.Item2 = verificationCode;
        // store it back in the mapping
        _botServices.DiscordInitialKeyMapping[Context.User.Id] = tuple;

        // display the popup model asking for the initial secret key the plugin generated for the user.
        await RespondWithModalAsync<VerificationModal>("wizard-claim-verify-check").ConfigureAwait(false);
    }

    /// <summary>
    /// Called upon after submitting a verification code, returning if the outcome is sucessful or not.
    /// </summary>
    /// <param name="verificationCode"></param>
    /// <returns></returns>
    [ModalInteraction("wizard-claim-verify-check")]
    public async Task ComponentRegisterVerifyCheck(VerificationModal verificationCodeModal)
    {
        if (!(await ValidateInteraction().ConfigureAwait(false))) return;

        _logger.LogInformation("{method}:{userId}:{uid}", nameof(ComponentRegisterVerifyCheck), Context.Interaction.User.Id, verificationCodeModal);

        EmbedBuilder eb = new();
        ComponentBuilder cb = new();
        eb.WithColor(Color.Magenta);
        // await the finish of the model
        (bool success, string uid, string key) = await HandleVerificationModalAsync(eb, verificationCodeModal).ConfigureAwait(false);

        if (success)
        {
            eb.WithColor(Color.Green);
            eb.WithTitle($"Account Sucessfully Claimed. The UID : {uid} now belongs to you.");
            eb.WithDescription("You claimed this account with its corrisponding secret key." + Environment.NewLine + Environment.NewLine
                + $"**Save this secret key, as if you lose it, all sub-profiles will also be lost.**"
                + Environment.NewLine + Environment.NewLine
                + $"**Your UID is:** {uid}" + Environment.NewLine
                + $"**Your Secret Key is:** {key}");
            AddHome(cb);
        }
        else
        {
            eb.WithColor(Color.Gold);
            eb.WithTitle("Failed to Claim Account");
            eb.WithDescription("CK GagSpeak Services was unable to claim your account as the verification time expired, "
                + "or the code was incorrect." + Environment.NewLine + Environment.NewLine
                + "Please restart your verification process.");
            cb.WithButton("Cancel", "wizard-claim", ButtonStyle.Secondary, emote: new Emoji("❌"));
        }
        await ModifyModalInteraction(eb, cb).ConfigureAwait(false);

    }

    /// <summary>
    /// Called by the start of the registration when asking for the initial key.
    /// </summary>
    /// <param name="embed"> the embed builder for the message </param>
    /// <param name="arg"> the initial key modal as an argument passed in. </param>
    /// <returns> if it was sucessful or not, and the verification code string. </returns>
    private async Task<bool> HandleRegisterModalAsync(EmbedBuilder embed, InitialKeyModal arg)
    {
        // at this point in time, remember that we have no accountClaimAuth object, only a user and auth object.
        using var scope = _services.CreateScope();
        using var db = scope.ServiceProvider.GetService<GagspeakDbContext>();
        // if it is empty, fail the handle.
        if (arg.InitialKeyStr == null)
        {
            embed.WithTitle("Initial key was not provided in the modal. Try again.");
            return false;
        }
        // otherwise, if the initial key is not in our database, then someone is trying to forge it
        else if (!db.Auth.Any(a => a.HashedKey == arg.InitialKeyStr))
        {
            embed.WithTitle("This secret key is not being used by any users, or you pasted it in wrong.");
            return false;
        }
        else if (db.AccountClaimAuth.Any(a => a.InitialGeneratedKey == arg.InitialKeyStr))
        {
            embed.WithTitle("This secret key has already been claimed by another user.");
            return false;
        }

        embed.WithTitle("Complete Account Claim Process");
        embed.WithDescription("CK GagSpeak Services has generated a verification code for you. This will be sent to your currently logged in client."
                              + Environment.NewLine + Environment.NewLine
                              + $"**When you are ready for the verification code to be pushed to your game, press the button below.**"
                              + Environment.NewLine + Environment.NewLine
                              + "Verification will expire in 10minutes starting now.\nIf you fail to verify, you'll have to register again.");

        // store the initial key to the initialkeymapping.
        _botServices.DiscordInitialKeyMapping[Context.User.Id] = (arg.InitialKeyStr, string.Empty);

        // return sucess with the verification code.
        return true;
    }

    private async Task<(bool, string, string)> HandleVerificationModalAsync(EmbedBuilder eb, VerificationModal verificationModal)
    {
        // fetch the verification code
        _botServices.DiscordInitialKeyMapping.TryGetValue(Context.User.Id, out var keyValue);
        var initialKey = keyValue.Item1;
        _logger.LogInformation("Initial key {key} for {userid}", initialKey, Context.User.Id);
        var verificationCode = keyValue.Item2;
        _logger.LogInformation("Verification code {code} for {userid}", verificationCode, Context.User.Id);

        _botServices.DiscordVerifiedUsers.Remove(Context.User.Id, out _);
        // first make sure the user is still valid in the context from the services
        if(!_botServices.DiscordInitialKeyMapping.ContainsKey(Context.User.Id))
        {
            _logger.LogInformation("User {userid} does not have an initial key mapping", Context.User.Id);
            eb.WithTitle("You have not started the registration process, or you timed out. Please try again.");
            _botServices.DiscordVerifiedUsers[Context.User.Id] = false;
            return (false, string.Empty, string.Empty);
        }

        // check to see if the answers match
        if (verificationModal.VerificationCodeStr != verificationCode)
        {
            _logger.LogInformation("Verification code {code} did not match the one generated for {userid}", verificationModal.VerificationCodeStr, Context.User.Id);
            eb.WithTitle("The verification code you entered does not match the one generated for you, try registration again.");
            _botServices.DiscordVerifiedUsers[Context.User.Id] = false;
            return (false, string.Empty, string.Empty);
        }
        
        // otherwise the keys did match, so we should clear the claimauth started at, and set the user to an actual user, and the verification code to null.
        _logger.LogInformation("Verification code {code} matched the one generated for {userid}", verificationModal.VerificationCodeStr, Context.User.Id);
        _botServices.DiscordVerifiedUsers[Context.User.Id] = true;
        // handle adding this user to the database
        using var db = GetDbContext();
        var accountClaimAuth = db.AccountClaimAuth.SingleOrDefault(u => u.DiscordId == Context.User.Id);

        _logger.LogInformation("User {userid} has key {key}", Context.User.Id, initialKey);
        // to grab the user, fetch the associated auth object from the db
        var auth = await db.Auth.SingleOrDefaultAsync(u => u.HashedKey == initialKey).ConfigureAwait(false);
        User user = await db.Users.SingleOrDefaultAsync(u => u.UID == auth.UserUID).ConfigureAwait(false);

        accountClaimAuth.InitialGeneratedKey = null; // for security reasons
        accountClaimAuth.StartedAt = null; // clear time started, meaning verification worked.
        accountClaimAuth.User = user; // set user to reflect appropriate user
        accountClaimAuth.VerificationCode = null; // set verification code to null

        // set last logged in time for the user to right now
        accountClaimAuth.User.LastLoggedIn = DateTime.UtcNow;

        await db.SaveChangesAsync().ConfigureAwait(false);

        // return sucess with the user's UID
        return (true, user.UID, initialKey);
    }

    /// <summary>
    /// Called upon whenever we want to add a new secondary profile to the database. (potentially make another function for verifying the primary claim)
    /// </summary>
    /// <param name="db"> the database context </param>
    /// <returns> the new userUID, and the secretKey associated with it. </returns>
    private async Task<(string, string)> HandleAddUser(GagspeakDbContext db)
    {
        var accountClaimAuth = db.AccountClaimAuth.SingleOrDefault(u => u.DiscordId == Context.User.Id);

        var user = new User();

        var hasValidUid = false;
        while (!hasValidUid)
        {
            var uid = StringUtils.GenerateRandomString(10);
            if (db.Users.Any(u => u.UID == uid || u.Alias == uid)) continue;
            user.UID = uid;
            hasValidUid = true;
        }

        user.LastLoggedIn = DateTime.UtcNow;

        var computedHash = StringUtils.Sha256String(StringUtils.GenerateRandomString(64) + DateTime.UtcNow.ToString());
        var auth = new Auth()
        {
            HashedKey = StringUtils.Sha256String(computedHash),
            User = user,
        };

        await db.Users.AddAsync(user).ConfigureAwait(false);
        await db.Auth.AddAsync(auth).ConfigureAwait(false);

        _botServices.Logger.LogInformation("User registered: {userUID}", user.UID);

        accountClaimAuth.StartedAt = null;
        accountClaimAuth.User = user;
        accountClaimAuth.VerificationCode = null;

        await db.SaveChangesAsync().ConfigureAwait(false);

        _botServices.DiscordVerifiedUsers.Remove(Context.User.Id, out _);

        return (user.UID, computedHash);
    }
}
