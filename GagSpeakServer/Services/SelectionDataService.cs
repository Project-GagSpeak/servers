namespace GagspeakServer.Services;
public class SelectionDataService : ResultsPageInfo
{
    /// <summary> The http client for making requests to sex.com </summary>
    public HttpClient Img_HttpClient { get; set; } = new HttpClient();

    /// <summary> The list of images in the results </summary>
    public MediaCollection ResultImgs { get; set; } = new MediaCollection();

    /// <summary> the sorting filter used for the search </summary>
    public string SortFilter { get; set; } = "relevance";

    public SelectionDataService() { }

    public SelectionDataService(string filter)
    {
        if(filter != "") SortFilter = filter;
    }
    
    /// <summary> Adds a new MediaImg to the imageList </summary>
    public void AddMediaImg(MediaImg newMediaImg) => ResultImgs.AddMediaImg(newMediaImg);
    /// <summary> Updates the CurIndex value in the media collection </summary>
    public void UpdateCurIdx(int newIndex) => ResultImgs.UpdateCurIdx(newIndex);

    /// <summary> Updates the referer uri to the search results page </summary>
    public void UpdateResultsPageReferer(Uri newReferer) => base.ResultsPageReferer = newReferer;

    /// <summary> Updates the search terms field with our search terms. </summary>
    public void UpdateSearchTerms(string newSearchTerms) => base.SearchTerms = newSearchTerms;
}

