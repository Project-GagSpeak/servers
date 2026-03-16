
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using GagspeakDiscord.Services;
using GagspeakShared.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace GagspeakDiscord;
#nullable enable
#pragma warning disable IDISP006

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
    private readonly IServiceProvider _services;

    // The cached Guild for Ck Discord Guild
    public RestGuild? KinkporiumGuildCached;
    public ConcurrentQueue<KeyValuePair<ulong, Func<DiscordBotServices, Task>>> VerificationQueue { get; } = new(); // the verification queue
    private CancellationTokenSource _verificationTaskCts = new();

    public DiscordBotServices(ILogger<DiscordBotServices> logger, IServiceProvider services)
    {
        Logger = logger;
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
        _verificationTaskCts?.Cancel();
        _verificationTaskCts?.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Adds a verification task to the queue. (dont think this will have a purpose)
    /// </summary>
    private async Task ProcessVerificationQueue()
    {
        // create a new cts for the verification task
        _verificationTaskCts?.Cancel();
        _verificationTaskCts?.Dispose();
        _verificationTaskCts = new CancellationTokenSource();
        // while the cancellation token is not requested
        while (!_verificationTaskCts.IsCancellationRequested)
        {
            Logger.LogTrace($"Processing Verification Queue, Entries: {VerificationQueue.Count}");
            // if the queue has a peeked item
            if (VerificationQueue.TryPeek(out var queueitem))
            {
                try
                {
                    await queueitem.Value.Invoke(this).ConfigureAwait(false);
                    Logger.LogInformation($"Processed Verification for {queueitem.Key}");
                }
                catch (Exception e)
                {
                    Logger.LogError($"Error during queue work: {e}");
                }
                finally
                {
                    VerificationQueue.TryDequeue(out _);
                }
            }
            // await a delay of 2 seconds
            await Task.Delay(TimeSpan.FromSeconds(2), _verificationTaskCts.Token).ConfigureAwait(false);
        }
    }

    public async Task CreateOrUpdateReportWizardWithExistingMessage(SocketTextChannel channel, IUserMessage message)
    {
        using var scope = _services.CreateAsyncScope();
        using var db = scope.ServiceProvider.GetRequiredService<GagspeakDbContext>();

        var totalProfileReports = await db.ReportedProfiles.CountAsync().ConfigureAwait(false);
        var totalChatReports = await db.ReportedChats.CountAsync().ConfigureAwait(false);

        var eb = new EmbedBuilder()
            .WithTitle("GagSpeak Report Wizard")
            .WithDescription("View and decide an outcome for reported chat and profiles. Select an option below:")
            .WithThumbnailUrl("https://raw.githubusercontent.com/CordeliaMist/GagSpeak-Client/main/images/iconUI.png")
            .AddField("Current Profile Reports", totalProfileReports, true)
            .AddField("Current Chat Reports", totalChatReports, true)
            .WithColor(Color.Orange);

        var cb = new ComponentBuilder()
            .WithButton("Profile Reports", "reports-profile-home:true", ButtonStyle.Primary, Emoji.Parse("🖼️"))
            .WithButton("Chat Reports", "reports-chat-home:true", ButtonStyle.Primary, Emoji.Parse("💬"))
            .WithButton("🔄 Refresh", "reports-refresh", ButtonStyle.Secondary);

        await message.ModifyAsync(m =>
        {
            m.Embed = eb.Build();
            m.Components = cb.Build();
        }).ConfigureAwait(false);
    }

    internal void UpdateGuild(RestGuild guild)
    {
        Logger.LogInformation("Guild Updated to cache: "+ guild.Name);
        KinkporiumGuildCached = guild;
    }
}

#nullable disable
#pragma warning restore IDISP006