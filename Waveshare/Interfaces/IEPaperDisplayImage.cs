namespace Waveshare.Interfaces;

/// <summary> Base generic Interface for ImageLoaders </summary>
/// <typeparam name="T"></typeparam>
public interface IEPaperDisplayImage<T> : IEPaperDisplay
{
    /// <summary> Display an Image on the Display </summary>
    /// <param name="image">Image that should be displayed</param>
    /// <param name="dithering">Use Dithering</param>
    /// <param name="partialRefresh">Enable Refreshing only part of the display.</param>
    /// <param name="x">Leftmost coordinate for partial refresh</param> 
    /// <param name="y">Uppermost coordinate for partial refresh </param> 
    void DisplayImage(T image, bool dithering, bool partialRefresh = false, int x = 0, int y = 0);
}
