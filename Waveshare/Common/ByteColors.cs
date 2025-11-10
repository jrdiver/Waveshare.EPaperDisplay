#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// MIT License
// Copyright(c) 2021 Andre Wehrli

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

namespace Waveshare.Common;

/// <summary> Colors as ByteArrays </summary>
internal class ByteColors
{
    /// <summary> Color White as ByteColor </summary>
    public static readonly ByteColor White = new(255, 255, 255);

    /// <summary> Color Gray as ByteColor </summary>
    public static readonly ByteColor Gray = new(128, 128, 128);

    /// <summary> Color Black as ByteColor </summary>
    public static readonly ByteColor Black = new(0, 0, 0);

    /// <summary> Color Red as ByteColor </summary>
    public static readonly ByteColor Red = new(255, 0, 0);

    /// <summary> Color Green as ByteColor </summary>
    public static readonly ByteColor Green = new(0, 255, 0);

    /// <summary> Color Blue as ByteColor </summary>
    public static readonly ByteColor Blue = new(0, 0, 255);

    /// <summary> Color Yellow as ByteColor </summary>
    public static readonly ByteColor Yellow = new(255, 255, 0);

    /// <summary> Color Orange as ByteColor </summary>
    public static readonly ByteColor Orange = new(255, 165, 0);
}
