namespace GagspeakServer.Services;

public class Board : ResultsPageInfo
{
    public string Title { get; private set; }
    public List<PreviewImg> Thumbnails { get; private set; } = new List<PreviewImg>();
    public MediaCollection BoardImgs { get; private set; } = new MediaCollection();

    /// <summary> Updates the title of the board </summary>
    public void UpdateTitle(string newTitle) => Title = newTitle;

    /// <summary> Updates the uri of the boards page link </summary>
    public void UpdateBoardUri(Uri newUri) => base.ResultsPageReferer = newUri;

    /// <summary> Imports a list of thumbnails to the board </summary>
    public void ImportThumbnails(List<PreviewImg> newThumbnails) => Thumbnails = newThumbnails;

    /// <summary> Adds a new thumbnail to the board </summary>
    public void AddThumbnail(PreviewImg newThumbnail) => Thumbnails.Add(newThumbnail);

    /// <summary> Adds a new image to the board </summary>
    public void AddImage(MediaImg newImage) => BoardImgs.AddMediaImg(newImage);

    /// <summary> Updates the current index of the board </summary>
    public void UpdateBoardCurIdx(int newIndex) => BoardImgs.UpdateCurIdx(newIndex);
}