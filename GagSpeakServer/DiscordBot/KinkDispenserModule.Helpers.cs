
using System.Net;
using System.Text;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GagspeakServer.Services;
using HtmlAgilityPack;

namespace GagspeakServer.Discord;

#pragma warning disable MA0004
#pragma warning disable CS8601
public partial class KinkDispenserModule : InteractionModuleBase
{
#region Debug Command
    [SlashCommand("debug", "Prints some debug info about the bot")]
    public async Task DebugCommand()
    {
        await Context.Interaction.DeferAsync();
        // print all current instances of every active gifdata, picdata, and board data service
        StringBuilder sb = new();
        sb.AppendLine("GIF Data Services:");
        foreach (var gifData in _botServices.GifData)
        {
            sb.AppendLine($"User: {gifData.Key}, Message: {gifData.Value}");
        }
        sb.AppendLine("PIC Data Services:");
        foreach (var picData in _botServices.PicData)
        {
            sb.AppendLine($"User: {picData.Key}, Message: {picData.Value}");
        }
        sb.AppendLine("Board Data Services:");
        foreach (var boardData in _botServices.BoardData)
        {
            sb.AppendLine($"User: {boardData.Key}, Message: {boardData.Value}");
        }
        EmbedBuilder eb = new();
        eb.WithTitle("Debug Info");
        eb.WithDescription(sb.ToString());
        eb.Color = Color.Magenta;

        await FollowupAsync(embed: eb.Build());
    }

    [SlashCommand("purge", "Purges the last 5 messages in the channel")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task PurgeCommand()
    {
        await Context.Interaction.DeferAsync();
        var messages = await Context.Channel.GetMessagesAsync(5).FlattenAsync();
        foreach (var message in messages)
        {
            await message.DeleteAsync();
        }
        await FollowupAsync("Purged the last 5 messages in the channel.");
    }
#endregion Debug Command
#region EmbedHelpers
    public async Task ModifyMessageAsync(EmbedBuilder eb, ComponentBuilder cb, IUserMessage message, string text = "")
    {
        await message.ModifyAsync(msg =>
        {
            msg.Content = text;
            msg.Embed = eb.Build();
            msg.Components = cb.Build();
        });
    }

    /// <summary> Validate the interaction being made with the discord bot </summary>
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
        await HandleSessionExpired().ConfigureAwait(false);
        return false;
    }

    // main interaction modifier
    private async Task ModifyInteraction(EmbedBuilder eb, ComponentBuilder cb)
    {
        await ((Context.Interaction) as IComponentInteraction).UpdateAsync(m =>
        {
            m.Embed = eb.Build();
            m.Components = cb.Build();
        }).ConfigureAwait(false);
    }

    private async Task CreateEmbeddedGifDisplay(EmbedBuilder eb, ComponentBuilder cb, ulong msgId)
    {
        if(_botServices.GifData.TryGetValue(msgId, out var service))
        {
            eb.WithTitle("[Result " + (service.ResultImgs.CurIdx+1) + "/" + service.ResultImgs.ImageList.Count +
                "] Search: " + service.SearchTerms);
            eb.WithDescription("[Image Caption]: " + service.ResultImgs.ImageList[service.ResultImgs.CurIdx].Label);
            eb.WithImageUrl(service.ResultImgs.ImageList[service.ResultImgs.CurIdx].ThumbUrl);
            eb.WithFooter("[Full Image Resolution]: " +
               service.ResultImgs.ImageList[service.ResultImgs.CurIdx].FullResImgSize.Width + "x" +
               service.ResultImgs.ImageList[service.ResultImgs.CurIdx].FullResImgSize.Height + "\n" +
               "[Tags]: " + string.Join(", ", service.ResultImgs.ImageList[service.ResultImgs.CurIdx].Tags));
            eb.Color = Color.Magenta;
            ActionRowBuilder row1 = new();
            ActionRowBuilder row2 = new();
            row1.WithButton(" ", "back-five", ButtonStyle.Secondary, new Emoji("⏪"));
            row1.WithButton(" ", "back-one", ButtonStyle.Secondary, new Emoji("◀️"));
            row1.WithButton(" ", "forward-one", ButtonStyle.Secondary, new Emoji("▶️"));
            row1.WithButton(" ", "forward-five", ButtonStyle.Secondary, new Emoji("⏩"));
            row2.WithButton("Full-Res", "print", ButtonStyle.Primary, new Emoji("📤"));
            row2.WithButton(" ", "kill", ButtonStyle.Danger, new Emoji("🔪"));
            cb.AddRow(row1);
            cb.AddRow(row2);
        } 
        else
        {
            _logger.LogError("Error: Could not find the service associated with the message ID.");
            await HandleSessionExpired().ConfigureAwait(false);
            return;
        }
    }

    private async Task CreateEmbeddedPicDisplay(EmbedBuilder eb, ComponentBuilder cb, ulong msgId)
    {
        if(_botServices.PicData.TryGetValue(msgId, out var service))
        {
            eb.WithTitle("[Result " + (service.ResultImgs.CurIdx+1) + "/" + service.ResultImgs.ImageList.Count +
                "] Search: " + service.SearchTerms);
            eb.WithDescription("[Image Caption]: " + service.ResultImgs.ImageList[service.ResultImgs.CurIdx].Label);
            eb.WithImageUrl(service.ResultImgs.ImageList[service.ResultImgs.CurIdx].ThumbUrl);
            eb.WithFooter("[Full Image Resolution]: " +
                 service.ResultImgs.ImageList[service.ResultImgs.CurIdx].FullResImgSize.Width + "x" +
                 service.ResultImgs.ImageList[service.ResultImgs.CurIdx].FullResImgSize.Height + "\n" +
                 "[Tags]: " + string.Join(", ", service.ResultImgs.ImageList[service.ResultImgs.CurIdx].Tags));
            eb.Color = Color.Magenta;
            ActionRowBuilder row1 = new();
            ActionRowBuilder row2 = new();
            row1.WithButton(" ", "back-five-pic", ButtonStyle.Secondary, new Emoji("⏪"));
            row1.WithButton(" ", "back-one-pic", ButtonStyle.Secondary, new Emoji("◀️"));
            row1.WithButton(" ", "forward-one-pic", ButtonStyle.Secondary, new Emoji("▶️"));
            row1.WithButton(" ", "forward-five-pic", ButtonStyle.Secondary, new Emoji("⏩"));
            row2.WithButton("Full-Res", "print-pic", ButtonStyle.Primary, new Emoji("📤"));
            row2.WithButton(" ", "kill-pic", ButtonStyle.Danger, new Emoji("🔪"));
            cb.AddRow(row1);
            cb.AddRow(row2);
        } 
        else
        {
            await HandleSessionExpired().ConfigureAwait(false);
            return;
        }
    }


    private async Task CreateEmbeddedBoardDisplay(EmbedBuilder eb, ComponentBuilder cb, ulong msgId)
    {
        // get the service associated
        if(_botServices.BoardData.TryGetValue(msgId, out var service)) 
        {
            eb.WithTitle("Albums for: " + service.SearchTerms + "  ["+(service.BoardSearchIdx+1)+"-"+(service.BoardSearchIdx+4)+"/"+service.Boards.Count+"]");
            eb.WithDescription(
                $"**({service.BoardSearchIdx+1})** **" + service.CurBoard.Title + "**\n" +
                $"({service.BoardSearchIdx+2}) " + service.Boards[(service.BoardSearchIdx + 1) % service.Boards.Count].Title + "\n" +
                $"({service.BoardSearchIdx+3}) " + service.Boards[(service.BoardSearchIdx + 2) % service.Boards.Count].Title + "\n" +
                $"({service.BoardSearchIdx+4}) " + service.Boards[(service.BoardSearchIdx + 3) % service.Boards.Count].Title + "\n");
            //eb.WithFooter("Displaying Thumbnails for the bolded album title above.");
            // if there is only one thumbnail, display it as the image url and display no thumbnail
            if(service.CurBoard.Thumbnails.Count == 1)
            {
                eb.WithImageUrl(service.CurBoard.Thumbnails[0].ThumbUrl);
            }
            else
            {
                eb.WithThumbnailUrl(service.CurBoard.Thumbnails[0].ThumbUrl); // first preview pic
                eb.WithImageUrl(service.CurBoard.Thumbnails[1].ThumbUrl); // second preview pic
            }

            // create the components for the embed
            ActionRowBuilder row1 = new();
            ActionRowBuilder row2 = new();
            row1.WithButton(" ", "back-four-board", ButtonStyle.Secondary, new Emoji("⏪"));
            row1.WithButton(" ", "back-one-board", ButtonStyle.Secondary, new Emoji("◀️"));
            row1.WithButton(" ", "forward-one-board", ButtonStyle.Secondary, new Emoji("▶️"));
            row1.WithButton(" ", "forward-four-board", ButtonStyle.Secondary, new Emoji("⏩"));
            row2.WithButton("View this BoardPage", "view-board", ButtonStyle.Primary, new Emoji("🔍"));
            row2.WithButton(" ", "kill-board", ButtonStyle.Danger, new Emoji("🔪"));
            cb.AddRow(row1);
            cb.AddRow(row2);
        }
        else
        {
            await HandleSessionExpired().ConfigureAwait(false);
        }
    }

    private async Task CreateBoardPageEmbedDisplay(EmbedBuilder eb, ComponentBuilder cb, ulong msgId, string url)
    {
        // get the service associated
        if(_botServices.BoardData.TryGetValue(msgId, out var service)) 
        {
            eb.WithTitle("[Album]: " + service.CurBoard.Title);
            eb.WithDescription("[Image Label]: " + service.CurBoard.BoardImgs.ImageList[service.CurBoard.BoardImgs.CurIdx].Label);
            eb.WithImageUrl(url);
            eb.WithFooter("[Tags]: " + string.Join(", ", service.CurBoard.BoardImgs.ImageList[service.CurBoard.BoardImgs.CurIdx].Tags) +
            "\n[Viewing Image " + (service.CurBoard.BoardImgs.CurIdx + 1) + "/" + service.CurBoard.BoardImgs.ImageList.Count + "]");
            eb.Color = Color.Magenta;
            ActionRowBuilder row1 = new();
            ActionRowBuilder row2 = new();
            row1.WithButton(" ", "back-five-boardpage-pic", ButtonStyle.Secondary, new Emoji("⏪"));
            row1.WithButton(" ", "back-one-boardpage-pic", ButtonStyle.Secondary, new Emoji("◀️"));
            row1.WithButton(" ", "forward-one-boardpage-pic", ButtonStyle.Secondary, new Emoji("▶️"));
            row1.WithButton(" ", "forward-five-boardpage-pic", ButtonStyle.Secondary, new Emoji("⏩"));
            row2.WithButton("Back To Albums", "view-albums", ButtonStyle.Primary, new Emoji("🔙"));
            row2.WithButton("Print Full-Res", "print-album-pic", ButtonStyle.Primary, new Emoji("📤"));
            row2.WithButton(" ", "kill-board", ButtonStyle.Danger, new Emoji("🔪"));
            cb.AddRow(row1);
            cb.AddRow(row2);
        }
        else
        {
            await HandleSessionExpired().ConfigureAwait(false);
        }
    }

#endregion EmbedHelpers

#region ConverterHelpers

    private async Task<string> ConvertSexSiteURLtoDiscordURL(string url, string path, ulong msgId)
    {
        // get the service associated
        if(_botServices.BoardData.TryGetValue(msgId, out var service)) 
        {
            _logger.LogInformation("[Attempting to access URL]: {url}", url); 
            var boardData = await service.Board_HttpClient.GetByteArrayAsync(url);
            // Save the PIC to the local server
            await File.WriteAllBytesAsync(path, boardData);
            // Upload the PIC to Discord on a dummy channel
            var dummyChannel = Context.Client as DiscordSocketClient;
            ulong targetChannelId = 1248399463664582667;
            var targetChannel = dummyChannel.GetChannel(targetChannelId) as SocketTextChannel;
            var message = await targetChannel.SendFileAsync(path);
            // extract the URL
            var boardUrl = message.Attachments.First().Url;
            // delete the PIC from the local server
            File.Delete(path);
            _logger.LogDebug("URL sucessfully extracted");
            // return the URL
            return boardUrl;
        }
        else
        {
            await HandleSessionExpired().ConfigureAwait(false);
            return "";
        }
    }

    private async Task<string> ConvertSexSiteImgURLtoDiscordURL(string url, string path, ulong msgId)
    {
        SelectionDataService dataServiceFound;
        if(_botServices.GifData.TryGetValue(msgId, out var service)) {
            dataServiceFound = service;
        } else if (_botServices.PicData.TryGetValue(msgId, out var picService)) {
            dataServiceFound = picService;
        } else {
            _logger.LogError("Error: No active instance of this service exists.");
            return "";
        }
        // now we use the info to get our data
        _logger.LogInformation("[Attempting to access URL]: {url}", url); 
        var imgData = await dataServiceFound.Img_HttpClient.GetByteArrayAsync(url);
        // Save the PIC to the local server
        await File.WriteAllBytesAsync(path, imgData);
        // Upload the PIC to Discord on a dummy channel
        var dummyChannel = Context.Client as DiscordSocketClient;
        ulong targetChannelId = 1248399463664582667;
        var targetChannel = dummyChannel.GetChannel(targetChannelId) as SocketTextChannel;
        var message = await targetChannel.SendFileAsync(path);
        // extract the URL
        var boardUrl = message.Attachments.First().Url;
        // delete the PIC from the local server
        File.Delete(path);
        // return the URL
        return boardUrl;
    }

    public async Task ConvertAlbumThumbnailsToDiscordURL(ulong msgId)
    {
        if(_botServices.BoardData.TryGetValue(msgId, out var service)) 
        {
            var thumbnails = service.CurBoard.Thumbnails;
            int count = Math.Min(thumbnails.Count, 2); // Ensure we only loop a maximum of 2 times or less if there are fewer thumbnails

            for (int i = 0; i < count; i++)
            {
                // get the url
                var url = thumbnails[i].ThumbUrl;

                // get the path, set it to gif if it includes gif? in the url
                string extension = url.Contains("gif?") ? "gif" : "png";
                string path = Path.Combine("Downloads-Boards", $"board-thumbnail{i}.{extension}");

                // now lets convert the url to a discord url
                string newDiscordURL = await ConvertSexSiteURLtoDiscordURL(url, path, msgId);

                // update the URL of the thumbnail to be the discord URL
                thumbnails[i].ThumbUrl = newDiscordURL;
            }
        }
        else
        {
            await HandleSessionExpired().ConfigureAwait(false);
        }
    }

    private async Task ConvertSexSiteBoardImgURLtoDiscordFileUpload(string url, string path, ulong msgId)
    {
        if(_botServices.BoardData.TryGetValue(msgId, out var service))
        {
            // now we use the info to get our data
            _logger.LogInformation("[Attempting to access URL]: {url}", url); 
            var imgData = await service.Board_HttpClient.GetByteArrayAsync(url);
            // Save the PIC to the local server
            await File.WriteAllBytesAsync(path, imgData);
            // Upload the PIC directly under our final output
            var message = await Context.Channel.SendFileAsync(path);
            // extract the URL and store it in the data service for future reference
            service.CurBoard.BoardImgs.ImageList[service.CurBoard.BoardImgs.CurIdx].FullResURL = message.Attachments.First().Url;
            // delete the PIC from the local server
            File.Delete(path);
        }
        else
        {
            await HandleSessionExpired().ConfigureAwait(false);
        }
    }

    private async Task ConvertSexSiteImgURLtoDiscordFileUpload(string url, string path, ulong msgId)
    {
        // get the service associated
        SelectionDataService dataServiceFound;
        if(_botServices.GifData.TryGetValue(msgId, out var service)) {
            dataServiceFound = service;
        } else if (_botServices.PicData.TryGetValue(msgId, out var picService)) {
            dataServiceFound = picService;
        } else {
            _logger.LogError("Error: No active instance of this service exists.");
            return;
        }

        // now we use the info to get our data
        _logger.LogInformation("[Attempting to access URL]: {url}", url); 
        var imgData = await dataServiceFound.Img_HttpClient.GetByteArrayAsync(url);
        // Save the PIC to the local server
        await File.WriteAllBytesAsync(path, imgData);
        // Upload the PIC directly under our final output
        var message = await Context.Channel.SendFileAsync(path);
        // extract the URL and store it in the data service for future reference
        dataServiceFound.ResultImgs.ImageList[dataServiceFound.ResultImgs.CurIdx].FullResURL = message.Attachments.First().Url;
        // delete the PIC from the local server
        File.Delete(path);
    }

#endregion ConverterHelpers

#region ParseResultsAndPages
 // store everything from our results into the dataservice object
    private async Task ParseGifUrls(string htmlResponse, SelectionDataService service)
    {
        // we wont know the context of the message ID in here so we will have to pass it in
        // load up the html document
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(htmlResponse);

        _logger.LogInformation("HTML Responce retrieved, parsing...");

        // get the nodes for all the thumbnails
        var nodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'masonry_box small_pin_box')]");

        // if we found results
        if (nodes != null)
        {
            // for each result we found
            foreach (var node in nodes)
            {
                // create a new media URL
                MediaImg tmpMediaImg = new MediaImg();

                // find the HTML node of the image
                var imgNode = node.SelectSingleNode(".//img[contains(@class, 'image')]");
                // and get the gifURL of the thumbnail and store it
                tmpMediaImg.ThumbUrl = imgNode?.GetAttributeValue("data-src", imgNode.GetAttributeValue("src", ""));

                // get the alt attribute of the image and store it
                tmpMediaImg.Label = WebUtility.HtmlDecode(imgNode?.GetAttributeValue("alt", ""));
                if(tmpMediaImg.Label == "") tmpMediaImg.Label = "(Untitled)";
                //_logger.LogInformation("Found label: {tmpMediaImg.Label}", tmpMediaImg.Label);

                // now locate the link to the site containing the full res image
                var fullResPageUrlNode = node.SelectSingleNode(".//a[contains(@class, 'image_wrapper')]");
                // obtain the href for it
                var fullResPageUrl = fullResPageUrlNode?.GetAttributeValue("href", "");

                //_logger.LogInformation("Found thumbnail: {tmpMediaImg.ThumbUrl}", tmpMediaImg.ThumbUrl);

                // define the referer for the full res image, but dont access it, this will cause dramatic slowdown.
                if (!string.IsNullOrEmpty(fullResPageUrl))
                {
                    var fullResUrl = "https://www.sex.com" + fullResPageUrl;
                    tmpMediaImg.FullresReferer = new Uri(fullResUrl);
                }

                // Get the tags
                var tagNodes = node.SelectNodes(".//div[@class='tags']//a");
                var tags = tagNodes?.Select(tagNode => tagNode.InnerText).ToList();

                // add the tags to the object
                tmpMediaImg.Tags = tags;

                // add the object to the list of mediaUrl's
                service.AddMediaImg(tmpMediaImg);
            }
        }
    }

    // if you want comments to help you understand this method more, see the pic version of this
    private async Task ParsePicUrls(string htmlResponse, SelectionDataService service)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(htmlResponse);

        var nodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'masonry_box small_pin_box')]");

        if (nodes != null)
        {
            // get the full res urls and the thumbnail urls and their referrers
            foreach (var node in nodes)
            {
                MediaImg tmpMediaImg = new MediaImg();

                // get the preview image URL
                var imgNode = node.SelectSingleNode(".//img[contains(@class, 'image')]");
                tmpMediaImg.ThumbUrl = imgNode?.GetAttributeValue("data-src", imgNode.GetAttributeValue("src", ""));
                //_logger.LogInformation("Found thumbnail: {tmpMediaImg.ThumbUrl}", tmpMediaImg.ThumbUrl);

                // get the full res URL href
                var fullResPageUrlNode = node.SelectSingleNode(".//a[contains(@class, 'image_wrapper')]");
                var fullResPageUrl = fullResPageUrlNode?.GetAttributeValue("href", "");
                tmpMediaImg.Label = fullResPageUrlNode?.GetAttributeValue("title", "");

                if (!string.IsNullOrEmpty(fullResPageUrl))
                {
                    // set the referer to the full res page
                    var fullResUrl = "https://www.sex.com" + fullResPageUrl;
                    tmpMediaImg.FullresReferer = new Uri(fullResUrl);
                }

                // Get the tags and set them
                var tagNodes = node.SelectNodes(".//div[@class='tags']//a");
                var tags = tagNodes?.Select(tagNode => tagNode.InnerText).ToList();
                tmpMediaImg.Tags = tags;

                // add the object to the list of mediaUrl's
                service.AddMediaImg(tmpMediaImg);
            }
        }
    }


    // if you want comments to help you understand this method more, see the board version of this
    private async Task ParseBoardResults(string htmlResponse, ulong msgId)
    {
        // get the service
        if(_botServices.BoardData.TryGetValue(msgId, out var service))
        {
            _logger.LogTrace("Service found and is valid");
            
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlResponse);

            var nodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'masonry_box small_board_box')]");

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    Board tmpBoard = new Board();

                    // Extract the title
                    var titleNode = node.SelectSingleNode(".//div[@class='title']//a//strong");
                    tmpBoard.UpdateTitle(WebUtility.HtmlDecode(titleNode?.InnerText));
                    // if  http:// is in the title, strip it
                    if (tmpBoard.Title.Contains("http://")) { tmpBoard.UpdateTitle(tmpBoard.Title.Replace("http://", "")); }
                    // if a . is in the title strip it
                    if (tmpBoard.Title.Contains(".")) { tmpBoard.UpdateTitle(tmpBoard.Title.Replace(".", "")); }

                    // Extract the href
                    var hrefNode = node.SelectSingleNode(".//div[@class='title']//a");
                    tmpBoard.UpdateBoardUri(new Uri("https://www.sex.com" + hrefNode?.GetAttributeValue("href", string.Empty)));

                    //_logger.LogInformation("Fetching search referer for board {boardTitle}\n{link}", tmpBoard.Title, tmpBoard.BoardPage.searchReferrer);
                    // Extract the first 2 thumbnail images
                    var thumbnailNodes = node.SelectNodes(".//div[@class='board-thumb']//img").Take(2);
                    // Add the thumbnails to the board
                    tmpBoard.ImportThumbnails(
                        thumbnailNodes?
                            .Where(n => !WebUtility.HtmlDecode(n.GetAttributeValue("src", "")).Contains("data:image"))
                            .Select(n => new PreviewImg { ThumbUrl = WebUtility.HtmlDecode(n.GetAttributeValue("src", "")) })
                            .ToList()
                    );
                    
                    // Add the board to the list of boards
                    service.AddBoard(tmpBoard);
                }
            }
        }
        // the service was not found, so inform the user
        else
        {
            EmbedBuilder eb = new();
            eb.WithTitle("Session expired");
            eb.WithDescription("This session has expired as it was active prior to the last bot restart, or has been 6 hours since interaction.");
            eb.WithColor(Color.Red);
            ComponentBuilder cb = new();
            await ModifyInteraction(eb, cb).ConfigureAwait(false);
        }
    }

    private async Task ParseBoardPage(string htmlResponse, ulong msgId)
    {
        // try and get the service. If it is not found, run the else condition
        if(_botServices.BoardData.TryGetValue(msgId, out var service))
        {
            _logger.LogTrace("Service found and is valid");
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlResponse);

            var nodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'masonry_box small_pin_box')]");

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    MediaImg tmpMediaImg = new MediaImg();
                    // Extract the title and assign it to the MediaImg's Label
                    var titleNode = node.SelectSingleNode(".//div[contains(@class, 'title')]/a");
                    tmpMediaImg.Label = WebUtility.HtmlDecode(titleNode?.InnerText);
                    //_logger.LogInformation("Title: {title}", tmpMediaImg.Label);

                    // Extract the href and assign it to the MediaImg's Url
                    var imgNode = node.SelectSingleNode(".//img[contains(@class, 'image')]");
                    tmpMediaImg.ThumbUrl = imgNode?.GetAttributeValue("data-src", imgNode.GetAttributeValue("src", ""));

                    // Extract the full resolution image URL
                    var fullResPageUrlNode = node.SelectSingleNode(".//a[contains(@class, 'image_wrapper')]");
                    var fullResPageUrl = fullResPageUrlNode?.GetAttributeValue("href", "");
                    if (!string.IsNullOrEmpty(fullResPageUrl))
                    {
                        var fullResUrl = "https://www.sex.com" + fullResPageUrl;
                        tmpMediaImg.FullresReferer = new Uri(fullResUrl);
                    }

                    // Add the MediaImg to the list of media images
                    service.CurBoard.BoardImgs.AddMediaImg(tmpMediaImg);
                }
            }
        }
        // the trygetvalue failed, so output the session expired error
        else
        {
            EmbedBuilder eb = new();
            eb.WithTitle("Session expired");
            eb.WithDescription("This session has expired as it was active prior to the last bot restart, or has been 6 hours since interaction.");
            eb.WithColor(Color.Red);
            ComponentBuilder cb = new();
            await ModifyInteraction(eb, cb).ConfigureAwait(false);
        }
    }

    /// <summary> General helper function called whenever a trygetvalue fails. </summary>
    private async Task HandleSessionExpired()
    {
        EmbedBuilder eb = new();
        eb.WithTitle("Session expired");
        eb.WithDescription("This session has expired as it was active prior to the last bot restart, or has been 6 hours since interaction.");
        eb.WithColor(Color.Red);
        ComponentBuilder cb = new();
        await ModifyInteraction(eb, cb).ConfigureAwait(false);
    }
#endregion ParseResultsAndPages
}

#pragma warning restore MA0004
#pragma warning restore CS8601