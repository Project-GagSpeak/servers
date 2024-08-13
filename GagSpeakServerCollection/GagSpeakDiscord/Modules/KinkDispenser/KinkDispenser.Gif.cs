
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GagspeakDiscord.Services;
using GagspeakShared.Services;
using GagspeakShared.Utils.Configuration;
using StackExchange.Redis;

namespace GagspeakDiscord.Modules.KinkDispenser;

#pragma warning disable MA0004
public partial class KinkDispenser : InteractionModuleBase
{
    private readonly ILogger<KinkDispenser> _logger;                               // the logger for the GagspeakCommands
    private readonly IServiceProvider _services;                                    // our service provider
    public DiscordBotServices _botServices;
    private readonly IConfigurationService<DiscordConfiguration> _discordConfigService;    // the discord configuration service
    private readonly IConnectionMultiplexer _connectionMultiplexer;                 // the connection multiplexer for the discord bot

    public KinkDispenser(ILogger<KinkDispenser> logger, IServiceProvider services,
        IConfigurationService<DiscordConfiguration> gagspeakDiscordConfiguration, DiscordBotServices botServices,
        IConnectionMultiplexer connectionMultiplexer)
    {
        _logger = logger;
        _services = services;
        _botServices = botServices;
        _discordConfigService = gagspeakDiscordConfiguration;
        _connectionMultiplexer = connectionMultiplexer;
    }

    [SlashCommand("kinkygifs", "Displays a gallery of gifs based on your search terms")]
    public async Task KinkGifCommand(
        [Summary("searchTerms", "The search terms to use for finding gifs, separated by spaces")] string searchTerms,
        [Choice("By Relevance", "relevance"), Choice("By Most Popular", "popular-all"), Choice("By Most Recent", "latest"),
             Summary("sort", "Optional field to filter the results by relevance, popularity, or recent upload.")] string sort = "")
    {
        // make sure the user has access
        var user = Context.User as SocketGuildUser;

        // Check if the sort parameter is valid
        if (sort != "popular-all" && sort != "latest" && sort != "relevance" && sort != "")
        {
            await ReplyAsync("Invalid sort parameter. It can only be 'popular-all', 'relevance', or 'latest'.");
            return;
        }

        // Check if the user has a specific role
        if (!user.Roles.Any(r => r.Name == "Family/Social Role") && user.Id != Context.Guild.OwnerId)
        {
            await ReplyAsync("You don't have permission to use this command.");
            return;
        }
        // if they do have access::

        // create a new instance of the gifdataserice for this command
        var newService = new SelectionDataService(sort);

        _logger.LogInformation("{method}:{userId}:{searchTerms}", nameof(KinkGifCommand), Context.User.Id, searchTerms);

        try
        {
            // get the URL defined
            var searchUrl = "https://www.sex.com/search/gifs?query=" + Uri.EscapeDataString(searchTerms);
            if (sort != "") searchUrl += "&sort=" + sort;

            _logger.LogInformation("Search URL: {searchUrl}", searchUrl);

            // Immediately respond with a deferred message
            await Context.Interaction.DeferAsync();
            newService.Img_HttpClient = new HttpClient(); // generate a new httpclient for
            newService.Img_HttpClient.Timeout = TimeSpan.FromSeconds(10); // set the timeout to 10 seconds
            newService.Img_HttpClient.DefaultRequestHeaders.Referrer = new Uri(searchUrl);

            // set the search terms and the referer
            newService.UpdateResultsPageReferer(new Uri(searchUrl));
            newService.UpdateSearchTerms(searchTerms);

            // await the gif links
            var response = await newService.Img_HttpClient.GetAsync(searchUrl);
            var htmlResponse = await response.Content.ReadAsStringAsync();

            // queue the initial message responce
            EmbedBuilder eb = new();
            eb.WithTitle("Results Found! Converting GIFs to discord attachments...");
            eb.Color = Color.Magenta;
            ComponentBuilder cb = new();

            // send it and log it as the response message
            await Context.Interaction.FollowupAsync(embed: eb.Build(), components: cb.Build(), ephemeral: true).ConfigureAwait(false);
            var resp = await GetOriginalResponseAsync().ConfigureAwait(false);
            _botServices.ValidInteractions[Context.User.Id] = resp.Id;
            _logger.LogInformation("Init Msg: {id}", resp.Id);
            // associate the service with the message ID
            _botServices.GifData[resp.Id] = newService;
            _logger.LogDebug("Dictionary now has {gifDataService.Count} entries", _botServices.GifData.Count);

            // Parse the HTML response to get the GIF URLs, store results into the data service
            await ParseGifUrls(htmlResponse, newService);

            _logger.LogInformation("Found {ResultImgs.ImageListCount} GIFs!!!", newService.ResultImgs.ImageList.Count);

            // initialize counter
            newService.UpdateCurIdx(0);

            string basePath = "/root/GagSpeak-WebService/GagSpeakServerCollection/GagSpeakDiscord/DownloadsFolder/";
            string gifPath = Path.Combine(basePath, "Downloads-Gifs", $"{resp.Id}-lowres-display.gif");
            var url = newService.ResultImgs.ImageList[newService.ResultImgs.CurIdx].ThumbUrl;

            // get the link
            var gifUrlTask = ConvertSexSiteImgURLtoDiscordURL(url, gifPath, resp.Id);

            // fetch the fullresURL and the info on it
            var fetchFullResURLTask = newService.ResultImgs.ImageList[newService.ResultImgs.CurIdx]
                 .FetchFullResURL(_logger, newService);

            // Wait for both tasks to complete
            await Task.WhenAll(gifUrlTask, fetchFullResURLTask);

            // After both tasks have completed, you can get the results
            var gifUrl = gifUrlTask.Result;
            newService.ResultImgs.ImageList[newService.ResultImgs.CurIdx].ThumbUrl = gifUrl;

            eb = new();
            cb = new();
            await CreateEmbeddedGifDisplay(eb, cb, resp.Id).ConfigureAwait(false);

            await ModifyMessageAsync(eb, cb, resp).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing kinky gifs, or 0 results came up, try different tags.");
            EmbedBuilder eb = new();
            eb.WithTitle("**Your search tags came up with 0 results.** Try combining different terms or using less tags!");
            eb.WithColor(Color.Red);
            ComponentBuilder cb = new();
            var resp = await Context.Interaction.FollowupAsync(embed: eb.Build(), components: cb.Build(), ephemeral: true).ConfigureAwait(false);
            // wait 3 seconds then delete the message. 
            await Task.Delay(3000);
            await Context.Interaction.DeleteOriginalResponseAsync(); // remove original
            await resp.DeleteAsync(); // remove the followup
            return; // Stop the execution of the current method
        }
    }

    [ComponentInteraction("back-five")]
    public async Task ShiftBackFive()
    {
        // get the message ID
        ulong msgId;
        if (Context.Interaction is IComponentInteraction componentInteraction) { msgId = componentInteraction.Message.Id; }
        else { _logger.LogError("Error: Could not get the message ID from the interaction."); return; }
        // get the service associated
        if (_botServices.GifData.TryGetValue(msgId, out var service))
        {
            // shift the current index back 5
            service.ResultImgs.ShiftCurIdxBackwards(5);
            await ShiftImageDisplay(msgId).ConfigureAwait(false);
        }
    }

    [ComponentInteraction("back-one")]
    public async Task ShiftBackOne()
    {
        // get the message ID
        ulong msgId;
        if (Context.Interaction is IComponentInteraction componentInteraction) { msgId = componentInteraction.Message.Id; }
        else { _logger.LogError("Error: Could not get the message ID from the interaction."); return; }
        // get the service associated
        if (_botServices.GifData.TryGetValue(msgId, out var service))
        {
            // shift the current index back 1
            service.ResultImgs.ShiftCurIdxBackwards(1);
            await ShiftImageDisplay(msgId).ConfigureAwait(false);
        }
    }

    [ComponentInteraction("forward-one")]
    public async Task ShiftForwardOne()
    {
        // get the message ID
        ulong msgId;
        if (Context.Interaction is IComponentInteraction componentInteraction) { msgId = componentInteraction.Message.Id; }
        else { _logger.LogError("Error: Could not get the message ID from the interaction."); return; }
        // get the service associated
        if (_botServices.GifData.TryGetValue(msgId, out var service))
        {
            _logger.LogInformation("Service found and is valid");
            // shift the current index forward 1
            service.ResultImgs.ShiftCurIdxForwards(1);
            await ShiftImageDisplay(msgId).ConfigureAwait(false);
        }
    }

    [ComponentInteraction("forward-five")]
    public async Task ShiftForwardFive()
    {
        // get the message ID
        ulong msgId;
        if (Context.Interaction is IComponentInteraction componentInteraction) { msgId = componentInteraction.Message.Id; }
        else { _logger.LogError("Error: Could not get the message ID from the interaction."); return; }
        // get the service associated
        if (_botServices.GifData.TryGetValue(msgId, out var service))
        {
            // shift the current index forward 5
            service.ResultImgs.ShiftCurIdxForwards(5);
            await ShiftImageDisplay(msgId).ConfigureAwait(false);
        }
    }

    public async Task ShiftImageDisplay(ulong msgId)
    {
        _logger.LogTrace("Shifting image display (start)");
        // get the service
        if (_botServices.GifData.TryGetValue(msgId, out var service))
        {
            _logger.LogTrace("Service found and is valid");

            // get the url and path for conversion
            var url = service.ResultImgs.ImageList[service.ResultImgs.CurIdx].ThumbUrl;

            string basePath = "/root/GagSpeak-WebService/GagSpeakServerCollection/GagSpeakDiscord/DownloadsFolder/";
            string gifPath = Path.Combine(basePath, "Downloads-Gifs", $"{msgId}-lowres-display.gif");
            // get the link
            var gifUrlTask = ConvertSexSiteImgURLtoDiscordURL(url, gifPath, msgId);

            // fetch the fullresURL and the info on it
            var fetchFullResURLTask = service.ResultImgs.ImageList[service.ResultImgs.CurIdx]
                .FetchFullResURL(_logger, service);

            // Wait for both tasks to complete
            await Task.WhenAll(gifUrlTask, fetchFullResURLTask);

            // After both tasks have completed, you can get the results
            var gifUrl = gifUrlTask.Result;
            service.ResultImgs.ImageList[service.ResultImgs.CurIdx].ThumbUrl = gifUrl;

            EmbedBuilder eb = new();
            ComponentBuilder cb = new();
            await CreateEmbeddedGifDisplay(eb, cb, msgId).ConfigureAwait(false);
            await ModifyInteraction(eb, cb).ConfigureAwait(false);
        }
        else
        {
            await HandleSessionExpired().ConfigureAwait(false);
        }
    }

    [ComponentInteraction("print")]
    public async Task FinishDisplayWheel()
    {
        // get the message ID
        ulong msgId;
        if (Context.Interaction is IComponentInteraction componentInteraction) { msgId = componentInteraction.Message.Id; }
        else { _logger.LogError("Error: Could not get the message ID from the interaction."); return; }
        // get the service associated
        if (_botServices.GifData.TryGetValue(msgId, out var service))
        {
            // Defer the reply first
            await Context.Interaction.DeferAsync();
            // now that we have the full res URL lets start tossing in our path and url and updating this
            string url = service.ResultImgs.ImageList[service.ResultImgs.CurIdx].FullResURL;
            // get the path, set it to gif if it includes gif? in the url
            string basePath = "/root/GagSpeak-WebService/GagSpeakServerCollection/GagSpeakDiscord/DownloadsFolder/";
            string gifPath = Path.Combine(basePath, "Downloads-Gifs", $"{msgId}-fullres-display.gif");

            _logger.LogTrace("Full res image sent to user");
            EmbedBuilder eb = new();
            eb.WithDescription("From Search: " + service.SearchTerms);
            eb.WithFooter("[Tags]: " + string.Join(", ", service.ResultImgs.ImageList[service.ResultImgs.CurIdx].Tags)
             + "\n[Sorted By]:" + service.SortFilter);
            eb.Color = Color.Magenta;
            ComponentBuilder cb = new();
            await Context.Interaction.FollowupAsync(embed: eb.Build(), components: cb.Build()).ConfigureAwait(false);
            await ConvertSexSiteImgURLtoDiscordFileUpload(url, gifPath, msgId);
        }
        else
        {
            await HandleSessionExpired().ConfigureAwait(false);
        }
    }

    [ComponentInteraction("kill")]
    public async Task KillGifQuery()
    {
        // get the message ID
        ulong msgId;
        if (Context.Interaction is IComponentInteraction componentInteraction) { msgId = componentInteraction.Message.Id; }
        else { _logger.LogError("Error: Could not get the message ID from the interaction."); return; }
        // get the service associated
        if (_botServices.GifData.TryGetValue(msgId, out var service))
        {
            EmbedBuilder eb = new();
            eb.WithTitle("Gif Search Querty Killed");
            ComponentBuilder cb = new();
            await ModifyInteraction(eb, cb).ConfigureAwait(false);
            // delete the message in 3 seconds
            await Task.Delay(3000);
            await Context.Interaction.DeleteOriginalResponseAsync();

            // dispose of the service as we are done with it
            _botServices.GifData.TryRemove(msgId, out _);
        }
        // the service was not found, so inform the user
        else
        {
            await HandleSessionExpired().ConfigureAwait(false);
            await Task.Delay(3000);
            await Context.Interaction.DeleteOriginalResponseAsync();

            // dispose of the service as we are done with it
            _botServices.GifData.TryRemove(msgId, out _);
        }
    }
}