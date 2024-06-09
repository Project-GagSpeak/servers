using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GagspeakServer.Services;

namespace GagspeakServer.Discord;

#pragma warning disable MA0004
public partial class KinkDispenserModule : InteractionModuleBase
{

#region AlbumBoardResults

    [SlashCommand("kinkyboards", "Search for albums made by users on site for collections of boards & gifs related to the album title!")]
    public async Task KinkBoardCommand(
        [Summary("searchTerms", "Search terms used to find the album titles. Multiple terms are split by spaces")] string searchTerms)
    {
        // make sure the user has access
        var user = Context.User as SocketGuildUser;

        // Check if the user has a specific role
        if (!user.Roles.Any(r => r.Name == "Family/Social Role") && user.Id != Context.Guild.OwnerId)
        {
            await ReplyAsync("You don't have permission to use this command.");
            return;
        }
        // if they do have access::

        // create a new instance of the gifdataserice for this command
        var newService = new SelectionBoardService();

        _logger.LogInformation("{method}:{userId}:{searchTerms}", nameof(KinkBoardCommand), Context.User.Id, searchTerms);
        
        try {
            // get our URL defined
            var searchUrl = "https://www.sex.com/search/boards?query=" + Uri.EscapeDataString(searchTerms);

            // Immediately respond with a deferred message
            await Context.Interaction.DeferAsync();
            newService.Board_HttpClient = new HttpClient();
            newService.Board_HttpClient.Timeout = TimeSpan.FromSeconds(10); // set the timeout to 10 seconds
            newService.Board_HttpClient.DefaultRequestHeaders.Referrer = new Uri(searchUrl);

            // set the referer and the search terms
            newService.UpdateBoardUri(new Uri(searchUrl));
            newService.UpdateBoardSearchTerms(searchTerms);

            _logger.LogDebug("Searching under URL {searchUrl}", searchUrl);
            // send the get request for the search results page to the sex.com server, and retrieve the responce.
            var response = await newService.Board_HttpClient.GetAsync(searchUrl);
            var htmlResponse = await response.Content.ReadAsStringAsync();

            // queue the initial message responce
            EmbedBuilder eb = new();
            eb.WithTitle("Results Found! Localizing search results to GagSpeak...");
            eb.Color = Color.Magenta;
            ComponentBuilder cb = new();

            await Context.Interaction.FollowupAsync(embed: eb.Build(), components: cb.Build(), ephemeral: true).ConfigureAwait(false);
            var resp = await GetOriginalResponseAsync().ConfigureAwait(false);
            _botServices.ValidInteractions[Context.User.Id] = resp.Id;
            // associate the service with the message ID
            _botServices.BoardData[resp.Id] = newService;
            _logger.LogInformation("Dictionary now has {picDataService.Count} entries", _botServices.BoardData.Count);

            // Parse the HTML response to get the album titles and their respective thumbnails. The inner contents are not fetched until selected
            await ParseBoardResults(htmlResponse, resp.Id);

            _logger.LogInformation("Found {imageListCount} Boards for your search terms.", newService.Boards.Count);
    
            // initialize counter
            newService.UpdateBoardSearchIdx(0);
            
            // we are going to display the titles of up to the first 4 boards, bold the first, and include the four thumbnail images for them.
            // Download the pics of the four thumbnails for the current album
            await ConvertAlbumThumbnailsToDiscordURL(resp.Id);

            // now we can build the display
            eb = new();
            cb = new();
            await CreateEmbeddedBoardDisplay(eb, cb, resp.Id);

            await ModifyMessageAsync(eb, cb, resp).ConfigureAwait(false);

        } 
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex.InnerException);
            // log the trace of the error
            _logger.LogError(ex.StackTrace);
            await Context.Interaction.FollowupAsync("**Your search tags came up with 0 results.** Try combining different terms or using less tags!");
            // wait 3 seconds then delete the message. 
            await Task.Delay(3000);
            await Context.Interaction.DeleteOriginalResponseAsync(); // Send an empty message
            return; // Stop the execution of the current method
        }
    }

    // differentiate between board switching and album images switching
    [ComponentInteraction("back-four-board")]
    public async Task ShiftBackFourBoard()
    {
        // get the message ID
        ulong msgId;
        if(Context.Interaction is IComponentInteraction componentInteraction) { msgId = componentInteraction.Message.Id; }
        else { _logger.LogError("Error: Could not get the message ID from the interaction."); return; }
        // get the service associated
        if(_botServices.BoardData.TryGetValue(msgId, out var service)) 
        {
            service.ShiftSearchIdxBack(4);
            await ShiftImageBoardDisplay(msgId).ConfigureAwait(false);
        }
    }

    [ComponentInteraction("back-one-board")]
    public async Task ShiftBackOneBoard()
    {
        // get the message ID
        ulong msgId;
        if(Context.Interaction is IComponentInteraction componentInteraction) { msgId = componentInteraction.Message.Id; }
        else { _logger.LogError("Error: Could not get the message ID from the interaction."); return; }
        // get the service associated
        if(_botServices.BoardData.TryGetValue(msgId, out var service)) 
        {
            service.ShiftSearchIdxBack(1);
            await ShiftImageBoardDisplay(msgId).ConfigureAwait(false);
        }
    }

    [ComponentInteraction("forward-one-board")]
    public async Task ShiftForwardOneBoard()
    {
        // get the message ID
        ulong msgId;
        if(Context.Interaction is IComponentInteraction componentInteraction) { msgId = componentInteraction.Message.Id; }
        else { _logger.LogError("Error: Could not get the message ID from the interaction."); return; }
        // get the service associated
        if(_botServices.BoardData.TryGetValue(msgId, out var service)) 
        {
            service.ShiftSearchIdxForward(1);
            await ShiftImageBoardDisplay(msgId).ConfigureAwait(false);
        }
    }

    [ComponentInteraction("forward-four-board")]
    public async Task ShiftForwardFourBoard()
    {
        // get the message ID
        ulong msgId;
        if(Context.Interaction is IComponentInteraction componentInteraction) { msgId = componentInteraction.Message.Id; }
        else { _logger.LogError("Error: Could not get the message ID from the interaction."); return; }
        // get the service associated
        if(_botServices.BoardData.TryGetValue(msgId, out var service)) 
        {
            service.ShiftSearchIdxForward(4);
            await ShiftImageBoardDisplay(msgId).ConfigureAwait(false);
        }
    }

    public async Task ShiftImageBoardDisplay(ulong msgId)
    {
        _logger.LogInformation("Shifting to new album in search results");
        // get the service associated
        if(_botServices.BoardData.TryGetValue(msgId, out var service)) 
        {
            // queue the initial message responce
            EmbedBuilder eb = new();
            eb.WithTitle("Taking you back to the album selection...");
            eb.Color = Color.Magenta;
            ComponentBuilder cb = new();

            // fetch the new album thumbnails
            await ConvertAlbumThumbnailsToDiscordURL(msgId);

            // now we can build the display
            eb = new();
            cb = new();
            await CreateEmbeddedBoardDisplay(eb, cb, msgId);
            // modify the interaction display
            await ModifyInteraction(eb, cb).ConfigureAwait(false);
        }
        // the service was not found, so inform the user
        else
        {
            await HandleSessionExpired().ConfigureAwait(false);
            return;
        }
    }

    // cloning the above for independant viewing
    [ComponentInteraction("view-albums")]
    public async Task ShiftImageBoardDisplay()
    {
        // get the message ID
        ulong msgId;
        if(Context.Interaction is IComponentInteraction componentInteraction) { msgId = componentInteraction.Message.Id; }
        else { _logger.LogError("Error: Could not get the message ID from the interaction."); return; }
        if(_botServices.BoardData.TryGetValue(msgId, out var service)) 
        {
            // fetch the new album thumbnails
            await ConvertAlbumThumbnailsToDiscordURL(msgId);

            // now we can build the display
            EmbedBuilder eb = new();
            ComponentBuilder cb = new(); // create the components for the embed post

            // await the construction
            await CreateEmbeddedBoardDisplay(eb, cb, msgId);
            // modify the interaction display
            await ModifyInteraction(eb, cb).ConfigureAwait(false);
        }
        // the service was not found, so inform the user
        else
        {
            await HandleSessionExpired().ConfigureAwait(false);
            return;
        }
    }

#endregion AlbumBoardResults

#region AlbumBoardPage

    [ComponentInteraction("view-board")]
    public async Task ViewBoardPage()
    {
        ulong msgId;
        if(Context.Interaction is IComponentInteraction componentInteraction) { msgId = componentInteraction.Message.Id; }
        else { _logger.LogError("Error: Could not get the message ID from the interaction."); return; }
        // get the service associated
        if(_botServices.BoardData.TryGetValue(msgId, out var service)) 
        {
            // Defer the reply first
            await Context.Interaction.DeferAsync();
            // lets redirect our httpclient to the uri of the boardpage
            service.UpdateRequestHeaderReferer(service.CurBoard.ResultsPageReferer);
            // now that we have the new page, lets get that pages information
            var response = await service.Board_HttpClient.GetAsync(service.CurBoard.ResultsPageReferer);
            var htmlResponse = await response.Content.ReadAsStringAsync();

            // queue the initial message responce
            EmbedBuilder eb = new();
            eb.WithTitle("Downloading information for Album:");
            eb.WithDescription(service.CurBoard.Title);
            eb.Color = Color.Magenta;
            ComponentBuilder cb = new();
            cb.WithButton(" ", "kill-board", ButtonStyle.Danger, new Emoji("🔪"));
            await ModifyMessageAsync(eb, cb, componentInteraction.Message).ConfigureAwait(false);

            // Now, just like the pics and gifs, lets parse out the images, their 
            await ParseBoardPage(htmlResponse, msgId);
            _logger.LogInformation("Found {imageListCount} Media Content in this Album!", service.CurBoard.BoardImgs.ImageList.Count);

            service.CurBoard.UpdateBoardCurIdx(0);

            // get the url, path, and update it all
            var url = service.CurBoard.BoardImgs.ImageList[service.CurBoard.BoardImgs.CurIdx].ThumbUrl;
            string extension = url.Contains("gif?") ? "gif" : "png";
            string path = Path.Combine("Downloads-Boards", $"board-thumbnail.{extension}");

            // now lets convert the url to a discord url
            string newDiscordURL = await ConvertSexSiteURLtoDiscordURL(url, path, msgId);
            // extract information from the full res page such as the full res URL, and tags, and label
            await service.FetchFullResUrlAndTags();

            // create the embed and components
            eb = new();
            cb = new();
            await CreateBoardPageEmbedDisplay(eb, cb, msgId, newDiscordURL);
            await ModifyMessageAsync(eb, cb, componentInteraction.Message).ConfigureAwait(false);
        }
        else
        {
            await HandleSessionExpired().ConfigureAwait(false);
            return;
        }
    }

    // differentiate between board switching and album images switching
    [ComponentInteraction("back-five-boardpage-pic")]
    public async Task ShiftBackFiveBoardPagePic()
    {
        ulong msgId;
        if(Context.Interaction is IComponentInteraction componentInteraction) { msgId = componentInteraction.Message.Id; }
        else { _logger.LogError("Error: Could not get the message ID from the interaction."); return; }
        // get the service associated
        if(_botServices.BoardData.TryGetValue(msgId, out var service)) 
        {
            service.CurBoard.BoardImgs.ShiftCurIdxBackwards(5);
            await ShiftImageBoardPicDisplay(msgId, componentInteraction.Message).ConfigureAwait(false);
        }
    }

    [ComponentInteraction("back-one-boardpage-pic")]
    public async Task ShiftBackOneBoardPagePic()
    {
        ulong msgId;
        if(Context.Interaction is IComponentInteraction componentInteraction) { msgId = componentInteraction.Message.Id; }
        else { _logger.LogError("Error: Could not get the message ID from the interaction."); return; }
        // get the service associated
        if(_botServices.BoardData.TryGetValue(msgId, out var service)) 
        {
            service.CurBoard.BoardImgs.ShiftCurIdxBackwards(1);
            await ShiftImageBoardPicDisplay(msgId, componentInteraction.Message).ConfigureAwait(false);
        }
    }

    [ComponentInteraction("forward-one-boardpage-pic")]
    public async Task ShiftForwardOneBoardPagePic()
    {
        ulong msgId;
        if(Context.Interaction is IComponentInteraction componentInteraction) { msgId = componentInteraction.Message.Id; }
        else { _logger.LogError("Error: Could not get the message ID from the interaction."); return; }
        // get the service associated
        if(_botServices.BoardData.TryGetValue(msgId, out var service)) 
        {
            service.CurBoard.BoardImgs.ShiftCurIdxForwards(1);
            await ShiftImageBoardPicDisplay(msgId, componentInteraction.Message).ConfigureAwait(false);
        }
    }

    [ComponentInteraction("forward-five-boardpage-pic")]
    public async Task ShiftForwardFiveBoardPagePic()
    {
        ulong msgId;
        if(Context.Interaction is IComponentInteraction componentInteraction) { msgId = componentInteraction.Message.Id; }
        else { _logger.LogError("Error: Could not get the message ID from the interaction."); return; }
        // get the service associated
        if(_botServices.BoardData.TryGetValue(msgId, out var service)) 
        {
            service.CurBoard.BoardImgs.ShiftCurIdxForwards(5);
            await ShiftImageBoardPicDisplay(msgId, componentInteraction.Message).ConfigureAwait(false);
        }
    }

    public async Task ShiftImageBoardPicDisplay(ulong msgId, IUserMessage message)
    {
        // get the service associated
        if(_botServices.BoardData.TryGetValue(msgId, out var service)) 
        {
            _logger.LogInformation("Shifting to new image in album {albumTitle}", service.CurBoard.Title);

            // acknowledge the interaction
            await Context.Interaction.DeferAsync();
            // get the url, path, and update it all
            var url = service.CurBoard.BoardImgs.ImageList[service.CurBoard.BoardImgs.CurIdx].ThumbUrl;
            string extension = url.Contains("gif?") ? "gif" : "png";
            string path = Path.Combine("Downloads-Boards", $"board-thumbnail.{extension}");

            // now lets convert the url to a discord url
            string newDiscordURL = await ConvertSexSiteURLtoDiscordURL(url, path, msgId);
            // extract information from the full res page such as the full res URL, and tags, and label
            await service.FetchFullResUrlAndTags();

            // create the embed and components
            EmbedBuilder eb = new();
            ComponentBuilder cb = new();
            await CreateBoardPageEmbedDisplay(eb, cb, msgId, newDiscordURL);
            await ModifyMessageAsync(eb, cb, message).ConfigureAwait(false);
        }
        // the service was not found, so inform the user
        else
        {
            await HandleSessionExpired().ConfigureAwait(false);
        }
    }


    [ComponentInteraction("print-album-pic")]
    public async Task FinishDisplayWheelBoard()
    {
        // get the message ID
        ulong msgId;
        if(Context.Interaction is IComponentInteraction componentInteraction) { msgId = componentInteraction.Message.Id; }
        else { _logger.LogError("Error: Could not get the message ID from the interaction."); return; }
        // get the service associated
        if(_botServices.BoardData.TryGetValue(msgId, out var service)) 
        {
            // Defer the reply first
            await Context.Interaction.DeferAsync();
            // before we go to download the fullResURL we first need to obtain it
            await service.CurBoard.BoardImgs.ImageList[service.CurBoard.BoardImgs.CurIdx].FetchFullResURL(_logger, service).ConfigureAwait(false);
            // now that we have the full res URL lets start tossing in our path and url and updating this
            string url = service.CurBoard.BoardImgs.ImageList[service.CurBoard.BoardImgs.CurIdx].FullResURL;
            // get the path, set it to pic if it includes pic? in the url
            string extension = url.Contains("gif?") ? "gif" : "png";
            string picPath = Path.Combine("Downloads-Boards", $"{msgId}-boardpage-highres.{extension}");

            

            _logger.LogTrace("Full res image sent to user");
            EmbedBuilder eb = new();
            eb.WithDescription("From Search: " + service.SearchTerms);
            eb.WithFooter("[Tags]: " + string.Join(", ", service.CurBoard.BoardImgs.ImageList[service.CurBoard.BoardImgs.CurIdx].Tags));
            eb.Color = Color.Magenta;
            ComponentBuilder cb = new();
            await Context.Interaction.FollowupAsync(embed: eb.Build(), components: cb.Build()).ConfigureAwait(false);
            await ConvertSexSiteBoardImgURLtoDiscordFileUpload(url, picPath, msgId).ConfigureAwait(false);
        }
        else
        {
            await HandleSessionExpired().ConfigureAwait(false);
        }
    }

    [ComponentInteraction("kill-board")]
    public async Task KillBoardQuery()
    {
        // get the message ID
        ulong msgId;
        if(Context.Interaction is IComponentInteraction componentInteraction) { msgId = componentInteraction.Message.Id; }
        else { _logger.LogError("Error: Could not get the message ID from the interaction."); return; }
        // get the service associated
        if(_botServices.BoardData.TryGetValue(msgId, out var service)) 
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
#endregion AlbumBoardPage