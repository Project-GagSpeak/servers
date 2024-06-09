
using System.Collections.Concurrent;
using Discord.Rest;
using GagspeakServer.Services;

// namespace dedicated to the discord bot.
namespace GagspeakServer.Discord;

// the class overviewing the bots services.
public class DiscordBotServices
{
    public readonly string[] LodestoneServers = new[] { "eu", "na", "jp", "fr", "de" }; // the different lodestone servers
    
    public ConcurrentDictionary<ulong, string> DiscordLodestoneMapping = new();         // the discord lodestone mapping paths
    public ConcurrentDictionary<ulong, string> DiscordRelinkLodestoneMapping = new();   // The discord relinking lodestone mapping
    public ConcurrentDictionary<ulong, bool> DiscordVerifiedUsers { get; } = new();     // The discord verified users
    public ConcurrentDictionary<ulong, DateTime> LastVanityChange = new();              // the last vanity changes
    public ConcurrentDictionary<string, DateTime> LastVanityGidChange = new();          // the latest vanity gid changes
    public ConcurrentDictionary<ulong, ulong> ValidInteractions { get; } = new();       // The valid interactions
    public Dictionary<RestRole, string> VanityRoles { get; set; } = new();              // The different vanity roles

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


    /// <summary>
    /// Starts the verification process
    /// </summary>
    public Task Start()
    {
        _ = ProcessVerificationQueue();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the verification process
    /// </summary>
    public Task Stop()
    {
        verificationTaskCts?.Cancel();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds a verification task to the queue.
    /// </summary>
    private async Task ProcessVerificationQueue()
    {
        // create a new cts for the verification task
        verificationTaskCts = new CancellationTokenSource();
        // while the cancellation token is not requested
        while (!verificationTaskCts.IsCancellationRequested)
        {
            // log the debug message that we are processing the verification queue
            Logger.LogDebug("Processing Verification Queue, Entries: {entr}", VerificationQueue.Count);
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