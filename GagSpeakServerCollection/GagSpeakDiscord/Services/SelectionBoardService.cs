using System.Net;
using GagspeakDiscord.Services.HelperServices;
using HtmlAgilityPack;

namespace GagspeakDiscord.Services;
#pragma warning disable IDISP001
/// <summary>
/// Service class for the selection board. Boards are the user generated albums in sex.com
/// </summary>
public class SelectionBoardService : ResultsPageInfo
{
    public HttpClient Board_HttpClient { get; set; } // our http client for making requests to sex.com
    public List<Board> Boards { get; private set; } = new List<Board>(); // the boards
    public int BoardSearchIdx { get; private set; } // the current board we are examining
    public Board CurBoard => Boards[BoardSearchIdx]; // the current board we are looking at (reference)

    
    // search terms and page referrer in base class
    public void AddBoard(Board newBoard) => Boards.Add(newBoard);
    public void UpdateBoardSearchIdx(int newIndex) => BoardSearchIdx = newIndex;

    public void UpdateBoardUri(Uri newUri) => base.ResultsPageReferer = newUri;
    public void UpdateBoardSearchTerms(string newSearchTerms) => base.SearchTerms = newSearchTerms;

    public void ShiftSearchIdxBack(int shiftValue)
    {
        BoardSearchIdx -= shiftValue;
        if (BoardSearchIdx < 0)
        {
            BoardSearchIdx = Boards.Count + BoardSearchIdx;
        }
    }

    public void ShiftSearchIdxForward(int shiftValue)
    {
        BoardSearchIdx += shiftValue;
        if (BoardSearchIdx >= Boards.Count)
        {
            BoardSearchIdx = BoardSearchIdx % Boards.Count;
        }
    }

    public void UpdateRequestHeaderReferer(Uri newReferer) 
    {
        Board_HttpClient.DefaultRequestHeaders.Referrer = newReferer;
    }

    /// <summary> 
    /// Fetch the fullresURl and tags for the current embed 
    /// <para> NOTICE: YOU MUST HAVE THE ALBUM HTTPCLIENT REFERER SET CORRECTLY BEFORE CALLING THIS METHOD </para>
    /// </summary>
    public async Task FetchFullResUrlAndTags() 
    {
        // Fetch the full resolution page
        var fullResPageResponse = await Board_HttpClient.GetAsync(
                CurBoard.BoardImgs.ImageList[CurBoard.BoardImgs.CurIdx].FullresReferer).ConfigureAwait(false);
        
        // if the request was successful
        if (fullResPageResponse.IsSuccessStatusCode)
        {
            // Load the full resolution page html
            var fullResPageHtml = await fullResPageResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            var fullResPageHtmlDoc = new HtmlDocument();
            fullResPageHtmlDoc.LoadHtml(fullResPageHtml);

            // Extract the full resolution image source
            var fullResUrlNode = fullResPageHtmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'big_pin_box')]//div[@class='image_frame']//img");
            var encodedUrl = fullResUrlNode.GetAttributeValue("src", "");
            CurBoard.BoardImgs.ImageList[CurBoard.BoardImgs.CurIdx].FullResURL = WebUtility.HtmlDecode(encodedUrl);

            // Extract the tags
            var tagsNode = fullResPageHtmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'big_pin_box')]//div[@class='tags']");
            CurBoard.BoardImgs.ImageList[CurBoard.BoardImgs.CurIdx].Tags = tagsNode?.SelectNodes(".//a").Select(n => WebUtility.HtmlDecode(n.InnerText)).ToList();
        }
    }
}
#pragma warning restore IDISP001