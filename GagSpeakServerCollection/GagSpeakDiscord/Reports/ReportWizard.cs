
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GagspeakAPI.Enums;
using GagspeakAPI.Hub;
using GagspeakServer.Hubs;
using GagspeakShared.Data;
using GagspeakShared.Models;
using GagspeakShared.Services;
using GagspeakShared.Utils;
using GagspeakShared.Utils.Configuration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using SundouleiaAPI.Reporting;
using System.Globalization;
using System.Text;
using ServerDiscordConfig = GagspeakShared.Utils.Configuration.DiscordConfig;

namespace GagspeakDiscord;

public partial class ReportWizard : InteractionModuleBase
{
    private readonly ILogger<ReportWizard> _logger;
    private readonly IServiceProvider _services;
    private readonly DiscordBotServices _botServices;
    private readonly IConfigurationService<ServerConfiguration> _gsConfigService;
    private readonly IConfigurationService<ServerDiscordConfig> _discordConfig;
    private readonly IHubContext<GagspeakHub> _hubContext;
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly IDbContextFactory<GagspeakDbContext> _dbContextFactory;

    public ReportWizard(ILogger<ReportWizard> logger, IServiceProvider services, DiscordBotServices botServices,
        IConfigurationService<ServerConfiguration> gsConfig, IConfigurationService<ServerDiscordConfig> discordConfig,
        IHubContext<GagspeakHub> hubContent, IConnectionMultiplexer multiplexer, 
        IDbContextFactory<GagspeakDbContext> dbContextFactory)
    {
        _logger = logger;
        _services = services;
        _botServices = botServices;
        _gsConfigService = gsConfig;
        _discordConfig = discordConfig;
        _hubContext = hubContent;
        _multiplexer = multiplexer;
        _dbContextFactory = dbContextFactory;
    }

    // Launcher for the Report Wizard
    [ComponentInteraction("reports-profile-home:*")]
    public async Task ProfileReportWizardHome(bool init = false)
    {
        // if the interaction was not valid, then return.
        if (!init && !(await ValidateInteraction().ConfigureAwait(false))) return;

        // Interaction was successful, so log it. 
        _logger.LogInformation("{method}:{userId}", nameof(ProfileReportWizardHome), Context.Interaction.User.Id);

        // fetch the database context to see if they already have a claimed account.
        using var db = await GetDbContext().ConfigureAwait(false);
        var reports = db.ProfileReports.ToList();
        var eb = new EmbedBuilder()
            .WithTitle("Profile Reports")
            .WithColor(Color.DarkRed)
            .WithDescription(reports.Count is 0 ? "⚠️ No active reports." : "📖 Select a report to review.");

        var cb = new ComponentBuilder();
        if (reports.Count > 0)
        {
            var select = new SelectMenuBuilder()
                .WithCustomId("reports-profile-selector")
                .WithPlaceholder("Select a report");

            foreach (var report in reports.Take(25))
                select.AddOption(label: report.ReportedUserUID, value: $"{report.ReportID}", description: $"Reported by {report.ReportingUserUID}");

            cb.WithSelectMenu(select); // only add if >0 options
        }

        // if this message is being generated in response to the user pressing "Start" on the initial message,
        // send the message as an ephemeral message, meaning a reply personalized so only the user can see it.
        if (init)
        {
            await RespondAsync(embed: eb.Build(), components: cb.Build(), ephemeral: true).ConfigureAwait(false);
            IUserMessage resp = await GetOriginalResponseAsync().ConfigureAwait(false);
            // Validate the interaction for this user as their current ephemeral interaction.
            _botServices.ValidInteractions[Context.User.Id] = resp.Id;
            _logger.LogInformation($"Emphemeral Interaction Started: {resp.Id}");
        }
        else
        {
            await ModifyInteraction(eb, cb).ConfigureAwait(false);
        }
    }

    // We want to create an initial entry point after clicking on a users report.
    [ComponentInteraction("reports-profile-selector")]
    public async Task SelectionProfileReport(string reportId)
    {
        // By default, navigate to the overview when first opening the report.
        await ProcessProfileReportViewAsync(reportId, "overview").ConfigureAwait(false);
    }

    // An additional entry point for the dropdown view selector
    [ComponentInteraction("report-profile-view-*")]
    public async Task ViewProfileReport(string id, string[] selectedValues)
    {
        string view = selectedValues.FirstOrDefault() ?? "overview";
        await ProcessProfileReportViewAsync(id, view).ConfigureAwait(false);
    }

    // The centralized processing method where we capture memory streams and other data.
    private async Task ProcessProfileReportViewAsync(string id, string view)
    {
        if (!await ValidateInteraction().ConfigureAwait(false))
            return;

        // Load the report with all related data
        if (!int.TryParse(id, CultureInfo.InvariantCulture, out var reportId))
        {
            await RespondAsync("Invalid report ID.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        // Get the report details and render the report.
        using var db = await GetDbContext().ConfigureAwait(false);
        var reportData = await GetProfileReport(db, reportId).ConfigureAwait(false);

        var eb = new EmbedBuilder();
        var cb = new ComponentBuilder();
        var attachments = new List<FileAttachment>();
        var streamsToDispose = new List<MemoryStream>();

        try
        {
            // Render the profile report for the view we have spesifically requested.
            await RenderProfileReport(eb, reportData, attachments, streamsToDispose, view).ConfigureAwait(false);

            // Our dropdown selector action
            AddProfileReportViewSelector(cb, reportId, view);
            // Any leftover action buttons we still have once this is done.
            AddProfileReportActionSelector(cb, reportId);
            AddProfileHome(cb);

            await ModifyInteraction(eb, cb, attachments).ConfigureAwait(false);
        }
        finally
        {
            // Dispose of the render streams
            foreach (var stream in streamsToDispose)
                stream.Dispose();
        }
    }

    // Renderer for the reported profile, allowing for toggles between snapshot and live states.
    private async Task RenderProfileReport(EmbedBuilder eb, ReportedProfileInfo data, List<FileAttachment> attachments, List<MemoryStream> streams, string view)
    {
        // Title + Color
        eb.WithTitle($"Report: {data.Report.ReportID}")
          .WithColor(Color.Magenta);

        if (string.Equals(view, "overview", StringComparison.Ordinal))
        {
            eb.WithDescription("**Report Overview**");

            var reportTime = new DateTimeOffset(data.Report.ReportTime, TimeSpan.Zero);
            eb.AddField("Report Time", string.Create(CultureInfo.InvariantCulture, $"<t:{reportTime.ToUnixTimeSeconds()}:F>"));

            string reportedDisplay = data.ReportedAuth?.DiscordId != null
                ? $"{data.Report.ReportedUserUID} (<@{data.ReportedAuth.DiscordId}>)"
                : data.Report.ReportedUserUID;

            string reporterDisplay = data.ReporterAuth?.DiscordId != null
                ? $"{data.Report.ReportingUserUID} (<@{data.ReporterAuth.DiscordId}>)"
                : data.Report.ReportingUserUID;

            eb.AddField("Reported User", reportedDisplay, inline: true);
            eb.AddField("Reporter User", reporterDisplay, inline: true);
            eb.AddField("Report Reason", string.IsNullOrWhiteSpace(data.Report.ReportReason) ? "-" : data.Report.ReportReason);
        }
        // Handle comparisons with any snapshot -vs- live data if possible.
        else
        {
            var isSnapshot = string.Equals(view, "snapshot", StringComparison.Ordinal);
            eb.WithDescription(isSnapshot ? "**Snapshot Profile State**" : "**Live Profile State**");

            // There is snapshot data, and then raw data.
            var profileJson = isSnapshot ? data.Report.SnapshotJson : data.Profile.Description;
            var profileV1 = ProfilesJsonEx.ReadUserV1(profileJson);

            eb.AddField("Description", string.IsNullOrWhiteSpace(profileV1.Description) ? "-" : profileV1.Description);

            // Capture the icon and background for the attachments.
            var iconData = isSnapshot ? data.Report.SnapshotIcon : data.Profile.Base64ProfilePic;
            //var bgData = isSnapshot ? data.Report.SnapshotBg : data.Profile.Base64Background;

            // Handle Icon (Thumbnail)
            if (!string.IsNullOrEmpty(iconData))
            {
                var iconName = $"{data.Report.ReportedUserUID}_icon_{view}_{Guid.NewGuid():N}.png";
                var stream = new MemoryStream(Convert.FromBase64String(iconData));
                streams.Add(stream);
                attachments.Add(new FileAttachment(stream, iconName));
                eb.WithThumbnailUrl($"attachment://{iconName}");
            }

            // Handle Banner (Main Image)
            //if (!string.IsNullOrEmpty(bgData))
            //{
            //    var bgName = $"{data.Report.ReportedUserUID}_banner_{view}_{Guid.NewGuid():N}.png";
            //    var stream = new MemoryStream(Convert.FromBase64String(bgData));
            //    streams.Add(stream);
            //    attachments.Add(new FileAttachment(stream, bgName));
            //    eb.WithImageUrl($"attachment://{bgName}");
            //}

            // Attach the theme as copiable data to verify if reportable behavior was made with shapes.
            if (profileV1.Theme is not null)
            {
                var serializer = Newtonsoft.Json.JsonSerializer.Create(ProfilesJsonEx.Settings);
                string templateJObject = new JObject
                {
                    ["Version"] = 0,
                    ["TemplateID"] = Guid.NewGuid().ToString(),
                    ["CreationDate"] = DateTime.UtcNow,
                    ["Name"] = $"Reported Theme ({(isSnapshot ? "Snapshot" : "Live")})",
                    ["Author"] = data.Report.ReportedUserUID,
                    ["Theme"] = JObject.FromObject(profileV1.Theme, serializer),
                }.ToString(Newtonsoft.Json.Formatting.Indented);

                // Encode the template to stream to attach as a .json file.
                var themeStream = new MemoryStream(Encoding.UTF8.GetBytes(templateJObject));
                // Add to streams so it gets disposed of after the pose.
                streams.Add(themeStream);
                // Set the filename and attach it.
                var themeFileName = $"Theme_{data.Report.ReportedUserUID}_{(isSnapshot ? "Snapshot" : "Live")}.json";
                attachments.Add(new FileAttachment(themeStream, themeFileName, "Copiable theme template"));
                // Notify the moderator that the file is attached
                eb.AddField("Theme Template", $"Attached as `{themeFileName}`. Click to expand and copy.");
            }
        }
    }

    private void AddProfileReportViewSelector(ComponentBuilder cb, int reportId, string currentView)
    {
        var menu = new SelectMenuBuilder()
            .WithCustomId($"report-profile-view-{reportId}")
            .WithPlaceholder("Select view...")
            .AddOption("Report Overview", "overview", "Basic report details and reason", isDefault: string.Equals(currentView, "overview", StringComparison.Ordinal))
            .AddOption("Snapshot State", "snapshot", "Profile data exactly as it was reported", isDefault: string.Equals(currentView, "snapshot", StringComparison.Ordinal))
            .AddOption("Live State", "live", "Current real-time profile data", isDefault: string.Equals(currentView, "live", StringComparison.Ordinal));

        cb.WithSelectMenu(menu);
    }

    private void AddProfileReportActionSelector(ComponentBuilder cb, int reportId)
    {
        var actionMenu = new SelectMenuBuilder()
            .WithCustomId($"report-profile-action-{reportId}")
            .WithPlaceholder("Select an action to take...")
            // The value of the option is what gets passed to your interaction handler
            .AddOption("Dismiss", "dismiss", "Dismiss this report without taking action", new Emoji("✅"))
            .AddOption("Strike Viewing", "strike-viewing", "Issue a viewing strike to the reported user", new Emoji("⚠️"))
            .AddOption("Strike Editing", "strike-editing", "Issue an editing strike to the reported user", new Emoji("⚠️"))
            .AddOption("Ban Viewing", "ban-viewing", "Ban the reported user from viewing profiles", new Emoji("🔨"))
            .AddOption("Ban Editing", "ban-editing", "Ban the reported user from editing their profile", new Emoji("🔨"))
            .AddOption("Flag Reporter", "flag-reporter", "Flag the reporting user for a false report", new Emoji("🚩"));

        cb.WithSelectMenu(actionMenu);
    }

    [ComponentInteraction("report-profile-action-*")]
    public async Task HandleProfileReportAction(string reportIdStr, string[] selectedValues)
    {
        if (!await ValidateInteraction().ConfigureAwait(false))
            return;

        if (!int.TryParse(reportIdStr, CultureInfo.InvariantCulture, out var reportId))
        {
            await RespondAsync("Invalid report ID.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        // Isolate the first selected action to take and use that.
        string action = selectedValues.FirstOrDefault();
        if (string.IsNullOrEmpty(action))
        {
            await RespondAsync("No action selected.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        switch (action)
        {
            case "dismiss":
                await ProcessDismissProfileReportAsync(reportId).ConfigureAwait(false);
                break;
            case "strike-viewing":
                await ProcessStrikeProfileViewingAsync(reportId, false).ConfigureAwait(false);
                break;
            case "strike-editing":
                await ProcessStrikeProfileEditingAsync(reportId, true).ConfigureAwait(false);
                break;
            case "ban-viewing":
                await ProcessStrikeProfileEditingAsync(reportId, false).ConfigureAwait(false);
                break;
            case "ban-editing":
                await ProcessStrikeProfileEditingAsync(reportId, true).ConfigureAwait(false);
                break;
            case "flag-reporter":
                await ProcessFlagFalseReporterAsync(reportId).ConfigureAwait(false);
                break;
            default:
                await RespondAsync("Unknown action.", ephemeral: true).ConfigureAwait(false);
                return;
        }
    }

    // Dismissing reports.
    private async Task ProcessDismissProfileReportAsync(int id)
    {
        using var db = await GetDbContext().ConfigureAwait(false);
        if (await db.ProfileReports.FindAsync(id).ConfigureAwait(false) is not { } report)
        {
            await ResolveReportInteraction("Report not found.").ConfigureAwait(false);
            return;
        }

        // Clear the profile flag
        if (await db.UserProfileData.SingleOrDefaultAsync(p => p.UserUID == report.ReportedUserUID).ConfigureAwait(false) is { } profile)
            profile.FlaggedForReport = false;

        // Remove the report from the database without taking any actions.
        db.Remove(report);
        await db.SaveChangesAsync().ConfigureAwait(false);

        // Could likely perform a callback here to all connected users of the hub context so they know the choice has been made.
        await ResolveReportInteraction($"Report {id} dismissed.").ConfigureAwait(false);
    }

    private async Task ProcessStrikeProfileViewingAsync(int id, bool immidiateBan)
    {
        using var db = await GetDbContext().ConfigureAwait(false);
        if (await db.ProfileReports.SingleOrDefaultAsync(r => r.ReportID == id).ConfigureAwait(false) is not { } report)
        {
            await ResolveReportInteraction("Report no longer exists.").ConfigureAwait(false);
            return;
        }

        if (await db.UserProfileData.SingleOrDefaultAsync(p => p.UserUID == report.ReportedUserUID).ConfigureAwait(false) is { } profile)
            profile.FlaggedForReport = false;

        // Strike the accounts reputation for profile viewing.
        var reputation = await LoadTargetReputationAsync(db, report.ReportingUserUID).ConfigureAwait(false);
        if (immidiateBan)
            ApplyImmediateBan(reputation, StrikeKind.ProfileViewing);
        else
            await StrikeReputation(reputation, StrikeKind.ProfileViewing).ConfigureAwait(false);

        await LogReportResolutionAsync(report, StrikeKind.ProfileViewing, immidiateBan).ConfigureAwait(false);
        db.ProfileReports.Remove(report);

        // If the account is banned after this interaction, grab the auth and ensure a ban is set.
        if (reputation.IsBanned)
        {
            _logger.LogDebug($"Reported User {report.ReportedUserUID} Gained a ProfileView strike, and now is marked as banned.");
            if (await db.AccountClaimAuth.Include(a => a.User).SingleOrDefaultAsync(a => a.User.UID == reputation.UserUID).ConfigureAwait(false) is { } claim)
                db.BannedRegistrations.Add(new BannedRegistrations() { DiscordId = claim.DiscordId.ToString() });
        }
        await db.SaveChangesAsync().ConfigureAwait(false);

        // Force the reported user to reconnect, getting their latest accountReputation / informed status of a ban.
        if (reputation.IsBanned)
        {
            await _hubContext.Clients.User(report.ReportedUserUID).SendAsync(nameof(IGagspeakHub.Callback_HardReconnectMessage),
                MessageSeverity.Warning, "After reviewing a profile report against you, CK decided to strike your Account. " +
                "This Account now holds over 5 strikes, warrenting an Account Ban. If you wish to appeal or discuss the action, " +
                "please DM an assistant.", ServerState.ForcedReconnect).ConfigureAwait(false);
        }
        else
        {
            await _hubContext.Clients.User(report.ReportedUserUID).SendAsync(nameof(IGagspeakHub.Callback_ReputationInfo),
                ToApi(reputation), "After reviewing a profile report against you, CK decided to strike your account " +
                "Profile Viewing Access. Gaining 3 strikes revokes your ability to view profiles permanently, and 5 total strikes result " +
                "in an Account Ban. Please DM an assistant with any questions.").ConfigureAwait(false);
        }

        await ResolveReportInteraction($"Profile viewing strike applied to `{report.ReportedUserUID}`.").ConfigureAwait(false);
    }

    private async Task ProcessStrikeProfileEditingAsync(int id, bool immidiateBan)
    {
        using var db = await GetDbContext().ConfigureAwait(false);
        if (await db.ProfileReports.SingleOrDefaultAsync(r => r.ReportID == id).ConfigureAwait(false) is not { } report)
        {
            await ResolveReportInteraction("Report no longer exists.").ConfigureAwait(false);
            return;
        }

        if (await db.UserProfileData.SingleOrDefaultAsync(p => p.UserUID == report.ReportedUserUID).ConfigureAwait(false) is { } profile)
        {
            profile.Base64ProfilePic = string.Empty;
            //profile.Base64Background = string.Empty;
            profile.FlaggedForReport = false;
            try
            {
                var data = ProfilesJsonEx.ReadUserV1(profile.Description);
                data.Description = string.Empty;
                var newJson = ProfilesJsonEx.WriteToJson(data);
                profile.Description = newJson;
            }
            catch (Exception)
            {
                await ResolveReportInteraction("Failed to parse profile JSON. Cannot clear description.").ConfigureAwait(false);
            }
        }

        // Cleanse the report and take action against it.
        var reputation = await LoadTargetReputationAsync(db, report.ReportingUserUID).ConfigureAwait(false);
        if (immidiateBan)
            ApplyImmediateBan(reputation, StrikeKind.ProfileEditing);
        else
            await StrikeReputation(reputation, StrikeKind.ProfileEditing).ConfigureAwait(false);
        await LogReportResolutionAsync(report, StrikeKind.ProfileEditing, immidiateBan).ConfigureAwait(false);
        db.ProfileReports.Remove(report);

        // If now banned, add to banned.
        if (reputation.IsBanned)
        {
            _logger.LogDebug($"Reported User {report.ReportedUserUID} Gained a ProfileEdit strike, and now is marked as banned.");
            if (await db.AccountClaimAuth.Include(a => a.User).SingleOrDefaultAsync(a => a.User.UID == reputation.UserUID).ConfigureAwait(false) is { } claim)
                db.BannedRegistrations.Add(new BannedRegistrations() { DiscordId = claim.DiscordId.ToString() });
        }

        await db.SaveChangesAsync().ConfigureAwait(false);

        if (reputation.IsBanned)
        {
            await _hubContext.Clients.User(report.ReportedUserUID).SendAsync(nameof(IGagspeakHub.Callback_HardReconnectMessage),
                MessageSeverity.Warning, "After reviewing a profile report against you, CK decided to strike your Account. " +
                "This Account now holds over 5 strikes, warrenting an Account Ban. If you wish to appeal or discuss the action, " +
                "please DM an assistant.", ServerState.ForcedReconnect).ConfigureAwait(false);
        }
        else
        {
            await _hubContext.Clients.User(report.ReportedUserUID).SendAsync(nameof(IGagspeakHub.Callback_ReputationInfo),
                ToApi(reputation), "After reviewing a profile report against you, CK decided to strike your account " +
                "Profile Editing Access. Gaining 3 strikes revokes your ability to edit profiles permanently, and 5 total strikes result " +
                "in an Account Ban. Please DM an assistant with any questions.", ServerState.ForcedReconnect).ConfigureAwait(false);
        }

        await ResolveReportInteraction($"Profile editing banned for `{report.ReportedUserUID}`.").ConfigureAwait(false);
    }

    private async Task ProcessFlagFalseReporterAsync(int id)
    {
        using var db = await GetDbContext().ConfigureAwait(false);
        if (await db.ProfileReports.SingleOrDefaultAsync(r => r.ReportID == id).ConfigureAwait(false) is not { } report)
        {
            await ResolveReportInteraction("Report no longer exists.").ConfigureAwait(false);
            return;
        }

        var reporterRep = await LoadReporterReputationAsync(db, report).ConfigureAwait(false);
        await StrikeReputation(reporterRep, StrikeKind.FalseReport).ConfigureAwait(false);
        await LogReportResolutionAsync(report, StrikeKind.FalseReport, false).ConfigureAwait(false);
        db.ProfileReports.Remove(report);
        await db.SaveChangesAsync().ConfigureAwait(false);

        // Send back to this user they got a strike, along with a forced reconnection.
        await _hubContext.Clients.User(report.ReportingUserUID).SendAsync(nameof(IGagspeakHub.Callback_ReputationInfo),
                ToApi(reporterRep), $"After review, CK decided your report on {report.ReportedUserUID} was falsely made. " +
            $"As a result a FalseReportStrike has been placed on you. This does not count towards your ReputationStrikes. " +
            $"DM an assistant if you wish to know why.", ServerState.ForcedReconnect).ConfigureAwait(false);

        await ResolveReportInteraction($"Reporter `{report.ReportingUserUID}` flagged for false report.").ConfigureAwait(false);
    }
}