
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GagspeakServer.Services;

namespace GagspeakServer.Discord;

#pragma warning disable MA0004
public partial class KinkDispenserModule : InteractionModuleBase
{
    [SlashCommand("kinkypics", "Displays a gallery of pics based on your search terms")]
    public async Task KinkPicCommand(
        [Summary("searchTerms", "The search terms to use for finding pics. Multiple terms are split by spaces")] string searchTerms,
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

        // create a new instance of the picdataserice for this command
        var newService = new SelectionDataService();

        _logger.LogInformation("{method}:{userId}:{searchTerms}", nameof(KinkPicCommand), Context.User.Id, searchTerms);
        
        try {
            // get our URL defined
            var searchUrl = "https://www.sex.com/search/pics?query=" + Uri.EscapeDataString(searchTerms);
            if(sort != "") searchUrl += "&sort=" + sort;

            // Immediately respond with a deferred message
            await Context.Interaction.DeferAsync();
            newService.Img_HttpClient = new HttpClient();
            newService.Img_HttpClient.Timeout = TimeSpan.FromSeconds(10); // set the timeout to 10 seconds
            newService.Img_HttpClient.DefaultRequestHeaders.Referrer = new Uri(searchUrl);

            // set the search terms and the referer
            newService.UpdateResultsPageReferer(new Uri(searchUrl));
            newService.UpdateSearchTerms(searchTerms);

            _logger.LogDebug("Searching under URL {searchUrl}", searchUrl);
            // send the get request for the search results page to the sex.com server, and retrieve the responce.
            var response = await newService.Img_HttpClient.GetAsync(searchUrl);
            var htmlResponse = await response.Content.ReadAsStringAsync();

            // queue the initial message responce
            EmbedBuilder eb = new();
            eb.WithTitle("Results Found! Converting Pics to discord attachments...");
            eb.Color = Color.Magenta;
            ComponentBuilder cb = new();

            // send it and log it as the response message
            await Context.Interaction.FollowupAsync(embed: eb.Build(), components: cb.Build(), ephemeral: true).ConfigureAwait(false);
            var resp = await GetOriginalResponseAsync().ConfigureAwait(false);
            _botServices.ValidInteractions[Context.User.Id] = resp.Id;
            _logger.LogInformation("Init Msg: {id}", resp.Id);           
            // associate the service with the message ID
            _botServices.PicData[resp.Id] = newService;
            _logger.LogDebug("Dictionary now has {picDataService.Count} entries", _botServices.PicData.Count);
            
            // Parse the HTML response to get the GIF URLs, store results into the data newService
            await ParsePicUrls(htmlResponse, newService);

            _logger.LogInformation("Found {imageListCount} PICs!!!", newService.ResultImgs.ImageList.Count);

            // initialize counter
            newService.UpdateCurIdx(0);

            // we need to store the pic path and the pic url for transfering the pic to discord link
            var picPath = Path.Combine("Downloads-Pics", $"{resp.Id}-lowres-display.png");
            var url = newService.ResultImgs.ImageList[newService.ResultImgs.CurIdx].ThumbUrl;

            // convert it to the discord link
            var picUrl = await ConvertSexSiteImgURLtoDiscordURL(url, picPath, resp.Id);
            newService.ResultImgs.ImageList[newService.ResultImgs.CurIdx].ThumbUrl = picUrl;

            eb = new();
            cb = new();
            await CreateEmbeddedPicDisplay(eb, cb, resp.Id).ConfigureAwait(false);

            await ModifyMessageAsync(eb, cb, resp).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing kinky pics, or 0 results came up, try different tags.");
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


    [ComponentInteraction("back-five-pic")]
    public async Task ShiftBackFivePic()
    {
        // get the message ID
        ulong msgId;
        if(Context.Interaction is IComponentInteraction componentInteraction) { msgId = componentInteraction.Message.Id; }
        else { _logger.LogError("Error: Could not get the message ID from the interaction."); return; }
        // get the service associated
        if(_botServices.PicData.TryGetValue(msgId, out var service)) 
        {
            // shift the current index back 5
            service.ResultImgs.ShiftCurIdxBackwards(5);
            // shift the image display
            await ShiftImagePicDisplay(msgId).ConfigureAwait(false);
        }
    }

    [ComponentInteraction("back-one-pic")]
    public async Task ShiftBackOnePic()
    {
        // get the message ID
        ulong msgId;
        if(Context.Interaction is IComponentInteraction componentInteraction) { msgId = componentInteraction.Message.Id; }
        else { _logger.LogError("Error: Could not get the message ID from the interaction."); return; }
        // get the service associated
        if(_botServices.PicData.TryGetValue(msgId, out var service)) 
        {
            // shift the current index back 1
            service.ResultImgs.ShiftCurIdxBackwards(1);
            await ShiftImagePicDisplay(msgId).ConfigureAwait(false);
        }
    }

    [ComponentInteraction("forward-one-pic")]
    public async Task ShiftForwardOnePic()
    {
        // get the message ID
        ulong msgId;
        if(Context.Interaction is IComponentInteraction componentInteraction) { msgId = componentInteraction.Message.Id; }
        else { _logger.LogError("Error: Could not get the message ID from the interaction."); return; }
        // get the service associated
        if(_botServices.PicData.TryGetValue(msgId, out var service)) 
        {
            // shift the current index forward 1
            service.ResultImgs.ShiftCurIdxForwards(1);
            await ShiftImagePicDisplay(msgId).ConfigureAwait(false);
        }
    }

    [ComponentInteraction("forward-five-pic")]
    public async Task ShiftForwardFivePic()
    {
        // get the message ID
        ulong msgId;
        if(Context.Interaction is IComponentInteraction componentInteraction) { msgId = componentInteraction.Message.Id; }
        else { _logger.LogError("Error: Could not get the message ID from the interaction."); return; }
        // get the service associated
        if(_botServices.PicData.TryGetValue(msgId, out var service)) 
        {
            // shift the current index forward 5
            service.ResultImgs.ShiftCurIdxForwards(5);
            await ShiftImagePicDisplay(msgId).ConfigureAwait(false);
        }
    }

    public async Task ShiftImagePicDisplay(ulong msgId)
    {
        _logger.LogTrace("Shifting image display (start)");
        // get the service
        if(_botServices.PicData.TryGetValue(msgId, out var service))
        {
            _logger.LogTrace("Service found and is valid");

            // get the url and path for conversion
            var url = service.ResultImgs.ImageList[service.ResultImgs.CurIdx].ThumbUrl;
            var picPath = Path.Combine("Downloads-Pics", $"{msgId}-lowres-display.png");
            // get the link
            var picUrl = await ConvertSexSiteImgURLtoDiscordURL(url, picPath, msgId);
            service.ResultImgs.ImageList[service.ResultImgs.CurIdx].ThumbUrl = picUrl;

            EmbedBuilder eb = new();
            ComponentBuilder cb = new();
            await CreateEmbeddedGifDisplay(eb, cb, msgId).ConfigureAwait(false);
            await ModifyInteraction(eb, cb).ConfigureAwait(false);
        }
        // the service was not found, so inform the user
        else
        {
            await HandleSessionExpired().ConfigureAwait(false);
        }
    }

    [ComponentInteraction("print-pic")]
    public async Task FinishedDisplayWheelPic()
    {
        // get the message ID
        ulong msgId;
        if(Context.Interaction is IComponentInteraction componentInteraction) { msgId = componentInteraction.Message.Id; }
        else { _logger.LogError("Error: Could not get the message ID from the interaction."); return; }
        // get the service associated
        if(_botServices.PicData.TryGetValue(msgId, out var service)) 
        {
            // Defer the reply first
            await Context.Interaction.DeferAsync();
            // before we go to download the fullResURL we first need to obtain it
            await service.ResultImgs.ImageList[service.ResultImgs.CurIdx].FetchFullResURL(_logger, service).ConfigureAwait(false);
            // now that we have the full res URL lets start tossing in our path and url and updating this
            string url = service.ResultImgs.ImageList[service.ResultImgs.CurIdx].FullResURL;
            // get the path, set it to pic if it includes pic? in the url
            string picPath = Path.Combine("Downloads-Pics", $"{msgId}-fullres-display.png");

            _logger.LogTrace("Full res image sent to user");
            EmbedBuilder eb = new();
            eb.WithDescription("From Search: " + service.SearchTerms);
            eb.WithFooter("[Tags]: " + string.Join(", ", service.ResultImgs.ImageList[service.ResultImgs.CurIdx].Tags)
             + "\n[Sorted By]:" + service.SortFilter);
            eb.Color = Color.Magenta;
            ComponentBuilder cb = new();
            await Context.Interaction.FollowupAsync(embed: eb.Build(), components: cb.Build()).ConfigureAwait(false);
            await ConvertSexSiteImgURLtoDiscordFileUpload(url, picPath, msgId);
        }
        else
        {
            await HandleSessionExpired().ConfigureAwait(false);
        }
    }

    [ComponentInteraction("kill-pic")]
    public async Task KillPicQuery()
    {
        // get the message ID
        ulong msgId;
        if(Context.Interaction is IComponentInteraction componentInteraction) { msgId = componentInteraction.Message.Id; }
        else { _logger.LogError("Error: Could not get the message ID from the interaction."); return; }
        // get the service associated
        if(_botServices.PicData.TryGetValue(msgId, out var service)) 
        {
            EmbedBuilder eb = new();
            eb.WithTitle("Pic Search Querty Killed");
            ComponentBuilder cb = new();
            await ModifyInteraction(eb, cb).ConfigureAwait(false);
            await Task.Delay(3000);
            await Context.Interaction.DeleteOriginalResponseAsync();

            // dispose of the service as we are done with it
            _botServices.PicData.TryRemove(msgId, out _);
        }
        // the service was not found, so inform the user
        else
        {
            await HandleSessionExpired().ConfigureAwait(false);
            await Task.Delay(3000);
            await Context.Interaction.DeleteOriginalResponseAsync();

            // dispose of the service as we are done with it
            _botServices.PicData.TryRemove(msgId, out _);
        }
    }
}