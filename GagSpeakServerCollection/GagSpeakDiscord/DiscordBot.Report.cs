
using Discord;
using Discord.WebSocket;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Enums;
using GagspeakAPI.SignalR;
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
        // ensure this was a report button.
        var id = arg.Data.CustomId;
        if (!id.StartsWith("gagspeak-report-button", StringComparison.Ordinal)) return;

        // scope the user who interacted, and the dbContext within the scope.
        var userId = arg.User.Id;
        using var scope = _services.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<GagspeakDbContext>();
        var user = await dbContext.AccountClaimAuth.Include(u => u.User).SingleOrDefaultAsync(u => u.DiscordId == userId).ConfigureAwait(false);

        // if the user is null, respond that we cannot resolve the report. (possibly rework to accept any since its in a filtered channel anyways).
        if (user == null)
        {
            EmbedBuilder eb = new();
            eb.WithTitle($"Cannot resolve report");
            eb.WithDescription($"<@{userId}>: You have no rights to resolve this report");
            await arg.RespondAsync(embed: eb.Build()).ConfigureAwait(false);
            return;
        }

        // remove the common start string to get the lone leftovers, and parse through those entries.
        id = id.Remove(0, "gagspeak-report-button-".Length);
        var split = id.Split('-', StringSplitOptions.RemoveEmptyEntries);

        // grab the profile of the reported user.
        var profile = await dbContext.UserProfileData.SingleAsync(u => u.UserUID == split[1]).ConfigureAwait(false);

        var embed = arg.Message.Embeds.First();

        var builder = embed.ToEmbedBuilder();
        var otherPairs = await dbContext.ClientPairs.Where(p => p.UserUID == split[1]).Select(p => p.OtherUserUID).ToListAsync().ConfigureAwait(false);
        switch (split[0])
        {
            // if we are dismissing the report, display that it was resolved as dismissed.
            case "dismissreport":
                builder.AddField("Resolution", $"Dismissed by <@{userId}>");
                builder.WithColor(Color.Green);
                profile.FlaggedForReport = false; // clear the flag.
                break;

            // if we deem the image to be a screwup, but not worth of a ban, clear the image.
            case "clearprofileimage":
                builder.AddField("Resolution", $"Profile Image has been cleared, but not bad enough to ban user. Authorized by <@{userId}>");
                builder.WithColor(Color.Red);
                profile.Base64ProfilePic = null;
                profile.UserDescription = null;
                profile.ProfileTimeoutTimeStamp = DateTime.UtcNow;
                profile.FlaggedForReport = false;
                break;

            case "banprofile":
                builder.AddField("Resolution", $"Profile Access for the reported user has been revoked. Action Authorized by <@{userId}>");
                builder.WithColor(Color.Red);
                profile.Base64ProfilePic = null;
                profile.UserDescription = null;
                profile.ProfileDisabled = true;
                profile.FlaggedForReport = false;
                await _gagspeakHubContext.Clients.User(split[1]).SendAsync(nameof(IGagspeakHub.Client_ReceiveServerMessage),
                    MessageSeverity.Warning, "Your GagSpeak Profile contained content that harass or has a negative connotation towards " +
                    "another user. As a result, your ability to customize your profile has been revoked. Further exploitation of social " +
                    "features to attack other users will result in your user getting banned.")
                    .ConfigureAwait(false);
                break;

            case "banuser":
                builder.AddField("Resolution", $"User has been banned by <@{userId}>");
                builder.WithColor(Color.DarkRed);
                var offendingUser = await dbContext.Auth.SingleAsync(u => u.UserUID == split[1]).ConfigureAwait(false);
                offendingUser.IsBanned = true;
                profile.Base64ProfilePic = null;
                profile.UserDescription = null;
                profile.ProfileDisabled = true;
                var reg = await dbContext.AccountClaimAuth.SingleAsync(u => u.User.UID == offendingUser.UserUID).ConfigureAwait(false);
                // revoke access to bot interactions & new account registrations
                dbContext.BannedRegistrations.Add(new BannedRegistrations()
                {
                    DiscordId = reg.DiscordId.ToString()
                });
                await _gagspeakHubContext.Clients.User(split[1]).SendAsync(nameof(IGagspeakHub.Client_ReceiveServerMessage),
                    MessageSeverity.Warning, "Your GagSpeak User has been reported to commit harmful actions towards other GagSpeak users through " +
                    "miss using it's social features to attack or harm other players. Because of this, access to your GagSpeak account has been revoked." +
                    "Your playerCharacter has also been blacklisted as a result.")
                    .ConfigureAwait(false);
                break;

            case "flagreporter":
                builder.AddField("Resolution", $"Dismissed by <@{userId}>, But abusive reports lead to the user being flagged.");
                builder.WithColor(Color.DarkGreen);
                profile.FlaggedForReport = false;
                var reportingUser = await dbContext.Auth.SingleAsync(u => u.UserUID == split[2]).ConfigureAwait(false);
                reportingUser.IsBanned = true;
                var regReporting = await dbContext.AccountClaimAuth.SingleAsync(u => u.User.UID == reportingUser.UserUID).ConfigureAwait(false);
                dbContext.BannedRegistrations.Add(new BannedRegistrations()
                {
                    DiscordId = regReporting.DiscordId.ToString()
                });
                break;
        }

        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        await _gagspeakHubContext.Clients.Users(otherPairs).SendAsync(nameof(IGagspeakHub.Client_UserUpdateProfile), new UserDto(new(split[1]))).ConfigureAwait(false);
        await _gagspeakHubContext.Clients.User(split[1]).SendAsync(nameof(IGagspeakHub.Client_UserUpdateProfile), new UserDto(new(split[1]))).ConfigureAwait(false);

        await arg.Message.ModifyAsync(msg =>
        {
            msg.Content = arg.Message.Content;
            msg.Components = null;
            msg.Embed = new Optional<Embed>(builder.Build());
        }).ConfigureAwait(false);
    }
}