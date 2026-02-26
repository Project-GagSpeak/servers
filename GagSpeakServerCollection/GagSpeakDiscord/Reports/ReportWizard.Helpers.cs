
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GagspeakAPI.Enums;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakShared.Data;
using GagspeakShared.Models;
using GagspeakShared.Services;
using GagspeakShared.Utils.Configuration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text;
using ServerDiscordConfig = GagspeakShared.Utils.Configuration.DiscordConfig;

namespace GagspeakDiscord;

public record ReportedProfileInfo(ReportedProfile Report, UserProfileData Profile, AccountClaimAuth ReportedAuth, AccountClaimAuth ReporterAuth);
public record ReportedChatInfo(ReportedChat Report, UserProfileData Profile, AccountClaimAuth ReportedAuth, AccountClaimAuth ReporterAuth);

// Helpers class
public partial class ReportWizard
{
    /// <summary>
    ///     Grabs the database context from the GagSpeak VM host.
    /// </summary>
    private async Task<GagspeakDbContext> GetDbContext()
        => await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

    private async Task<ReportedProfileInfo> GetProfileReport(GagspeakDbContext db, int reportId)
    {
        var query = from report in db.ReportedProfiles.AsNoTracking()
                    where report.ReportID == reportId
                    join reportedProfile in db.ProfileData
                        on report.ReportedUserUID equals reportedProfile.UserUID
                    join reportedAuth in db.Auth
                        on report.ReportedUserUID equals reportedAuth.UserUID
                    join reporterAuth in db.Auth
                        on report.ReportingUserUID equals reporterAuth.UserUID
                    join reportedClaim in db.AccountClaimAuth
                        on reportedAuth.PrimaryUserUID equals reportedClaim.User!.UID
                    join reporterClaim in db.AccountClaimAuth
                        on reporterAuth.PrimaryUserUID equals reporterClaim.User!.UID
                    select new ReportedProfileInfo(
                        report,
                        reportedProfile,
                        reportedClaim,
                        reporterClaim);

        // Return single result or null if not found
        return await query.SingleOrDefaultAsync().ConfigureAwait(false);
    }

    private async Task<Dictionary<int, ReportedChatInfo>> GetChatReports(GagspeakDbContext db)
    {
        var query = from report in db.ReportedChats.AsNoTracking()
                        // Reported user's profile
                    join reportedProfile in db.ProfileData
                        on report.ReportedUserUID equals reportedProfile.UserUID
                    // Auth rows (resolve PrimaryUserUID)
                    join reportedAuth in db.Auth
                        on report.ReportedUserUID equals reportedAuth.UserUID
                    join reporterAuth in db.Auth
                        on report.ReportingUserUID equals reporterAuth.UserUID
                    // Account claims via PrimaryUser
                    join reportedClaim in db.AccountClaimAuth
                        on reportedAuth.PrimaryUserUID equals reportedClaim.User!.UID
                    join reporterClaim in db.AccountClaimAuth
                        on reporterAuth.PrimaryUserUID equals reporterClaim.User!.UID
                    select new
                    {
                        report.ReportID,
                        Report = report,
                        ReportedProfile = reportedProfile,
                        ReportedUserClaim = reportedClaim,
                        ReporterUserClaim = reporterClaim
                    };
        var result = await query.AsNoTracking().ToDictionaryAsync(x => x.ReportID,
            x => new ReportedChatInfo(x.Report, x.ReportedProfile, x.ReportedUserClaim, x.ReporterUserClaim)).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    ///     Adds a home button to return to the report selection menu.
    /// </summary>
    private void AddProfileHome(ComponentBuilder cb)
        => cb.WithButton("Go Back", "reports-profile-home:false", ButtonStyle.Secondary, new Emoji("🏠"));

    private void AddChatHome(ComponentBuilder cb)
        => cb.WithButton("Go Back", "reports-chat-home:false", ButtonStyle.Secondary, new Emoji("🏠"));


    /// <summary>
    ///     Vlidates the interaction being made with the discord bot
    /// </summary>
    private async Task<bool> ValidateInteraction()
    {
        // if the context of the interaction is not an interaction component, return true
        if (Context.Interaction is not IComponentInteraction componentInteraction) return true;

        // otherwise, if the user is in the valid interactions list, and the interaction id is the same as the message id, return true
        if (_botServices.ValidInteractions.TryGetValue(Context.User.Id, out ulong interactionId) && interactionId == componentInteraction.Message.Id)
        {
            return true;
        }

        // otherwise, modify the interaction to show that the session has expired
        EmbedBuilder eb = new();
        eb.WithTitle("Session expired");
        eb.WithDescription("This session has expired since you have either again pressed \"Start\" on the initial message or the bot has been restarted." + Environment.NewLine + Environment.NewLine
            + "Please use the newly started interaction or start a new one.");
        eb.WithColor(Color.Red);
        ComponentBuilder cb = new();
        await ModifyInteraction(eb, cb).ConfigureAwait(false);

        return false;
    }

    /// <summary>
    ///     Modifies the interaction with embed and component builder. <br />
    ///     Because this is a regular interaction, we will check against the IComponentInteraction type.
    /// </summary>
    private async Task ModifyInteraction(EmbedBuilder eb, ComponentBuilder cb, List<FileAttachment> attachments = null)
    {
        await ((Context.Interaction) as IComponentInteraction).UpdateAsync(m =>
        {
            m.Attachments = attachments;
            m.Embed = eb.Build();
            m.Components = cb.Build();
        }).ConfigureAwait(false);
    }
}