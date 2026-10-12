
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GagspeakAPI.Reporting;
using GagspeakAPI.User;
using GagspeakShared.Data;
using GagspeakShared.Models;
using GagspeakShared.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Text;
using ServerDiscordConfig = GagspeakShared.Utils.Configuration.DiscordConfig;

namespace GagspeakDiscord;

public record ReportedProfileInfo(ReportedUserProfile Report, UserProfile Profile, AccountClaimAuth ReportedAuth, AccountClaimAuth ReporterAuth);

// Helpers class
public partial class ReportWizard
{
    #region Database Help
    /// <summary>
    ///   Grabs the database context from the CK VM host.
    /// </summary>
    private async Task<GagspeakDbContext> GetDbContext()
        => await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

    private async Task<ReportedProfileInfo> GetProfileReport(GagspeakDbContext db, int reportId)
    {
        var query = from report in db.ProfileReports.AsNoTracking()
                    where report.ReportID == reportId
                    join reportedProfile in db.UserProfileData
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

    private async Task<AccountReputation> LoadTargetReputationAsync(GagspeakDbContext db, string userUid)
    {
        var auth = await db.Auth.SingleAsync(a => a.UserUID == userUid).ConfigureAwait(false);
        return await db.AccountReputation.SingleAsync(r => r.UserUID == auth.PrimaryUserUID).ConfigureAwait(false);
    }

    private async Task<AccountReputation> LoadReporterReputationAsync(GagspeakDbContext db, ReportedUserProfile report)
    {
        var auth = await db.Auth.SingleAsync(a => a.UserUID == report.ReportingUserUID).ConfigureAwait(false);
        return await db.AccountReputation.SingleAsync(r => r.UserUID == auth.PrimaryUserUID).ConfigureAwait(false);
    }

    private async Task StrikeReputation(AccountReputation rep, StrikeKind kind)
    {
        var utcNow = DateTime.UtcNow;

        // Increment strikes and determine which bucket we're working with
        int strikeCount = kind switch
        {
            StrikeKind.ProfileViewing => ++rep.ProfileViewStrikes,
            StrikeKind.ProfileEditing => ++rep.ProfileEditStrikes,
            StrikeKind.ChatUsage => ++rep.ChatStrikes,

            StrikeKind.FalseReport =>
                ++rep.FalseReportStrikes,
            _ => 0
        };

        // Based on how many strikes exist for this catagory on that user, the duration gets longer.
        // So if they get reported 1 time for profile viewing, a strike to profile editing will only have a 12h timeout.
        TimeSpan punishmentDuration = strikeCount switch
        {
            1 => TimeSpan.FromHours(12),
            2 => TimeSpan.FromDays(3),
            3 => TimeSpan.FromDays(7),
            4 or 5 => TimeSpan.FromDays(14),
            _ => TimeSpan.Zero
        };

        // Apply punishment
        switch (kind)
        {
            case StrikeKind.ProfileViewing:
                rep.ProfileViewTimeout = utcNow + punishmentDuration;
                if (strikeCount >= 3)
                    rep.ProfileViewing = false;
                break;

            case StrikeKind.ProfileEditing:
                rep.ProfileEditTimeout = utcNow + punishmentDuration;
                if (strikeCount >= 3)
                    rep.ProfileEditing = false;
                break;

            case StrikeKind.ChatUsage:
                rep.ChatTimeout = utcNow + punishmentDuration;
                if (strikeCount >= 3)
                    rep.ChatUsage = false;
                break;

            case StrikeKind.FalseReport:
                // No timeout, just tracking
                break;
        }

        // Auto-ban if global strike threshold hit
        if (rep.ShouldBan)
            rep.IsBanned = true;
    }

    public static void ApplyImmediateBan(AccountReputation rep, StrikeKind type)
    {
        DateTime utcNow = DateTime.UtcNow;

        switch (type)
        {
            case StrikeKind.ProfileViewing:
                rep.ProfileViewing = false;
                rep.ProfileViewStrikes++;
                break;

            case StrikeKind.ProfileEditing:
                rep.ProfileEditing = false;
                rep.ProfileEditStrikes++;
                break;

            case StrikeKind.ChatUsage:
                rep.ChatUsage = false;
                rep.ChatStrikes++;
                break;

            case StrikeKind.ImmidiateBan:
                rep.IsBanned = true;
                rep.ProfileViewing = false;
                rep.ProfileEditing = false;
                rep.ChatUsage = false;
                break;
        }
    }

    public UserReputation ToApi(AccountReputation dbState)
        => dbState is null ? new() : new UserReputation()
        {
            IsVerified = dbState.IsVerified,
            IsBanned = dbState.IsBanned,
            WarningStrikes = dbState.WarningStrikes,
            ProfileViewing = dbState.ProfileViewing,
            ProfileViewTimeout = dbState.ProfileViewTimeout,
            ProfileViewStrikes = dbState.ProfileViewStrikes,
            ProfileEditing = dbState.ProfileEditing,
            ProfileEditTimeout = dbState.ProfileEditTimeout,
            ProfileEditStrikes = dbState.ProfileEditStrikes,
            RadarUsage = dbState.RadarUsage,
            RadarTimeout = dbState.RadarTimeout,
            RadarStrikes = dbState.RadarStrikes,
            ChatUsage = dbState.ChatUsage,
            ChatTimeout = dbState.ChatTimeout,
            ChatStrikes = dbState.ChatStrikes
        };
    #endregion

    [ComponentInteraction("reports-refresh")]
    public async Task RefreshReportWizard()
    {
        if (Context.Interaction is not SocketMessageComponent component || component.Channel is not SocketTextChannel channel)
            return;

        IUserMessage message = component.Message;
        // Run the same CreateOrUpdateReportWizard, but pass the message to update in-place
        await _botServices.CreateOrUpdateReportWizardWithExistingMessage(channel, message).ConfigureAwait(false);
        // Acknowledge the button to prevent "interaction failed"
        await component.DeferAsync().ConfigureAwait(false);
    }

    private void AddProfileHome(ComponentBuilder cb)
        => cb.WithButton("Go Back", "reports-profile-home:false", ButtonStyle.Secondary, new Emoji("🏠"));

    private void AddChatHome(ComponentBuilder cb)
        => cb.WithButton("Go Back", "reports-chat-home:false", ButtonStyle.Secondary, new Emoji("🏠"));

    private async Task LogReportResolutionAsync(ReportedUserProfile report, StrikeKind actionTaken, bool bannedContent)
    {
        _logger.LogDebug("Report Wizard: Getting ChannelForResolutions");
        // Fetch the channel ID using your configuration pattern
        if (_discordConfig.GetValue<ulong?>(nameof(ServerDiscordConfig.ChannelForResolutions)) is not { } resolutionsChannel)
        {
            _logger.LogWarning("Report Wizard: No channel configured for ChannelForResolutions");
            return;
        }

        // Attempt to fetch the channel using the bot's context
        if (await Context.Client.GetChannelAsync(resolutionsChannel).ConfigureAwait(false) is not IMessageChannel logChannel)
        {
            _logger.LogWarning($"ChannelForResolutions {resolutionsChannel} could not be found or accessed.");
            return;
        }

        using var db = await GetDbContext().ConfigureAwait(false);
        var reportedDisplay = report.ReportedUserUID;
        var reporterDisplay = report.ReportingUserUID;
        if (await db.Auth.AsNoTracking().SingleOrDefaultAsync(a => a.UserUID == report.ReportedUserUID).ConfigureAwait(false) is { } reportedAuth)
            if (await db.AccountClaimAuth.AsNoTracking().SingleOrDefaultAsync(a => a.User.UID == reportedAuth.PrimaryUserUID).ConfigureAwait(false) is { } reportedClaim)
                reportedDisplay = $"{report.ReportedUserUID} (<@{reportedClaim.DiscordId}>)";
        // Check other end.
        if (await db.Auth.AsNoTracking().SingleOrDefaultAsync(a => a.UserUID == report.ReportingUserUID).ConfigureAwait(false) is { } reporterAuth)
            if (await db.AccountClaimAuth.AsNoTracking().SingleOrDefaultAsync(a => a.User.UID == reporterAuth.PrimaryUserUID).ConfigureAwait(false) is { } reporterClaim)
                reporterDisplay = $"{report.ReportingUserUID} (<@{reporterClaim.DiscordId}>)";

        var attachments = new List<FileAttachment>();
        var streamsToDispose = new List<MemoryStream>();
        var embeds = new List<Embed>();

        try
        {
            // Build the Primary Embed
            var mainEb = new EmbedBuilder()
                .WithTitle($"Report Resolved: #{report.ReportID}")
                .WithColor(GetColorForAction(actionTaken))
                .AddField("Reported User", reportedDisplay, inline: true)
                .AddField("Reporter User", reporterDisplay, inline: true)
                .AddField("Report Reason", string.IsNullOrWhiteSpace(report.ReportReason) ? "-" : report.ReportReason)
                .AddField("Action Taken", $"{actionTaken} (ForceBan: {bannedContent})")
                .AddField("Moderator", $"<@{Context.User.Id}>")
                .WithCurrentTimestamp();

            // Extract Snapshot Images
            if (!string.IsNullOrEmpty(report.SnapshotIcon))
            {
                var iconStream = new MemoryStream(Convert.FromBase64String(report.SnapshotIcon));
                streamsToDispose.Add(iconStream);
                var iconName = $"snap_icon_{Guid.NewGuid():N}.png";
                attachments.Add(new FileAttachment(iconStream, iconName));
                mainEb.WithThumbnailUrl($"attachment://{iconName}");
            }

            if (!string.IsNullOrEmpty(report.SnapshotBg))
            {
                var bgStream = new MemoryStream(Convert.FromBase64String(report.SnapshotBg));
                streamsToDispose.Add(bgStream);
                var bgName = $"snap_bg_{Guid.NewGuid():N}.png";
                attachments.Add(new FileAttachment(bgStream, bgName));
                mainEb.WithImageUrl($"attachment://{bgName}");
            }

            embeds.Add(mainEb.Build());

            // Build Live Embed
            if (await db.UserProfileData.FirstOrDefaultAsync(p => p.UserUID == report.ReportedUserUID).ConfigureAwait(false) is { } reportedProfile)
            {
                var liveEb = new EmbedBuilder()
                    .WithTitle("Live Profile State at Time of Resolution")
                    .WithColor(Color.DarkGrey);

                if (!string.IsNullOrEmpty(reportedProfile.Base64ProfilePic))
                {
                    var liveIconStream = new MemoryStream(Convert.FromBase64String(reportedProfile.Base64ProfilePic));
                    streamsToDispose.Add(liveIconStream);
                    var liveIconName = $"live_icon_{Guid.NewGuid():N}.png";
                    attachments.Add(new FileAttachment(liveIconStream, liveIconName));
                    liveEb.WithThumbnailUrl($"attachment://{liveIconName}");
                }

                //if (!string.IsNullOrEmpty(reportedProfile.Base64Background))
                //{
                //    var liveBgStream = new MemoryStream(Convert.FromBase64String(reportedProfile.Base64Background));
                //    streamsToDispose.Add(liveBgStream);
                //    var liveBgName = $"live_bg_{Guid.NewGuid():N}.png";
                //    attachments.Add(new FileAttachment(liveBgStream, liveBgName));
                //    liveEb.WithImageUrl($"attachment://{liveBgName}");
                //}

                embeds.Add(liveEb.Build());
            }

            // Attach the Theme Template from Snapshot
            var profileV1 = ProfilesJsonEx.ReadUserV1(report.SnapshotJson);
            if (profileV1.Theme is not null)
            {
                var serializer = Newtonsoft.Json.JsonSerializer.Create(ProfilesJsonEx.Settings);
                string templateJObject = new JObject
                {
                    ["Version"] = 0,
                    ["TemplateID"] = Guid.NewGuid().ToString(),
                    ["CreationDate"] = DateTime.UtcNow,
                    ["Name"] = $"Reported Theme (Snapshot)",
                    ["Author"] = report.ReportedUserUID,
                    ["Theme"] = JObject.FromObject(profileV1.Theme, serializer),
                }.ToString(Newtonsoft.Json.Formatting.Indented);

                var themeStream = new MemoryStream(Encoding.UTF8.GetBytes(templateJObject));
                streamsToDispose.Add(themeStream);

                var themeFileName = $"Theme_{report.ReportedUserUID}_Snapshot.json";
                attachments.Add(new FileAttachment(themeStream, themeFileName, "Copiable theme template"));
            }

            // Dispatch the log with all compiled embeds and file attachments
            await logChannel.SendFilesAsync(embeds: [.. embeds], attachments: attachments).ConfigureAwait(false);
        }
        finally
        {
            // Ensure all memory streams are disposed to prevent memory leaks
            foreach (var stream in streamsToDispose)
                stream.Dispose();
        }
    }

    private Color GetColorForAction(StrikeKind action)
    {
        return action switch
        {
            StrikeKind.ProfileViewing or StrikeKind.ProfileEditing => Color.Orange,
            StrikeKind.ImmidiateBan => Color.Red,
            StrikeKind.FalseReport => Color.DarkRed,
            _ => Color.Default
        };
    }

    /// <summary>
    ///   Validates the interaction being made with the discord bot
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
        eb.WithDescription("This session has expired since you have either again pressed \"Start\" on the " +
            "initial message or the bot has been restarted.\n\n" +
            "Please use the newly started interaction or start a new one.");
        eb.WithColor(Color.Red);
        ComponentBuilder cb = new();
        await ModifyInteraction(eb, cb).ConfigureAwait(false);

        return false;
    }

    /// <summary>
    ///   Modifies the interaction with embed and component builder. <br />
    ///   Because this is a regular interaction, we will check against the IComponentInteraction type.
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

    private async Task ResolveReportInteraction(string resultMessage)
    {
        var eb = new EmbedBuilder()
            .WithTitle("Report Resolved")
            .WithColor(Color.Green)
            .WithDescription(resultMessage);

        var cb = new ComponentBuilder();
        AddProfileHome(cb);
        await ((Context.Interaction) as IComponentInteraction).UpdateAsync(m =>
        {
            m.Attachments = new List<FileAttachment>();
            m.Embed = eb.Build();
            m.Components = cb.Build();
        }).ConfigureAwait(false);
    }
}