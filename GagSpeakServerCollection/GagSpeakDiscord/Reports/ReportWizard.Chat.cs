
using Discord;
using Discord.Interactions;
using GagspeakAPI.Enums;
using GagspeakAPI.Hub;
using GagspeakAPI.Reporting;
using GagspeakShared.Models;
using GagspeakShared.Utils;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SundouleiaAPI.Reporting;
using System.Globalization;
using System.Text.Json;
using ServerDiscordConfig = GagspeakShared.Utils.Configuration.DiscordConfig;

namespace GagspeakDiscord;

public partial class ReportWizard : InteractionModuleBase
{
    // Launcher for the Report Wizard
    [ComponentInteraction("reports-chat-home:*")]
    public async Task ChatReportWizardHome(bool init = false)
    {
        if (!init && !(await ValidateInteraction().ConfigureAwait(false))) return;
        _logger.LogInformation("{method}:{userId}", nameof(ChatReportWizardHome), Context.Interaction.User.Id);

        using var db = await GetDbContext().ConfigureAwait(false);
        var reports = await db.ChatReports.ToListAsync().ConfigureAwait(false);

        var eb = new EmbedBuilder()
            .WithTitle("Chat Reports")
            .WithColor(Color.Blue)
            .WithDescription(reports.Count is 0 ? "⚠️ No active chat reports." : "💬 Select a chat report to review.");

        var cb = new ComponentBuilder();
        if (reports.Count > 0)
        {
            var select = new SelectMenuBuilder()
                .WithCustomId("reports-chat-selector")
                .WithPlaceholder("Select a chat report");

            foreach (var report in reports.Take(25))
                select.AddOption(label: report.ReportedUserUID, value: $"{report.ReportID}", description: $"Reported by {report.ReportedUserUID}");

            cb.WithSelectMenu(select);
        }

        if (init)
        {
            await RespondAsync(embed: eb.Build(), components: cb.Build(), ephemeral: true).ConfigureAwait(false);
            IUserMessage resp = await GetOriginalResponseAsync().ConfigureAwait(false);
            _botServices.ValidInteractions[Context.User.Id] = resp.Id;
        }
        else
            await ModifyInteraction(eb, cb).ConfigureAwait(false);
    }

    [ComponentInteraction("reports-chat-selector")]
    public async Task SelectionChatReport(string reportIdStr)
    {
        if (!await ValidateInteraction().ConfigureAwait(false)) return;
        if (!int.TryParse(reportIdStr, CultureInfo.InvariantCulture, out var reportId))
        {
            await RespondAsync("Invalid report ID.", ephemeral: true).ConfigureAwait(false);
            return;
        }
        using var db = await GetDbContext().ConfigureAwait(false);
        if (await db.ChatReports.FindAsync(reportId).ConfigureAwait(false) is not { } report)
        {
            await RespondAsync("Report not found.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        // Format the Chat History
        var formattedChat = "No chat context available.";
        if (!string.IsNullOrWhiteSpace(report.ChatContextJson))
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var chatContext = JsonSerializer.Deserialize<JsonChatMsg[]>(report.ChatContextJson, options);
                if (chatContext != null && chatContext.Length > 0)
                {
                    var lines = new List<string>();
                    int currentLength = 0;
                    int maxLength = 3900; // Safe buffer for Discord's 4096 description limit

                    // Iterate backwards starting from the most recent message
                    for (int i = chatContext.Length - 1; i >= 0; i--)
                    {
                        var msg = chatContext[i];
                        var prefix = string.Equals(msg.MsgId, report.MessageId, StringComparison.OrdinalIgnoreCase) ? ">> " : "   ";
                        var line = $"{prefix}[{msg.TimeSentUTC:HH:mm:ss}] {msg.SenderName} ({msg.SenderUid}): {msg.Message}";

                        // Check if we have space for this line (plus 1 for the newline character)
                        if (currentLength + line.Length + 1 <= maxLength)
                        {
                            // Insert at the front of the list to maintain chronological reading order
                            lines.Insert(0, line);
                            currentLength += line.Length + 1;
                        }
                        else
                        {
                            // Out of space, add the truncation marker at the very top and stop
                            lines.Insert(0, "... [Older Context Truncated]");
                            break;
                        }
                    }
                    // Join the list into a single string with newlines
                    formattedChat = string.Join("\n", lines);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to deserialize chat context for report {report.ReportID}", ex);
                formattedChat = "Error parsing chat history.";
            }
        }

        var reportedDisplay = report.ReportedUserUID;
        var reporterDisplay = report.ReporterUID;
        if (await db.Auth.AsNoTracking().SingleOrDefaultAsync(a => a.UserUID == report.ReportedUserUID).ConfigureAwait(false) is { } reportedAuth)
            if (await db.AccountClaimAuth.AsNoTracking().SingleOrDefaultAsync(a => a.User.UID == reportedAuth.PrimaryUserUID).ConfigureAwait(false) is { } reportedClaim)
                reportedDisplay = $"{report.ReportedUserUID} (<@{reportedClaim.DiscordId}>)";
        // Check other end.
        if (await db.Auth.AsNoTracking().SingleOrDefaultAsync(a => a.UserUID == report.ReporterUID).ConfigureAwait(false) is { } reporterAuth)
            if (await db.AccountClaimAuth.AsNoTracking().SingleOrDefaultAsync(a => a.User.UID == reporterAuth.PrimaryUserUID).ConfigureAwait(false) is { } reporterClaim)
                reporterDisplay = $"{report.ReporterUID} (<@{reporterClaim.DiscordId}>)";

        // Compose the Embed and Message.
        var safeReason = string.IsNullOrWhiteSpace(report.ReportReason) ? "-" :
            report.ReportReason.Length > 1020 ? report.ReportReason.Substring(0, 1020) + "..." : report.ReportReason;

        var safeMessage = string.IsNullOrWhiteSpace(report.Message) ? "-" :
            report.Message.Length > 1020 ? report.Message.Substring(0, 1020) + "..." : report.Message;

        // Build the Embed
        var eb = new EmbedBuilder()
            .WithTitle($"Chat Report: #{report.ReportID}")
            .WithColor(Color.Blue)
            // Note the ```ansi code block here instead of ```text
            .WithDescription($"**Chat Context:**\n```ansi\n{formattedChat}\n```")
            .AddField("Reported User", reportedDisplay, inline: true)
            .AddField("Reporter User", reporterDisplay, inline: true)
            // Metadata
            .AddField("GsChatKind", report.Chat.ToString(), inline: true)
            .AddField("ChatLog ID", report.ChatLogId.ToString(), inline: true)
            .AddField("Message ID", report.MessageId.ToString(), inline: true)

            .AddField("Flagged Message", safeMessage)
            // The specific offense (Using safe truncated strings)
            .AddField("Report Reason", safeReason);

        var cb = new ComponentBuilder();
        AddChatReportActionSelector(cb, reportId);
        AddChatHome(cb);

        await ModifyInteraction(eb, cb).ConfigureAwait(false);
    }

    private void AddChatReportActionSelector(ComponentBuilder cb, int reportId)
    {
        var actionMenu = new SelectMenuBuilder()
            .WithCustomId($"report-chat-action-{reportId}")
            .WithPlaceholder("Select an action to take...")
            .AddOption("Dismiss", "dismiss", "Dismiss without taking action", new Emoji("✅"))
            .AddOption("Strike Usage", "strike-usage", "Issue a chat usage strike", new Emoji("⚠️"))
            .AddOption("Ban Usage", "ban-usage", "Immediately ban chat usage", new Emoji("🔨"))
            .AddOption("Flag Reporter", "flag-reporter", "Flag the reporting user for a false report", new Emoji("🚩"));
        cb.WithSelectMenu(actionMenu);
    }

    [ComponentInteraction("report-chat-action-*")]
    public async Task HandleChatReportAction(string reportIdStr, string[] selectedValues)
    {
        if (!await ValidateInteraction().ConfigureAwait(false)) return;
        if (!int.TryParse(reportIdStr, CultureInfo.InvariantCulture, out var reportId)) return;

        string action = selectedValues.FirstOrDefault();
        if (string.IsNullOrEmpty(action)) return;

        await ProcessChatReportActionAsync(reportId, action, StrikeKind.ChatUsage).ConfigureAwait(false);
    }

    private async Task ProcessChatReportActionAsync(int reportId, string action, StrikeKind strikeKind)
    {
        using var db = await GetDbContext().ConfigureAwait(false);
        if (await db.ChatReports.FindAsync(reportId).ConfigureAwait(false) is not { } report)
        {
            await RespondAsync("Report no longer exists.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        switch (action)
        {
            case "dismiss":
                await LogChatReportResolutionAsync(report, StrikeKind.Dismissed, false).ConfigureAwait(false);
                db.ChatReports.Remove(report);
                await db.SaveChangesAsync().ConfigureAwait(false);
                await ResolveReportInteraction($"Report {reportId} dismissed.").ConfigureAwait(false);
                break;

            case "strike-usage":
            case "ban-usage":
                bool immediateBan = string.Equals(action, "ban-usage", StringComparison.Ordinal);
                var targetRep = await LoadTargetReputationAsync(db, report.ReportedUserUID).ConfigureAwait(false);
                if (immediateBan)
                    ApplyImmediateBan(targetRep, strikeKind);
                else
                    await StrikeReputation(targetRep, strikeKind).ConfigureAwait(false);
                await LogChatReportResolutionAsync(report, strikeKind, immediateBan).ConfigureAwait(false);
                db.ChatReports.Remove(report);

                if (targetRep.IsBanned)
                {
                    if (await db.AccountClaimAuth.Include(a => a.User).SingleOrDefaultAsync(a => a.User.UID == targetRep.UserUID).ConfigureAwait(false) is { } claim)
                        db.BannedRegistrations.Add(new BannedRegistrations() { DiscordId = claim.DiscordId.ToString() });
                }

                await db.SaveChangesAsync().ConfigureAwait(false);

                string banStatusStr = targetRep.IsBanned
                    ? $"This Account now holds over 5 strikes, warranting an Account Ban. If you wish to appeal or discuss the action, please DM an assistant."
                    : $"Gaining 3 strikes revokes your ability to use GlobalChat permanently, and 5 total strikes result in an Account Ban. Please DM an assistant with any questions.";
                // Alert the user via Hub
                if (targetRep.IsBanned)
                {
                    await _hubContext.Clients.User(report.ReportedUserUID).SendAsync(nameof(IGagspeakHub.Callback_HardReconnectMessage), MessageSeverity.Warning,
                        $"After reviewing a chat report against you, CK decided to strike your account.\n{banStatusStr}", ServerState.ForcedReconnect).ConfigureAwait(false);
                }
                else
                {
                    await _hubContext.Clients.User(report.ReportedUserUID).SendAsync(nameof(IGagspeakHub.Callback_ReputationInfo),
                        ToApi(targetRep), $"After reviewing a chat report against you, CK decided to strike your account.\n{banStatusStr}").ConfigureAwait(false);
                }

                await ResolveReportInteraction($"Chat strike/ban applied to `{report.ReportedUserUID}`.").ConfigureAwait(false);
                break;

            case "flag-reporter":
                var reporterRep = await LoadTargetReputationAsync(db, report.ReportedUserUID).ConfigureAwait(false);
                await StrikeReputation(reporterRep, StrikeKind.FalseReport).ConfigureAwait(false);
                await LogChatReportResolutionAsync(report, StrikeKind.FalseReport, false).ConfigureAwait(false);
                db.ChatReports.Remove(report);
                await db.SaveChangesAsync().ConfigureAwait(false);

                await _hubContext.Clients.User(report.ReporterUID).SendAsync(nameof(IGagspeakHub.Callback_HardReconnectMessage),
                    MessageSeverity.Warning, $"After review, CK decided your chat report on {report.ReportedUserUID} was falsely made. " +
                    $"As a result a FalseReportStrike has been placed on you. This does not count towards your ReputationStrikes. " +
                    $"DM an assistant if you wish to know why.", ServerState.ForcedReconnect).ConfigureAwait(false);

                await ResolveReportInteraction($"Reporter `{report.ReportedUserUID}` flagged for false report.").ConfigureAwait(false);
                break;
            default:
                await RespondAsync("Unknown action.", ephemeral: true).ConfigureAwait(false);
                return;
        }
    }

    private async Task LogChatReportResolutionAsync(ReportedChat report, StrikeKind actionTaken, bool bannedContent)
    {
        _logger.LogDebug("Report Wizard: Getting ChannelForResolutions");
        if (_discordConfig.GetValue<ulong?>(nameof(ServerDiscordConfig.ChannelForResolutions)) is not { } resolutionsChannel)
        {
            _logger.LogWarning("Report Wizard: No channel configured for ChannelForResolutions");
            return;
        }
        if (await Context.Client.GetChannelAsync(resolutionsChannel).ConfigureAwait(false) is not IMessageChannel logChannel)
        {
            _logger.LogWarning($"ChannelForResolutions {resolutionsChannel} could not be found or accessed.");
            return;
        }

        // Format the Chat History
        var formattedChat = "No chat context available.";
        if (!string.IsNullOrWhiteSpace(report.ChatContextJson))
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var chatContext = JsonSerializer.Deserialize<JsonChatMsg[]>(report.ChatContextJson, options);

                if (chatContext != null && chatContext.Length > 0)
                {
                    var lines = new List<string>();
                    int currentLength = 0;
                    int maxLength = 3900; // Safe buffer for Discord's 4096 description limit

                    // Iterate backwards starting from the most recent message
                    for (int i = chatContext.Length - 1; i >= 0; i--)
                    {
                        var msg = chatContext[i];
                        var prefix = string.Equals(msg.MsgId, report.MessageId, StringComparison.OrdinalIgnoreCase) ? ">> " : "   ";
                        var line = $"{prefix}[{msg.TimeSentUTC:HH:mm:ss}] {msg.SenderName} ({msg.SenderUid}): {msg.Message}";

                        if (currentLength + line.Length + 1 <= maxLength)
                        {
                            lines.Insert(0, line);
                            currentLength += line.Length + 1;
                        }
                        else
                        {
                            lines.Insert(0, "... [Older Context Truncated]");
                            break;
                        }
                    }
                    formattedChat = string.Join("\n", lines);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to deserialize chat context for report {report.ReportID}", ex);
                formattedChat = "Error parsing chat history.";
            }
        }

        // Fetch Display Names
        using var db = await GetDbContext().ConfigureAwait(false);
        var reportedDisplay = report.ReportedUserUID;
        var reporterDisplay = report.ReporterUID;

        if (await db.Auth.AsNoTracking().SingleOrDefaultAsync(a => a.UserUID == report.ReportedUserUID).ConfigureAwait(false) is { } reportedAuth)
            if (await db.AccountClaimAuth.AsNoTracking().SingleOrDefaultAsync(a => a.User.UID == reportedAuth.PrimaryUserUID).ConfigureAwait(false) is { } reportedClaim)
                reportedDisplay = $"{report.ReportedUserUID} (<@{reportedClaim.DiscordId}>)";

        if (await db.Auth.AsNoTracking().SingleOrDefaultAsync(a => a.UserUID == report.ReporterUID).ConfigureAwait(false) is { } reporterAuth)
            if (await db.AccountClaimAuth.AsNoTracking().SingleOrDefaultAsync(a => a.User.UID == reporterAuth.PrimaryUserUID).ConfigureAwait(false) is { } reporterClaim)
                reporterDisplay = $"{report.ReporterUID} (<@{reporterClaim.DiscordId}>)";

        // Safely truncate fields to respect Discord's 1024 char limit per embed field
        var safeReason = string.IsNullOrWhiteSpace(report.ReportReason) ? "-" :
            report.ReportReason.Length > 1020 ? report.ReportReason.Substring(0, 1020) + "..." : report.ReportReason;
        var safeMessage = string.IsNullOrWhiteSpace(report.Message) ? "-" :
            report.Message.Length > 1020 ? report.Message.Substring(0, 1020) + "..." : report.Message;

        // Build the Embed
        var eb = new EmbedBuilder()
            .WithTitle($"Report Resolved: #{report.ReportID}")
            .WithColor(GetColorForAction(actionTaken))
            .WithDescription($"**Chat Context:**\n```ansi\n{formattedChat}\n```")
            // Metadata
            .AddField("ChatKind", report.Chat.ToString(), inline: true)
            .AddField("ChatLog ID", report.ChatLogId.ToString(), inline: true)
            .AddField("Message ID", report.MessageId.ToString(), inline: true)
            .AddField("Flagged Message", safeMessage)
            .AddField("Report Reason", safeReason)

            // Actor & Resolution Data
            .AddField("Reported User", reportedDisplay, inline: true)
            .AddField("Reporter User", reporterDisplay, inline: true)
            .AddField("Action Taken", $"{actionTaken} (ForceBan: {bannedContent})", inline: true)
            .AddField("Moderator", $"<@{Context.User.Id}>")
            .WithCurrentTimestamp();

        await logChannel.SendMessageAsync(embed: eb.Build()).ConfigureAwait(false);
    }
}