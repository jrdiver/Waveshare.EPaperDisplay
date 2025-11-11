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

using System.Diagnostics;
using System.Reflection;
using SkiaSharp;
using Waveshare.Devices;
using Waveshare.Interfaces;

namespace Waveshare.Example
{
    /// <summary> Example for the Waveshare E-Paper Library </summary>
    internal class Program
    {
        #region Public Methods

        /// <summary> Application Main </summary>
        /// <param name="args">Commandline arguments</param>
        public static void Main(string[] args)
        {
            Console.Write("Initializing E-Paper Display...");
            Stopwatch time = Stopwatch.StartNew();
            using IEPaperDisplaySKBitmap ePaperDisplay = EPaperDisplay.Create(EPaperDisplayType.WaveShare7In5_V2);
            time.Stop();
            Console.WriteLine($" [Done {time.ElapsedMilliseconds} ms]");

            using SKBitmap bitmap = LoadBitmap(args, ePaperDisplay.Width, ePaperDisplay.Height);
            if (bitmap == null)
                return;

            Console.Write("Waiting for E-Paper Display...");
            time = Stopwatch.StartNew();
            ePaperDisplay.Clear();
            ePaperDisplay.WaitUntilReady();
            time.Stop();
            Console.WriteLine($" [Done {time.ElapsedMilliseconds} ms]");

            Console.Write("Sending Image to E-Paper Display...");
            time = Stopwatch.StartNew();
            ePaperDisplay.DisplayImage(bitmap, true);
            time.Stop();
            Console.WriteLine($" [Done {time.ElapsedMilliseconds} ms]");

            Console.WriteLine("Done");
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary> Load Bitmap from arguments or get the default bitmap </summary>
        /// <param name="args"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private static SKBitmap LoadBitmap(string[] args, int width, int height)
        {
            string bitmapFilePath;

            if (args == null || args.Length == 0)
            {
                string fileName = $"like_a_sir_{width}x{height}.bmp";
                bitmapFilePath = Path.Combine(ExecutingAssemblyPath, fileName);
            }
            else
                bitmapFilePath = args.First();

            if (File.Exists(bitmapFilePath)) return LoadSKBitmapFromFile(bitmapFilePath);

            Console.WriteLine($"Can not find Bitmap file: '{bitmapFilePath}'!");
            return null;
        }

        /// <summary> Load a SKBitmap from a file </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private static SKBitmap LoadSKBitmapFromFile(string filePath)
        {
            using FileStream stream = File.OpenRead(filePath);
            SKBitmap bitmap = SKBitmap.Decode(stream);
            return bitmap;
        }

        /// <summary> Return the path of the executing assembly </summary>
        private static string ExecutingAssemblyPath
        {
            get
            {
                string path = Assembly.GetExecutingAssembly().Location;
                return Path.GetDirectoryName(path);
            }
        }

        #endregion Private Methods
    }
}
