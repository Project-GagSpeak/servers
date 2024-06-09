namespace GagspeakServer.Services;

// Base class to hold the image list and the current image we are viewing
public class MediaCollection
{
    /// <summary> The list of images in the results </summary>
    public List<MediaImg> ImageList { get; protected set; } = new List<MediaImg>();

    /// <summary> The current index of the results, for the imageList </summary>
    public int CurIdx { get; protected set; } 

    /// <summary> Set the currentIdx </summary>
    public void UpdateCurIdx(int newIndex) => CurIdx = newIndex;

    /// <summary> Add a new MediaImg to the imageList </summary>
    public void AddMediaImg(MediaImg newMediaImg) => ImageList.Add(newMediaImg);

    /// <summary>
    /// Shifts the current index in the media collection by the specified value backwards.
    /// If the value goes below 0, it will wrap around to the end of the list.
    /// </summary>
    public void ShiftCurIdxBackwards(int shiftValue)
    {
        CurIdx -= shiftValue;
        if (CurIdx < 0)
        {
            CurIdx = ImageList.Count + CurIdx;
        }
    }

    /// <summary>
    /// Shifts the current index in the media collection by the specified value forwards.
    /// If the value goes beyond the end of the list, it will wrap around to the start of the list.
    /// </summary>
    public void ShiftCurIdxForwards(int shiftValue)
    {
        CurIdx += shiftValue;
        if (CurIdx >= ImageList.Count)
        {
            CurIdx = CurIdx % ImageList.Count;
        }
    }
}