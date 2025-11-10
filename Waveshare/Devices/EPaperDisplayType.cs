#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// MIT License
// Copyright(c) 2019 Andre Wehrli

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// --------------------------------------------------------------------------------------------------------------------
#endregion Copyright

// ReSharper disable InconsistentNaming
namespace Waveshare.Devices;

/// <summary> Type of the E-Paper Display </summary>
public enum EPaperDisplayType
{
    /// <summary> Default value </summary>
    None,
    /// <summary>
    /// Type: Waveshare 7.5inch e-Paper (B) <br/>
    /// Color: Black, White and Red <br/>
    /// Display Resolution: 640*384
    /// </summary>
    WaveShare7In5Bc,
    /// <summary>
    /// Type: Waveshare 7.5inch e-Paper V2 <br/>
    /// Color: Black and White <br/>
    /// Display Resolution: 800*480
    /// </summary>
    WaveShare7In5_V2,
    /// <summary>
    /// Type: Waveshare 7.5inch e-Paper V2 <br/>
    /// Color: Black, White and Red <br/>
    /// Display Resolution: 800*480
    /// </summary>
    WaveShare7In5b_V2,
    /// <summary>
    /// Type: Waveshare 5.65inch e-Paper (F) <br/>
    /// Color: Black, White, Green, Blue, Red, Yellow and Orange <br/>
    /// Display Resolution: 600*448 
    /// </summary>
    WaveShare5In65f
}