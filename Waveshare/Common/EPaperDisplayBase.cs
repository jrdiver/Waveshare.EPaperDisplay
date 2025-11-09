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

#region Usings

using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Waveshare.Interfaces.Internal;

#endregion Usings

namespace Waveshare.Common
{
    /// <summary>
    /// Base Class for all E-Paper Devices
    /// </summary>
    internal abstract class EPaperDisplayBase : IEPaperDisplayInternal
    {

        //########################################################################################

        #region Constants

        /// <summary>
        /// Timeout for the device Wait Until Ready
        /// </summary>
        private const int WaitUntilReadyTimeout = 50000;

        #endregion Constants

        //########################################################################################

        #region Fields

        /// <summary>
        /// Buffered Display Writer
        /// </summary>
        private IEPaperDisplayWriter m_DisplayWriter;

        /// <summary>
        /// Has class been disposed
        /// </summary>
        private bool m_Disposed;

        #endregion Fields

        //########################################################################################

        #region Properties

        /// <summary>
        /// Pixel Width of the Display
        /// </summary>
        public abstract int Width { get; }

        /// <summary>
        /// Pixel Height of the Display
        /// </summary>
        public abstract int Height { get; }

        /// <summary>
        /// Supported Colors of the E-Paper Device
        /// </summary>
        public abstract ByteColor[] SupportedByteColors { get; }

        /// <summary>
        /// Color Bytes of the E-Paper Device corresponding to the supported colors
        /// </summary>
        public abstract byte[] DeviceByteColors { get; }

        /// <summary>
        /// Color Bytes per Pixel (R, G, B)
        /// </summary>
        public int ColorBytesPerPixel { get; set; } = 3;

        /// <summary>
        /// Color Bytes per Pixel (R, G, B)
        /// </summary>
        public bool IsPalletMonochrome { get; private set; }

        /// <summary>
        /// State of the display sleep state
        /// </summary>
        protected bool DisplayIsSleeping { get; set; }

        /// <summary>
        /// Display Writer assigned to the device
        /// </summary>
        protected IEPaperDisplayWriter DisplayWriter => m_DisplayWriter ??= GetDisplayWriter();

        /// <summary>
        /// Pixels per Byte on the Device
        /// </summary>
        public abstract int PixelPerByte { get; }

        /// <summary>
        /// E-Paper Hardware Interface for GPIO and SPI Bus
        /// </summary>
        public IEPaperDisplayHardware EPaperDisplayHardware { get; set; }

        /// <summary>
        /// Get Status Command
        /// </summary>
        protected abstract byte GetStatusCommand { get; }

        /// <summary>
        /// Start Data Transmission Command
        /// </summary>
        protected abstract byte StartDataTransmissionCommand { get; }

        /// <summary>
        /// Stop Data Transmission Command
        /// </summary>
        protected abstract byte StopDataTransmissionCommand { get; }

        /// <summary>
        /// Display DeepSleep Command
        /// </summary>
        protected abstract byte DeepSleepComand { get; }

        #endregion Properties

        //########################################################################################

        #region Constructor / Dispose / Finalizer

        /// <summary>
        /// Finalizer
        /// </summary>
        ~EPaperDisplayBase() => Dispose(false);

        /// <summary>
        /// Dispose of class
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose of instantiated objects
        /// </summary>
        /// <param name="disposing">Explicit dispose</param>
        protected virtual void Dispose(bool disposing)
        {
            if (m_Disposed)
            {
                return;
            }

            if (disposing)
            {
                m_DisplayWriter?.Dispose();
                DeviceShutdown();
            }

            m_Disposed = true;
        }

        #endregion Constructor / Dispose / Finalizer

        //########################################################################################

        #region Public Methods

        /// <summary>
        /// Wait until the display is ready
        /// </summary>
        /// <returns>true if device is ready, false for timeout</returns>
        public bool WaitUntilReady()
        {
            return WaitUntilReady(WaitUntilReadyTimeout);
        }

        /// <summary>
        /// Wait until the display is ready
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns>true if device is ready, false for timeout</returns>
        public bool WaitUntilReady(int timeout)
        {
            bool busy;

            Stopwatch timeoutTimer = Stopwatch.StartNew();
            do
            {
                SendCommand(GetStatusCommand);
                busy = !(EPaperDisplayHardware.BusyPin == PinValue.High);

                if (timeoutTimer.ElapsedMilliseconds > timeout)
                {
                    return false;
                }
            } while (busy);

            return true;
        }

        /// <summary>
        /// Reset the Display
        /// </summary>
        public void Reset()
        {
            EPaperDisplayHardware.ResetPin = PinValue.High;
            Thread.Sleep(200);
            EPaperDisplayHardware.ResetPin = PinValue.Low;
            Thread.Sleep(2);
            EPaperDisplayHardware.ResetPin = PinValue.High;
            Thread.Sleep(200);
        }

        /// <summary>
        /// Display a Image on the Display
        /// </summary>
        /// <param name="rawImage">Bitmap that should be displayed</param>
        /// <param name="dithering"></param>
        public void DisplayImage(IRawImage rawImage, bool dithering)
        {
            SendCommand(StartDataTransmissionCommand);

            if (dithering)
            {
                SendDitheredBitmapToDevice(rawImage.ScanLine, rawImage.Stride, rawImage.Width, rawImage.Height);
            }
            else
            {
                SendBitmapToDevice(rawImage.ScanLine, rawImage.Stride, rawImage.Width, rawImage.Height);
            }

            if (StopDataTransmissionCommand < byte.MaxValue)
            {
                SendCommand(StopDataTransmissionCommand);
            }

            TurnOnDisplay();
        }

        /// <summary>
        /// Initialize the Display with the Hardware Interface
        /// </summary>
        /// <param name="ePaperDisplayHardware"></param>
        public void Initialize(IEPaperDisplayHardware ePaperDisplayHardware)
        {
            EPaperDisplayHardware = ePaperDisplayHardware;

            DeviceInitialize();

            IsPalletMonochrome = true;
            foreach (ByteColor color in SupportedByteColors)
            {
                if (!color.IsMonochrome)
                {
                    IsPalletMonochrome = false;
                    break;
                }
            }
        }

        /// <summary>
        /// Clear the Display to White
        /// </summary>
        public abstract void Clear();

        /// <summary>
        /// Clear the Display to Black
        /// </summary>
        public abstract void ClearBlack();

        /// <summary>
        /// Power the controller on.  Do not use with SleepMode.
        /// </summary>
        public abstract void PowerOn();

        /// <summary>
        /// Power the controller off.  Do not use with SleepMode.
        /// </summary>
        public abstract void PowerOff();

        /// <summary>
        /// Send the Display into SleepMode
        /// </summary>
        public virtual void Sleep()
        {
            if (!DisplayIsSleeping)
            {
                PowerOff();
                SendCommand(DeepSleepComand);
                SendData(0xA5);
                DisplayIsSleeping = true;
            }
        }

        /// <summary>
        /// WakeUp the Display from SleepMode
        /// </summary>
        public void WakeUp()
        {
            DeviceInitialize();
            DisplayIsSleeping = false;
        }

        #endregion Public Methods

        //########################################################################################

        #region Protected Methods

        /// <summary>
        /// Device specific Initialise
        /// </summary>
        protected abstract void DeviceInitialize();

        /// <summary>
        /// Turn the Display On
        /// </summary>
        protected abstract void TurnOnDisplay();

        /// <summary>
        /// Convert a Color to a Byte
        /// </summary>
        /// <param name="rgb"></param>
        /// <returns></returns>
        protected abstract byte ColorToByte(ByteColor rgb);

        /// <summary>
        /// Get the DisplayWriter
        /// </summary>
        /// <returns></returns>
        protected virtual IEPaperDisplayWriter GetDisplayWriter()
        {
            return new EPaperDisplayWriter(this);
        }

        /// <summary>
        /// Send a Command to the Display
        /// </summary>
        /// <param name="command"></param>
        public void SendCommand(byte command)
        {
            EPaperDisplayHardware.SpiDcPin = PinValue.Low;
            EPaperDisplayHardware.WriteByte(command);
        }

        /// <summary>
        /// Send a command to the display
        /// </summary>
        /// <param name="commands"></param>
        protected void SendCommands(params byte[] commands)
        {
            EPaperDisplayHardware.SpiDcPin = PinValue.Low;
            EPaperDisplayHardware.Write(commands);
        }

        /// <summary>
        /// Send data to the display
        /// </summary>
        /// <param name="data"></param>
        public void SendData(byte data)
        {
            EPaperDisplayHardware.SpiDcPin = PinValue.High;
            EPaperDisplayHardware.WriteByte(data);
        }

        /// <summary>
        /// Send data to the display
        /// </summary>
        /// <param name="data"></param>
        public void SendData(params byte[] data)
        {
            EPaperDisplayHardware.SpiDcPin = PinValue.High;
            EPaperDisplayHardware.Write(data);
        }

        /// <summary>
        /// Send data to the display
        /// </summary>
        /// <param name="stream"></param>
        public void SendData(MemoryStream stream)
        {
            EPaperDisplayHardware.SpiDcPin = PinValue.High;
            EPaperDisplayHardware.Write(stream);
        }

        /// <summary>
        /// Get a colored scan line on the device
        /// </summary>
        /// <param name="rgb"></param>
        /// <returns></returns>
        public byte[] GetColoredLineOnDevice(ByteColor rgb)
        {
            byte devicePixel = GetMergedPixelDataInByte(rgb);

            int outputWidth = Width / PixelPerByte;
            byte[] outputLine = new byte[outputWidth];

            for (int x = 0; x < outputLine.Length; x++)
            {
                outputLine[x] = devicePixel;
            }

            return outputLine;
        }

        /// <summary>
        /// Gets the index for the supported color closest to a color
        /// </summary>
        /// <param name="color">Color to look up</param>
        /// <returns>Color index of closest supported color</returns>
        public int GetColorIndex(ByteColor color)
        {
            double minDistance = GetColorDistance(color, SupportedByteColors[0]);
            if (minDistance < 1)
            {
                return 0;
            }

            int bestIndex = 0;

            for (int i = 1; i < SupportedByteColors.Length; i++)
            {
                bool monochrome = (color.R == color.G && color.G == color.B);
                ByteColor deviceColor = SupportedByteColors[i];
                double distance = GetColorDistance(color, deviceColor);
                if (distance <= minDistance && deviceColor.IsMonochrome == monochrome)
                {
                    minDistance = distance;
                    bestIndex = i;
                    if (minDistance < 1)
                    {
                        break;
                    }
                }
            }

            return bestIndex;
        }

        #endregion Protected Methods

        //########################################################################################

        #region Internal Methods

        /// <summary>
        /// Merge pixels into a Device Byte
        /// </summary>
        /// <param name="pixel"></param>
        /// <returns></returns>
        internal byte MergePixelDataInByte(params byte[] pixel)
        {
            if (pixel == null || pixel.Length == 0)
            {
                throw new ArgumentException($"Argument {nameof(pixel)} can not be null or empty", nameof(pixel));
            }

            if (pixel.Length != PixelPerByte)
            {
                throw new ArgumentException($"Argument {nameof(pixel)}.Length is not PixelPerByte {PixelPerByte}", nameof(pixel));
            }

            const int bitStates = 2;
            const int bitsInByte = 8;

            int bitMoveLength = bitsInByte / PixelPerByte;
            int maxValue = (byte) Math.Pow(bitStates, bitMoveLength) - 1;

            byte output = 0;

            for (int i = 0; i < pixel.Length; i++)
            {
                int bitMoveValue = bitsInByte - bitMoveLength - (i * bitMoveLength); 

                byte value = (byte)(pixel[i] << bitMoveValue);
                int mask = maxValue << bitMoveValue;
                byte posValue = (byte)(value & mask);

                output |= posValue;
            }

            return output;
        }

        /// <summary>
        /// Send a Bitmap as Byte Array to the Device
        /// </summary>
        /// <param name="scanLine">Int Pointer to the start of the Bytearray</param>
        /// <param name="stride">Length of a ScanLine</param>
        /// <param name="maxX">Max Pixels horizontal</param>
        /// <param name="maxY">Max Pixels Vertical</param>
        internal void SendBitmapToDevice(IntPtr scanLine, int stride, int maxX, int maxY)
        {
            byte[] inputLine = new byte[stride];
            ByteColor pixel = new ByteColor(0, 0, 0);

            for (int y = 0; y < maxY; y++, scanLine += stride)
            {
                Marshal.Copy(scanLine, inputLine, 0, inputLine.Length);

                int xPos = 0;
                for (int x = 0; x < maxX; x++)
                {
                    pixel.SetBGR(inputLine[xPos++], inputLine[xPos++], inputLine[xPos++], IsPalletMonochrome);
                    if (ColorBytesPerPixel > 3)
                    {
                        xPos += ColorBytesPerPixel - 3;
                    }
                    DisplayWriter.Write(GetColorIndex(pixel));
                }

                for (int x = maxX; x < Width; x++)
                {
                    DisplayWriter.WriteBlankPixel();
                }
            }

            // Write blank lines if image is smaller than display.
            for (int y = maxY; y < Height; y++)
            {
                DisplayWriter.WriteBlankLine();
            }

            DisplayWriter.Finish();
        }

        /// <summary>
        /// Send a Dithered Bitmap as Byte Array to the Device
        /// </summary>
        /// <param name="scanLine">Int Pointer to the start of the Bytearray</param>
        /// <param name="stride">Length of a ScanLine</param>
        /// <param name="maxX">Max Pixels horizontal</param>
        /// <param name="maxY">Max Pixels Vertical</param>
        internal void SendDitheredBitmapToDevice(IntPtr scanLine, int stride, int maxX, int maxY)
        {
            ByteColor[,] data = new ByteColor[Width, 2];
            int currentLine = 0;
            int previousLine = 1;

            byte[] inputLine = new byte[stride];
            ByteColor pixel = new ByteColor(0, 0, 0);
            bool odd = false;
            bool dither = false;

            for (int y = 0; y < maxY; y++, scanLine += stride)
            {
                if (odd)
                {
                    previousLine = 0;
                    currentLine = 1;
                    odd = false;
                    dither = true;
                }
                else
                {
                    previousLine = 1;
                    currentLine = 0;
                    odd = true;
                }

                Marshal.Copy(scanLine, inputLine, 0, inputLine.Length);

                int xPos = 0;
                for (int x = 0; x < maxX; x++)
                {
                    pixel.SetBGR(inputLine[xPos++], inputLine[xPos++], inputLine[xPos++], IsPalletMonochrome);
                    if (ColorBytesPerPixel > 3)
                    {
                        xPos += ColorBytesPerPixel - 3;
                    }
                    data[x, currentLine] = pixel;
                }

                for (int x = maxX; x < Width; x++)
                {
                    data[x, currentLine] = ByteColors.White;
                }

                if (dither)
                {
                    DitherAndWrite(data, previousLine, currentLine, false);
                }
            }

            // Finish last line
            DitherAndWrite(data, currentLine, previousLine, true);

            // Write blank lines if image is smaller than display.
            for (int y = maxY; y < Height; y++)
            {
                DisplayWriter.WriteBlankLine();
            }

            DisplayWriter.Finish();
        }

        #endregion Internal Methods

        //########################################################################################

        #region Private Methods

        /// <summary>
        /// Dither and Write a Line to the Display
        /// </summary>
        /// <param name="data"></param>
        /// <param name="line1"></param>
        /// <param name="line2"></param>
        /// <param name="lastLine"></param>
        private void DitherAndWrite(ByteColor[,] data, int line1, int line2, bool lastLine)
        {
            bool odd = true;
            for (int x = 0; x < Width; x++)
            {
                ByteColor oldPixel = data[x, line1];
                int colorIndex = GetColorIndex(oldPixel);
                ByteColor newPixel = SupportedByteColors[colorIndex];

                DisplayWriter.Write(colorIndex);

                int errorR = oldPixel.R - newPixel.R;
                int errorG = oldPixel.G - newPixel.G;
                int errorB = oldPixel.B - newPixel.B;

                if (lastLine)
                {
                    continue;
                }

                if (odd)
                {
                    if (x < Width - 1)
                    {
                        data[x + 1, line1].R = ConvertColorIntToByte(data[x + 1, line1].R + errorR * 7 / 16);
                        data[x + 1, line1].G = ConvertColorIntToByte(data[x + 1, line1].G + errorG * 7 / 16);
                        data[x + 1, line1].B = ConvertColorIntToByte(data[x + 1, line1].B + errorB * 7 / 16);
                    }

                    if (x > 0)
                    {
                        data[x - 1, line2].R = ConvertColorIntToByte(data[x - 1, line2].R + errorR * 3 / 16);
                        data[x - 1, line2].G = ConvertColorIntToByte(data[x - 1, line2].G + errorG * 3 / 16);
                        data[x - 1, line2].B = ConvertColorIntToByte(data[x - 1, line2].B + errorB * 3 / 16);
                    }

                    data[x, line2].R = ConvertColorIntToByte(data[x, line2].R + errorR * 5 / 16);
                    data[x, line2].G = ConvertColorIntToByte(data[x, line2].G + errorG * 5 / 16);
                    data[x, line2].B = ConvertColorIntToByte(data[x, line2].B + errorB * 5 / 16);

                    if (x < Width - 1)
                    {
                        data[x + 1, line2].R = ConvertColorIntToByte(data[x + 1, line2].R + errorR * 1 / 16);
                        data[x + 1, line2].G = ConvertColorIntToByte(data[x + 1, line2].G + errorG * 1 / 16);
                        data[x + 1, line2].B = ConvertColorIntToByte(data[x + 1, line2].B + errorB * 1 / 16);
                    }
                }
                else
                {
                    if (x > 0)
                    {
                        data[x - 1, line1].R = ConvertColorIntToByte(data[x - 1, line1].R + errorR * 7 / 16);
                        data[x - 1, line1].G = ConvertColorIntToByte(data[x - 1, line1].G + errorG * 7 / 16);
                        data[x - 1, line1].B = ConvertColorIntToByte(data[x - 1, line1].B + errorB * 7 / 16);
                    }

                    if (x < Width - 1)
                    {
                        data[x + 1, line2].R = ConvertColorIntToByte(data[x + 1, line2].R + errorR * 3 / 16);
                        data[x + 1, line2].G = ConvertColorIntToByte(data[x + 1, line2].G + errorG * 3 / 16);
                        data[x + 1, line2].B = ConvertColorIntToByte(data[x + 1, line2].B + errorB * 3 / 16);
                    }

                    data[x, line2].R = ConvertColorIntToByte(data[x, line2].R + errorR * 5 / 16);
                    data[x, line2].G = ConvertColorIntToByte(data[x, line2].G + errorG * 5 / 16);
                    data[x, line2].B = ConvertColorIntToByte(data[x, line2].B + errorB * 5 / 16);

                    if (x > 0)
                    {
                        data[x - 1, line2].R = ConvertColorIntToByte(data[x - 1, line2].R + errorR * 1 / 16);
                        data[x - 1, line2].G = ConvertColorIntToByte(data[x - 1, line2].G + errorG * 1 / 16);
                        data[x - 1, line2].B = ConvertColorIntToByte(data[x - 1, line2].B + errorB * 1 / 16);
                    }
                }

                odd = !odd;
            }
        }

        /// <summary>
        /// Shut down the device
        /// </summary>
        private void DeviceShutdown()
        {
            if (EPaperDisplayHardware != null)
            {
                Sleep();

                EPaperDisplayHardware?.Dispose();
                EPaperDisplayHardware = null;
            }
        }

        /// <summary>
        /// Get a byte on the device with one Color for all pixels
        /// </summary>
        /// <param name="rgb"></param>
        /// <returns></returns>
        private byte GetMergedPixelDataInByte(ByteColor rgb)
        {
            byte pixelData = ColorToByte(rgb);
            byte[] deviceBytesPerPixel = new byte[PixelPerByte];

            for (int i = 0; i < deviceBytesPerPixel.Length; i++)
            {
                deviceBytesPerPixel[i] = pixelData;
            }

            return MergePixelDataInByte(deviceBytesPerPixel);
        }

        /// <summary>
        /// Get the color distance between two colors
        /// </summary>
        /// <param name="color1"></param>
        /// <param name="color2"></param>
        /// <returns></returns>
        private static double GetColorDistance(ByteColor color1, ByteColor color2)
        {
            (double Y, double U, double V) yuv1 = GetYuv(color1);
            (double Y, double U, double V) yuv2 = GetYuv(color2);

            return Math.Abs(yuv1.Y - yuv2.Y) + Math.Abs(yuv1.U - yuv2.U) + Math.Abs(yuv1.V - yuv2.V);
        }

        /// <summary>
        /// Calculate YUV color space
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        private static (double Y, double U, double V) GetYuv(ByteColor color)
        {
            return (color.R *  .299000 + color.G *  .587000 + color.B *  .114000,
                    color.R * -.168736 + color.G * -.331264 + color.B *  .500000 + 128,
                    color.R *  .500000 + color.G * -.418688 + color.B * -.081312 + 128);
        }

        /// <summary>
        /// Convert a Color Int to a Color Byte
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private static byte ConvertColorIntToByte(int level)
        {
            if (level < byte.MinValue)
            {
                return byte.MinValue;
            }
            
            if (level > byte.MaxValue)
            {
                return byte.MaxValue;
            }

            return (byte)level;
        }

        #endregion Private Methods

        //########################################################################################

    }
}
