namespace GagspeakServer.Services;

/// <summary>
/// Base class to hold the image list and the current image we are viewing
/// </summary>
public class ResultsPageInfo
{
    /// <summary> The referrer uri to the search results page </summary>
    public Uri ResultsPageReferer { get; protected set; }
    
    /// <summary> The search terms used to get the results </summary>
    public string SearchTerms { get; protected set; } 
}