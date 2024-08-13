
using System.Collections.Concurrent;
using Discord.Rest;
using GagspeakDiscord.Services;

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

    // the concurrent dictionary representing when each discord user haad its las vanity change
    public ConcurrentDictionary<ulong, DateTime> LastVanityChange = new();

    // the concurrent dictionary representing the last time a user has interacted with the bot.
    public ConcurrentDictionary<ulong, ulong> ValidInteractions { get; } = new();

    // the various vanity roles that the bot can assign to users.
    public Dictionary<RestRole, string> VanityRoles { get; set; } = new();

    // the concurrent dictionary representing the stored data for the gifs pics and boards relative to the player who initialized it.

    public ConcurrentDictionary<ulong, SelectionDataService> GifData = new(); // the gif data service
    public ConcurrentDictionary<ulong, SelectionDataService> PicData = new(); // the pic data service
    public ConcurrentDictionary<ulong, SelectionBoardService> BoardData = new(); // the board data service

    private readonly IServiceProvider _serviceProvider;                                 // bot's service provider
    public ILogger<DiscordBotServices> Logger { get; init; }                            // logger for the bot
    public ConcurrentQueue<KeyValuePair<ulong, Func<DiscordBotServices, Task>>> VerificationQueue { get; } = new(); // the verification queue
    private CancellationTokenSource verificationTaskCts;                                 // the verification task cancellation tokens


    public DiscordBotServices(ILogger<DiscordBotServices> logger)
    {
        Logger = logger;
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
}