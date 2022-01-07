﻿#region Copyright
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

#region Usings

#endregion Usings

namespace Waveshare.Devices.Epd7in5b_V2
{
    /// <summary>
    /// Byte values for the supported hardware colors
    /// </summary>
    // ReSharper disable once InconsistentNaming
    internal class Epd7In5b_V2Colors
    {

        /// <summary>
        /// Byte value for color black
        /// </summary>
        public const byte Black = 0x00;

        /// <summary>
        /// Byte value for color white
        /// </summary>
        public const byte White = 0x01;

        /// <summary>
        /// Byte value for color red
        /// </summary>
        public const byte Red = 0x01;

        /// <summary>
        /// Byte value for color not red
        /// </summary>
        public const byte NotRed = 0x00;

    }
}
