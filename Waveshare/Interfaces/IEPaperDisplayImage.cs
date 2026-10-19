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

    /// <summary> Display an Image on the Display using a previous image state to prevent ghosting during partial refresh. </summary>
    /// <param name="image">Image that should be displayed</param>
    /// <param name="previousImage">Previous image state to clear</param>
    /// <param name="dithering">Use Dithering</param>
    /// <param name="partialRefresh">Enable Refreshing only part of the display.</param>
    /// <param name="x">Leftmost coordinate for partial refresh</param> 
    /// <param name="y">Uppermost coordinate for partial refresh </param> 
    void DisplayImage(T image, T previousImage, bool dithering, bool partialRefresh = false, int x = 0, int y = 0);

    /// <summary> Display a 4-level grayscale image on supported displays. </summary>
    /// <param name="image">4-gray pre-quantized/dithered image to display</param>
    void DisplayImage4Gray(T image);
}
