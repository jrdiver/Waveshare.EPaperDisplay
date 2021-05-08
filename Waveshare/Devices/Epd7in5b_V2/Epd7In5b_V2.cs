﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// MIT License
// Copyright(c) 2020 Greg Cannon

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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Waveshare.Common;

#endregion Usings

namespace Waveshare.Devices.Epd7in5b_V2
{
    /// <summary>
    /// Type: Waveshare 7.5inch e-Paper V2
    /// Color: Black, White and Red
    /// Display Resolution: 800*480
    /// </summary>
    // ReSharper disable once InconsistentNaming
    internal sealed class Epd7In5b_V2 : EPaperDisplayBase
    {

        //########################################################################################

        #region Properties

        /// <summary>
        /// Pixels per Byte on the Device
        /// </summary>
        protected override int PixelPerByte { get; } = 8;

        /// <summary>
        /// Pixel Width of the Display
        /// </summary>
        public override int Width { get; } = 800;

        /// <summary>
        /// Pixel Height of the Display
        /// </summary>
        public override int Height { get; } = 480;

        /// <summary>
        /// Supported Colors of the E-Paper Device
        /// </summary>
        public override IList<byte[]> SupportedByteColors { get; } = new List<byte[]> { ByteColors.White, ByteColors.Black, ByteColors.Red };

        /// <summary>
        /// Get Status Command
        /// </summary>
        protected override byte GetStatusCommand { get; } = (byte)Epd7In5b_V2Commands.GetStatus;

        /// <summary>
        /// Start Data Transmission Command
        /// </summary>
        protected override byte StartDataTransmissionCommand { get; } = (byte)Epd7In5b_V2Commands.DataStartTransmission2;

        /// <summary>
        /// Stop Data Transmission Command
        /// </summary>
        protected override byte StopDataTransmissionCommand { get; } = (byte)Epd7In5b_V2Commands.DataStop;

        #endregion Properties

        //########################################################################################

        #region Public Methods

        /// <summary>
        /// Clear the Display to White
        /// </summary>
        public override void Clear()
        {
            FillColor(Epd7In5b_V2Commands.DataStartTransmission1, ByteColors.White);
            FillColor(Epd7In5b_V2Commands.DataStartTransmission2, ByteColors.Black);
            TurnOnDisplay();
        }

        /// <summary>
        /// Clear the Display to Black
        /// </summary>
        public override void ClearBlack()
        {
            FillColor(Epd7In5b_V2Commands.DataStartTransmission1, ByteColors.Black);
            FillColor(Epd7In5b_V2Commands.DataStartTransmission2, ByteColors.Black);
            TurnOnDisplay();
        }

        /// <summary>
        /// Power the controller on.  Do not use with SleepMode.
        /// </summary>
        public override void PowerOn()
        {
            SendCommand(Epd7In5b_V2Commands.PowerOn);
            DeviceWaitUntilReady();
        }

        /// <summary>
        /// Power the controller off.  Do not use with SleepMode.
        /// </summary>
        public override void PowerOff()
        {
            SendCommand(Epd7In5b_V2Commands.PowerOff);
            DeviceWaitUntilReady();
        }

        /// <summary>
        /// Send the Display into SleepMode
        /// </summary>
        public override void Sleep()
        {
            PowerOff();
            SendCommand(Epd7In5b_V2Commands.DeepSleep);
            SendData(0xA5);
        }

        /// <summary>
        /// Wait until the display is ready
        /// </summary>
        public void DeviceWaitUntilReady()
        {
            WaitUntilReady();
            Thread.Sleep(200);
        }

        #endregion Public Methods

        //########################################################################################

        #region Protected Methods

        /// <summary>
        /// Device specific Initializer
        /// </summary>
        protected override void DeviceInitialize()
        {
            Reset();

            SendCommand(Epd7In5b_V2Commands.BoosterSoftStart);
            SendData(0x17);
            SendData(0x17);
            SendData(0x27);
            SendData(0x17);

            SendCommand(Epd7In5b_V2Commands.PowerSetting);
            SendData(0x07); // VGH: 20V
            SendData(0x07); // VGL: -20V
            SendData(0x3f); // VDH: 15V
            SendData(0x3f); // VDL: -15V

            SendCommand(Epd7In5b_V2Commands.PowerOn);
            Thread.Sleep(100);
            DeviceWaitUntilReady();

            SendCommand(Epd7In5b_V2Commands.PanelSetting);
            SendData(0x0F); // KW-3f   KWR-2F	BWROTP 0f	BWOTP 1f

            SendCommand(Epd7In5b_V2Commands.TconResolution);
            SendData(0x03); // source 800
            SendData(0x20);
            SendData(0x01); // gate 480
            SendData(0xe0);

            SendCommand(Epd7In5b_V2Commands.DualSpi);
            SendData(0x00);

            SendCommand(Epd7In5b_V2Commands.VcomAndDataIntervalSetting);
            SendData(0x11);
            SendData(0x07);

            SendCommand(Epd7In5b_V2Commands.TconSetting);
            SendData(0x22);

            SendCommand(Epd7In5b_V2Commands.GateSourceStartSetting);  // Resolution setting
            SendData(0x00);
            SendData(0x00);//800*480
            SendData(0x00);
            SendData(0x00);
        }

        /// <summary>
        /// Turn the Display PowerOn after a Sleep
        /// </summary>
        protected override void TurnOnDisplay()
        {
            SendCommand(Epd7In5b_V2Commands.DisplayRefresh);
            Thread.Sleep(100);
            DeviceWaitUntilReady();
        }

        /// <summary>
        /// Convert a pixel to a DataByte
        /// </summary>
        /// <param name="r">Red color byte</param>
        /// <param name="g">Green color byte</param>
        /// <param name="b">Blue color byte</param>
        /// <returns>Pixel converted to specific byte value for the hardware</returns>
        protected override byte ColorToByte(byte r, byte g, byte b)
        {
            if (IsMonochrom(r, g, b) || IsRed(r, g, b))
            {
                return (byte)(byte.MinValue + r);
            }

            return (byte)(byte.MinValue + (r * 0.299 + g * 0.587 + b * 0.114));
        }

        #endregion Protected Methods

        //########################################################################################

        #region Internal Methods

        /// <summary>
        /// Send a Bitmap as Byte Array to the Device
        /// </summary>
        /// <param name="scanLine">Int Pointer to the start of the Bytearray</param>
        /// <param name="stride">Length of a ScanLine</param>
        /// <param name="maxX">Max Pixels horizontal</param>
        /// <param name="maxY">Max Pixels Vertical</param>
        internal override void SendBitmapToDevice(IntPtr scanLine, int stride, int maxX, int maxY)
        {
            var line = new byte[stride];

            var imageBw = new List<List<byte>>();
            var imageRed = new List<List<byte>>();

            var imageLineMaxX = maxX * ColorBytesPerPixel;

            for (var y = 0; y < Height; y++)
            {
                var bwLine = new List<byte>();
                var redLine = new List<byte>();
                
                if (y < maxY)
                {
                    Marshal.Copy(scanLine, line, 0, line.Length);

                    for (var x = 0; x < stride; x += ColorBytesPerPixel)
                    {
                        if (x < imageLineMaxX)
                        {
                            var color = GetByteColor(x, line);

                            if (IsRed(color[2], color[1], color[0]))
                            {
                                redLine.AddRange(color);
                                bwLine.AddRange(ByteColors.White);
                            }
                            else
                            {
                                bwLine.AddRange(color);
                                redLine.AddRange(ByteColors.Black);
                            }
                        }
                    }

                    scanLine += stride;
                }

                imageBw.Add(bwLine);
                imageRed.Add(redLine);
            }

            SendCommand(Epd7In5b_V2Commands.DataStartTransmission1);
            SendRawBitmapToDevice(imageBw, stride, maxX, maxY);

            SendCommand(Epd7In5b_V2Commands.DataStartTransmission2);
            SendRawBitmapToDevice(imageRed, stride, maxX, maxY, ByteColors.Black);
        }

        /// <summary>
        /// Send a Bitmap as Byte Array to the Device
        /// </summary>
        /// <param name="data">Int Pointer to the start of the Bytearray</param>
        /// <param name="stride">Length of a ScanLine</param>
        /// <param name="maxX">Max Pixels horizontal</param>
        /// <param name="maxY">Max Pixels Vertical</param>
        /// <param name="fillColor"></param>
        internal void SendRawBitmapToDevice(List<List<byte>> data, int stride, int maxX, int maxY, byte[] fillColor = null)
        {
            var deviceStep = ColorBytesPerPixel * PixelPerByte;
            var imageWidth = maxX * ColorBytesPerPixel;

            for (var y = 0; y < Height; y++)
            {
                var outputLine = fillColor == null ? CloneWhiteScanLine() : GetColoredLineOnDevice(fillColor);

                if (y < maxY)
                {
                    var line = new byte[stride];
                    data[y].CopyTo(line);

                    for (var x = 0; x < imageWidth; x += deviceStep)
                    {
                        outputLine[x / deviceStep] = GetDevicePixels(x, line);
                    }
                }

                SendData(outputLine);
            }
        }

        #endregion

        //########################################################################################

        #region Private Methods

        /// <summary>
        /// Helper to send a Command based o the Epd7In5b_V2Commands Enum
        /// </summary>
        /// <param name="command">Command to send</param>
        private void SendCommand(Epd7In5b_V2Commands command)
        {
            SendCommand((byte)command);
        }

        /// <summary>
        /// Fill the screen with a color
        /// </summary>
        /// <param name="command">Start Data Transmission Command</param>
        /// <param name="rgb">Color to fill the screen</param>
        private void FillColor(Epd7In5b_V2Commands command, byte[] rgb)
        {
            var outputLine = GetColoredLineOnDevice(rgb);

            SendCommand(command);

            for (var y = 0; y < Height; y++)
            {
                SendData(outputLine);
            }
        }

        /// <summary>
        /// Get Color as Bytes from the Line
        /// </summary>
        /// <param name="x"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        private byte[] GetByteColor(int x, byte[] line)
        {
            var indexR = x + 2;
            var indexG = x + 1;
            var indexB = x + 0;

            var color = new byte[ColorBytesPerPixel];
            color[0] = line[indexB];
            color[1] = line[indexG];
            color[2] = line[indexR];
            return color;
        }

        #endregion Private Methods

        //########################################################################################

    }
}