
using Discord;
using Discord.WebSocket;
using GagspeakAPI.Enums;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakShared.Data;
using GagspeakShared.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GagspeakDiscord;

internal partial class DiscordBot
{
    /// <summary> Process the report button logic. </summary>
    private async Task ButtonExecutedHandler(SocketMessageComponent arg)
    {
        _logger.LogInformation("Attempted to process a button click.");
        // ensure this was a report button.
        string id = arg.Data.CustomId;
        if (!id.StartsWith("gagspeak-report-button", StringComparison.Ordinal)) return;

        // scope the user who interacted, and the dbContext within the scope.
        using IServiceScope scope = _services.CreateScope();
        using GagspeakDbContext dbContext = scope.ServiceProvider.GetRequiredService<GagspeakDbContext>();

        // Define the required role ID for access to this command
        ulong assistantRoleId = 884542694842597416; // Replace with your specific role ID
        ulong mistressRoleId = 878511993068355604; // Replace with your specific role ID

        // Get the user's ID and guild (server)
        ulong userId = arg.User.Id;
        SocketGuild guild = (arg.User as SocketGuildUser)?.Guild;

        if (guild is null)
        {
            _logger.LogWarning("Guild information could not be retrieved.");
            return;
        }

        // Fetch the user in the context of the guild
        SocketGuildUser guildUser = guild.GetUser(userId);

        // Check if the user has the required role
        if (guildUser is null || !guildUser.Roles.Any(r => r.Id == assistantRoleId || r.Id == mistressRoleId))
        {
            EmbedBuilder eb = new();
            eb.WithTitle("Cannot resolve report");
            eb.WithDescription($"<@{userId}>: You do not have the Assistant Role required to respond to this.");
            await arg.RespondAsync(embed: eb.Build()).ConfigureAwait(false);
            return;
        }
        // remove the common start string to get the lone leftovers, and parse through those entries.
        id = id.Remove(0, "gagspeak-report-button-".Length);
        string[] split = id.Split('-', StringSplitOptions.RemoveEmptyEntries);

        // grab the profile of the reported user.
        UserProfileData profile = await dbContext.ProfileData.SingleAsync(u => u.UserUID == split[1]).ConfigureAwait(false);
        ReportEntry report = await dbContext.ReportEntries.SingleAsync(u => u.ReportedUserUID == split[1]).ConfigureAwait(false);

        Embed embed = arg.Message.Embeds.First();

        EmbedBuilder builder = embed.ToEmbedBuilder();
        List<string> otherPairs = await dbContext.ClientPairs.Where(p => p.UserUID == split[1]).Select(p => p.OtherUserUID).ToListAsync().ConfigureAwait(false);
        switch (split[0])
        {
            // if we are dismissing the report, display that it was resolved as dismissed.
            case "dismissreport":
                builder.AddField("Resolution", $"Dismissed by <@{userId}>");
                builder.WithColor(Color.Green);
                profile.FlaggedForReport = false; // clear the flag.
                // do not notify anyone of this. don't want to rise suspicion.
                break;

            // if we deem the image to be a screwup, but not worth of a ban, clear the image.
            case "clearprofileimage":
                builder.AddField("Resolution", $"Profile Image has been cleared, and a warning strike has been added. Authorized by <@{userId}>");
                builder.WithColor(Color.Red);
                profile.Base64ProfilePic = string.Empty;
                profile.Description = string.Empty;
                profile.FlaggedForReport = false;
                await _gagspeakHubContext.Clients.User(split[1]).SendAsync(nameof(IGagspeakHub.Callback_ServerMessage),
                    MessageSeverity.Warning, "The CK Team has reviewed your KinkPlate and decided that your Picture / Description " +
                    "does not adhere to our guidelines. To help prevent these actions, we have cleared them and given you a warning. " +
                    "Warnings don't lead to a ban but tell us how many times this has happened. DM an assistant if you wish to know why.")
                    .ConfigureAwait(false);
                break;

            case "revokesocialfeatures":
                builder.AddField("Resolution", $"Profile Image & Description Access has revoked. Action Authorized by <@{userId}>");
                builder.WithColor(Color.Red);
                profile.Base64ProfilePic = string.Empty;
                profile.Description = string.Empty;
                profile.ProfileDisabled = true;
                profile.FlaggedForReport = false;
                await _gagspeakHubContext.Clients.User(split[1]).SendAsync(nameof(IGagspeakHub.Callback_ServerMessage),
                    MessageSeverity.Warning, "Your KinkPlate profile contained content that either harasses or has negative connotation towards " +
                    "another user. As a result, your ability to customize your profile has been revoked. If we recieve further reports," +
                    "your user will get banned.").ConfigureAwait(false);
                break;

            case "banuser":
                builder.AddField("Resolution", $"User has been banned by <@{userId}>");
                builder.WithColor(Color.DarkRed);
                Auth offendingUser = await dbContext.Auth.Include(a => a.AccountRep).SingleAsync(u => u.UserUID == split[1]).ConfigureAwait(false);
                offendingUser.AccountRep.IsBanned = true;
                profile.Base64ProfilePic = string.Empty;
                profile.Description = string.Empty;
                profile.FlaggedForReport = false;
                profile.ProfileDisabled = true;
                AccountClaimAuth reg = await dbContext.AccountClaimAuth.SingleAsync(u => u.User.UID == offendingUser.UserUID).ConfigureAwait(false);
                // revoke access to bot interactions & new account registrations
                dbContext.BannedRegistrations.Add(new BannedRegistrations()
                {
                    DiscordId = reg.DiscordId.ToString()
                });
                await _gagspeakHubContext.Clients.User(split[1]).SendAsync(nameof(IGagspeakHub.Callback_HardReconnectMessage),
                    MessageSeverity.Warning, "The CK Team has determined that your account must be banned from usage of GagSpeak Services. " +
                    "as a result, you will no longer be able to use GagSpeak on the currently logged in character with this account.", 
                    ServerState.ForcedReconnect).ConfigureAwait(false);
                break;

            case "flagreporter":
                builder.AddField("Resolution", $"Dismissed by <@{userId}>, But abusive reports lead to the user being flagged.");
                builder.WithColor(Color.DarkGreen);
                profile.FlaggedForReport = false;
                UserProfileData reportingUserProfile = await dbContext.ProfileData.SingleAsync(u => u.UserUID == split[2]).ConfigureAwait(false);
                reportingUserProfile.WarningStrikeCount++;
                await _gagspeakHubContext.Clients.User(split[2]).SendAsync(nameof(IGagspeakHub.Callback_ServerMessage),
                    MessageSeverity.Warning, "The CK Team has determined your report to be a miss-use of our system, or made with malicious " +
                    "attempt to bait another Kinkster into getting banned. As a result, a warning has been appended to your profile.").ConfigureAwait(false);
                break;
        }

        // remove the report from the dbcontext now that it has been processed by the server.
        if(report is not null)
        {
            _logger.LogInformation("Removing Report!");
            dbContext.Remove(report);
        }

        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        await _gagspeakHubContext.Clients.Users(otherPairs).SendAsync(nameof(IGagspeakHub.Callback_ProfileUpdated), new KinksterBase(new(split[1]))).ConfigureAwait(false);
        await _gagspeakHubContext.Clients.User(split[1]).SendAsync(nameof(IGagspeakHub.Callback_ProfileUpdated), new KinksterBase(new(split[1]))).ConfigureAwait(false);

        if(string.Equals(split[0], "flagreporter", StringComparison.OrdinalIgnoreCase))
        {
            await _gagspeakHubContext.Clients.Users(otherPairs).SendAsync(nameof(IGagspeakHub.Callback_ProfileUpdated), new KinksterBase(new(split[2]))).ConfigureAwait(false);
            await _gagspeakHubContext.Clients.User(split[2]).SendAsync(nameof(IGagspeakHub.Callback_ProfileUpdated), new KinksterBase(new(split[2]))).ConfigureAwait(false);
        }

        await arg.Message.ModifyAsync(msg =>
        {
            msg.Content = arg.Message.Content;
            msg.Components = null;
            msg.Embed = new Optional<Embed>(builder.Build());
        }).ConfigureAwait(false);
    }
}