
using System.Collections.Concurrent;
using Discord;
using System.Globalization;
using System.Text;
using Discord.Rest;
using GagspeakDiscord.Services;
using GagspeakShared.Data;
using GagspeakShared.Utils.Configuration;
using GagspeakShared.Services;
using Microsoft.EntityFrameworkCore;

// namespace dedicated to the discord bot.
namespace GagspeakDiscord;

// the class overviewing the bots services.
public class DiscordBotServices
{
    // for mapping the initial generated keys provided to the discord user who input it.
    public ConcurrentDictionary<ulong, (string, string)> DiscordInitialKeyMapping = new();
    // the same as above, but for relinking process.
    public ConcurrentDictionary<ulong, (string, string)> DiscordRelinkInitialKeyMapping = new();

    // a concurrent dictionary of the discord users who have verified their GagSpeak account.
    public ConcurrentDictionary<ulong, bool> DiscordVerifiedUsers { get; } = new();

    // the concurrent dictionary representing the last time a user has interacted with the bot.
    public ConcurrentDictionary<ulong, ulong> ValidInteractions { get; } = new();

    // the various vanity roles that the bot can assign to users.
    public Dictionary<RestRole, string> VanityRoles { get; set; } = new();

    // the concurrent dictionary representing the stored data for the gifs pics and boards relative to the player who initialized it.

    public ConcurrentDictionary<ulong, SelectionDataService> GifData = new(); // the gif data service
    public ConcurrentDictionary<ulong, SelectionDataService> PicData = new(); // the pic data service
    public ConcurrentDictionary<ulong, SelectionBoardService> BoardData = new(); // the board data service

    public ILogger<DiscordBotServices> Logger { get; init; }
    private readonly IConfigurationService<DiscordConfiguration> _config;
    private readonly IServiceProvider _services;
    public RestGuild? KinkporiumGuildCached; // The cached Guild for Ck Discord Guild
    public ConcurrentQueue<KeyValuePair<ulong, Func<DiscordBotServices, Task>>> VerificationQueue { get; } = new(); // the verification queue
    private CancellationTokenSource verificationTaskCts;                                 // the verification task cancellation tokens

    public DiscordBotServices(ILogger<DiscordBotServices> logger, IServiceProvider services,
        IConfigurationService<DiscordConfiguration> config)
    {
        Logger = logger;
        _config = config;
        _services = services;
    }

    /// <summary> Starts the verification process </summary>
    public Task Start()
    {
        _ = ProcessVerificationQueue();
        return Task.CompletedTask;
    }

    /// <summary> Stops the verification process </summary>
    public Task Stop()
    {
        verificationTaskCts?.Cancel();
        verificationTaskCts?.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds a verification task to the queue. (dont think this will have a purpose)
    /// </summary>
    private async Task ProcessVerificationQueue()
    {
        // create a new cts for the verification task
        verificationTaskCts = new CancellationTokenSource();
        // while the cancellation token is not requested
        while (!verificationTaskCts.IsCancellationRequested)
        {
            // log the debug message that we are processing the verification queue
            Logger.LogTrace("Processing Verification Queue, Entries: {entr}", VerificationQueue.Count);
            // if the queue has a peeked item
            if (VerificationQueue.TryPeek(out var queueitem))
            {
                // try and
                try
                {
                    // invoke the queue item and await the result
                    await queueitem.Value.Invoke(this).ConfigureAwait(false);
                    // log the information that the verification has been processed
                    Logger.LogInformation("Processed Verification for {key}", queueitem.Key);
                }
                catch (Exception e)
                {
                    // log the error that occured during the queue work
                    Logger.LogError(e, "Error during queue work");
                }
                finally
                {
                    // finally we should dequeue the item regardless of the outcome
                    VerificationQueue.TryDequeue(out _);
                }
            }

            // await a delay of 2 seconds
            await Task.Delay(TimeSpan.FromSeconds(2), verificationTaskCts.Token).ConfigureAwait(false);
        }
    }

    public async Task ProcessReports(IUser discordUser, CancellationToken token)
    {
        // if the guild is null, log the warning that the guild is null and return
        if (KinkporiumGuildCached is null)
        {
            Logger.LogWarning("No Guild Cached");
            return;
        }
        // if user id is null, log the warning that the user id is null and return
        Logger.LogInformation("Processing Reports Queue for Guild " + KinkporiumGuildCached.Name + 
            " from User: " + discordUser.GlobalName);

        // otherwise grab our channel report ID
        var reportChannelId = _config.GetValue<ulong?>(nameof(DiscordConfiguration.DiscordChannelForReports));
        if (reportChannelId is null)
        {
            Logger.LogWarning("No report channel configured");
            return;
        }

        var restChannel = await KinkporiumGuildCached.GetTextChannelAsync(reportChannelId.Value).ConfigureAwait(false);
        // Filter messages to only delete profile report messages sent by the bot
        var messages = await restChannel.GetMessagesAsync().FlattenAsync();
        var profileReportMessages = messages.Where(m => m.Author.Id == discordUser.Id);

        // Further filter messages to exclude those that contain an embed with a header labeled "Resolution"
        var messagesToDelete = profileReportMessages
            .Where(m => !m.Embeds.Any(e => e.Fields.Any(f => f.Name.Equals("Resolution", StringComparison.OrdinalIgnoreCase))))
            .Where(m => (DateTimeOffset.UtcNow - m.Timestamp).TotalDays <= 14); // Only include messages younger than two weeks


        // Delete messages
        if (messagesToDelete.Any())
        {
            await restChannel.DeleteMessagesAsync(messagesToDelete);
        }

        try
        {
            // within the scope of the service provider, execute actions using the GagSpeak DbContext
            using (var scope = _services.CreateScope())
            {
                Logger.LogInformation("Checking for Profile Reports");
                var dbContext = scope.ServiceProvider.GetRequiredService<GagspeakDbContext>();
                if (!dbContext.UserProfileReports.Any()) {
                    Logger.LogInformation("No Profile Reports Found");
                    return;
                }

                // collect the list of profile reports otherwise and get the report channel
                var reports = await dbContext.UserProfileReports.ToListAsync().ConfigureAwait(false);
                Logger.LogInformation("Found {count} Reports", reports.Count);

                // for each report, generate an embed and send it to the report channel
                foreach (var report in reports)
                {
                    Logger.LogDebug("Displaying Report for {reportedUserUID} by {reportingUserUID}", report.ReportedUserUID, report.ReportingUserUID);
                    // get the user who reported
                    var reportedUser = await dbContext.Users.SingleAsync(u => u.UID == report.ReportedUserUID).ConfigureAwait(false);
                    var reportedUserAccountClaim = await dbContext.AccountClaimAuth.SingleOrDefaultAsync(u => u.User.UID == report.ReportedUserUID).ConfigureAwait(false);

                    // get the user who was reported
                    var reportingUser = await dbContext.Users.SingleAsync(u => u.UID == report.ReportingUserUID).ConfigureAwait(false);
                    var reportingUserAccountClaim = await dbContext.AccountClaimAuth.SingleOrDefaultAsync(u => u.User.UID == report.ReportingUserUID).ConfigureAwait(false);

                    // get the profile data of the reported user.
                    var reportedUserProfile = await dbContext.UserProfileData.SingleAsync(u => u.UserUID == report.ReportedUserUID).ConfigureAwait(false);


                    // create an embed post to display reported profiles.
                    EmbedBuilder eb = new();
                    eb.WithTitle("GagSpeak Profile Report");

                    StringBuilder reportedUserSb = new();
                    StringBuilder reportingUserSb = new();
                    reportedUserSb.Append(reportedUser.UID);
                    reportingUserSb.Append(reportingUser.UID);
                    if (reportedUserAccountClaim != null)
                    {
                        reportedUserSb.AppendLine($" (<@{reportedUserAccountClaim.DiscordId}>)");
                    }
                    if (reportingUserAccountClaim != null)
                    {
                        reportingUserSb.AppendLine($" (<@{reportingUserAccountClaim.DiscordId}>)");
                    }
                    eb.AddField("Report Initiator", reportingUserSb.ToString());
                    var reportTimeUtc = new DateTimeOffset(report.ReportTime, TimeSpan.Zero);
                    var formattedTimestamp = string.Create(CultureInfo.InvariantCulture, $"<t:{reportTimeUtc.ToUnixTimeSeconds()}:F>");
                    eb.AddField("Report Time (Local)", formattedTimestamp);
                    eb.AddField("Report Reason", string.IsNullOrWhiteSpace(report.ReportReason) ? "-" : report.ReportReason);

                    // main report:
                    eb.AddField("Reported User", reportedUserSb.ToString());
                    eb.AddField("Reported User Profile Description", string.IsNullOrWhiteSpace(report.ReportedDescription) ? "-" : report.ReportedDescription);

                    var cb = new ComponentBuilder();
                    cb.WithButton("Dismiss Report", customId: $"gagspeak-report-button-dismissreport-{reportedUser.UID}", style: ButtonStyle.Primary);
                    cb.WithButton("Clear Profile", customId: $"gagspeak-report-button-clearprofileimage-{reportedUser.UID}", style: ButtonStyle.Secondary);
                    cb.WithButton("Revoke Social Features", customId: $"gagspeak-report-button-revokesocialfeatures-{reportedUser.UID}", style: ButtonStyle.Secondary);
                    cb.WithButton("Ban User", customId: $"gagspeak-report-button-banuser-{reportedUser.UID}", style: ButtonStyle.Danger);
                    cb.WithButton("Dismiss & Flag Reporting User", customId: $"gagspeak-report-button-flagreporter-{reportedUser.UID}-{reportingUser.UID}", style: ButtonStyle.Danger);

                    // Create a list for FileAttachments
                    var attachments = new List<FileAttachment>();


                    // List to keep track of streams to dispose later
                    var streamsToDispose = new List<MemoryStream>();

                    try
                    {
                        // Conditionally add the reported image
                        if (!string.IsNullOrEmpty(report.ReportedBase64Picture))
                        {
                            var reportedImageFileName = reportedUser.UID + "_profile_reported_" + Guid.NewGuid().ToString("N") + ".png";
                            var reportedImageStream = new MemoryStream(Convert.FromBase64String(report.ReportedBase64Picture));
                            streamsToDispose.Add(reportedImageStream);
                            var reportedImageAttachment = new FileAttachment(reportedImageStream, reportedImageFileName);
                            attachments.Add(reportedImageAttachment);
                            eb.WithImageUrl($"attachment://{reportedImageFileName}");
                        }

                        // Send files if there are any attachments
                        if (attachments.Count > 0)
                        {
                            await restChannel.SendFilesAsync(attachments, embed: eb.Build(), components: cb.Build()).ConfigureAwait(false);
                        }
                        else
                        {
                            // If no attachments, send the message with only the embed and components
                            await restChannel.SendMessageAsync(embed: eb.Build(), components: cb.Build()).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        // Dispose of all streams
                        foreach (var stream in streamsToDispose)
                            stream.Dispose();
                    }
                }

                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to process reports");
        }
    }

    internal void UpdateGuild(RestGuild guild)
    {
        Logger.LogInformation("Guild Updated to cache: "+ guild.Name);
        KinkporiumGuildCached = guild;
    }
}