﻿#region Copyright
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

using System.Threading;
using Waveshare.Common;

#endregion Usings

namespace Waveshare.Devices.Epd7in5bc
{
    /// <summary>
    /// Type: Waveshare 7.5inch e-Paper (B)
    /// Color: Black, White and Red
    /// Display Resolution: 640*384
    /// </summary>
    internal sealed class Epd7In5Bc : EPaperDisplayBase
    {

        //########################################################################################

        #region Properties

        /// <summary>
        /// Pixels per Byte on the Device
        /// </summary>
        public override int PixelPerByte { get; } = 2;

        /// <summary>
        /// Pixel Width of the Display
        /// </summary>
        public override int Width { get; } = 640;

        /// <summary>
        /// Pixel Height of the Display
        /// </summary>
        public override int Height { get; } = 384;

        /// <summary>
        /// Supported Colors of the E-Paper Device
        /// </summary>
        public override ByteColor[] SupportedByteColors { get; } = {ByteColors.Black, ByteColors.Gray, ByteColors.White, ByteColors.Red };

        /// <summary>
        /// Color Bytes of the E-Paper Device corresponding to the supported colors
        /// </summary>
        public override byte[] DeviceByteColors { get; } = { Epd7in5bcColors.Black, Epd7in5bcColors.Gray, Epd7in5bcColors.White, Epd7in5bcColors.Red };

        /// <summary>
        /// Get Status Command
        /// </summary>
        protected override byte GetStatusCommand { get; } = (byte)Epd7In5BcCommands.GetStatus;

        /// <summary>
        /// Start Data Transmission Command
        /// </summary>
        protected override byte StartDataTransmissionCommand { get; } = (byte)Epd7In5BcCommands.DataStartTransmission1;

        /// <summary>
        /// Stop Data Transmission Command
        /// </summary>
        protected override byte StopDataTransmissionCommand { get; } = (byte)Epd7In5BcCommands.DataStop;

        /// <summary>
        /// Display DeepSleep Command
        /// </summary>
        protected override byte DeepSleepComand { get; } = (byte)Epd7In5BcCommands.DeepSleep;

        #endregion Properties

        //########################################################################################

        #region Public Methods

        /// <summary>
        /// Clear the Display to White
        /// </summary>
        public override void Clear()
        {
            FillColor(ByteColors.White);
            TurnOnDisplay();
        }

        /// <summary>
        /// Clear the Display to Black
        /// </summary>
        public override void ClearBlack()
        {
            FillColor(ByteColors.Black);
            TurnOnDisplay();
        }

        /// <summary>
        /// Power the controller on.  Do not use with SleepMode.
        /// </summary>
        public override void PowerOn()
        {
            SendCommand(Epd7In5BcCommands.PowerOn);
            WaitUntilReady();
        }

        /// <summary>
        /// Power the controller off.  Do not use with SleepMode.
        /// </summary>
        public override void PowerOff()
        {
            SendCommand(Epd7In5BcCommands.PowerOff);
            WaitUntilReady();
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

            SendCommand(Epd7In5BcCommands.PowerSetting); // POWER_SETTING
            SendData(0x37);
            SendData(0x00);

            SendCommand(Epd7In5BcCommands.PanelSetting);
            SendData(0xCF);
            SendData(0x08);

            SendCommand(Epd7In5BcCommands.PllControl);
            SendData(0x3A); // PLL:  0-15:0x3C, 15+:0x3A

            SendCommand(Epd7In5BcCommands.VcmDcSetting);
            SendData(0x28); //all temperature  range

            SendCommand(Epd7In5BcCommands.BoosterSoftStart);
            SendData(0xc7);
            SendData(0xcc);
            SendData(0x15);

            SendCommand(Epd7In5BcCommands.VcomAndDataIntervalSetting);
            SendData(0x77);

            SendCommand(Epd7In5BcCommands.TconSetting);
            SendData(0x22);

            SendCommand(Epd7In5BcCommands.SpiFlashControl);
            SendData(0x00);

            SendCommand(Epd7In5BcCommands.TconResolution);
            SendData((byte)(Width >> 8)); // source 640
            SendData((byte)(Width & 0xff));
            SendData((byte)(Height >> 8)); // gate 384
            SendData((byte)(Height & 0xff));

            SendCommand(Epd7In5BcCommands.FlashMode);
            SendData(0x03);
        }

        /// <summary>
        /// Turn the Display PowerOn after a Sleep
        /// </summary>
        protected override void TurnOnDisplay()
        {
            SendCommand(Epd7In5BcCommands.PowerOn);
            WaitUntilReady();
            SendCommand(Epd7In5BcCommands.DisplayRefresh);
            Thread.Sleep(100);
            WaitUntilReady();
        }

        /// <summary>
        /// Convert a pixel to a DataByte
        /// </summary>
        /// <param name="rgb">color bytes</param>
        /// <returns>Pixel converted to specific byte value for the hardware</returns>
        protected override byte ColorToByte(ByteColor rgb)
        {
            if (rgb.IsMonochrome)
            {
                if (rgb.R <= 85)
                {
                    return Epd7in5bcColors.Black;
                }

                if (rgb.R <= 170)
                {
                    return Epd7in5bcColors.Gray;
                }

                return Epd7in5bcColors.White;
            }

            return rgb.R >= 64 ? Epd7in5bcColors.Red : Epd7in5bcColors.Black;
        }

        #endregion Protected Methods

        //########################################################################################

        #region Private Methods

        /// <summary>
        /// Helper to send a Command based o the Epd7In5BcCommands Enum
        /// </summary>
        /// <param name="command">Command to send</param>
        private void SendCommand(Epd7In5BcCommands command)
        {
            SendCommand((byte)command);
        }

        /// <summary>
        /// Fill the screen with a color
        /// </summary>
        /// <param name="rgb">Color to fill the screen</param>
        private void FillColor(ByteColor rgb)
        {
            byte[] outputLine = GetColoredLineOnDevice(rgb);

            SendCommand(StartDataTransmissionCommand);

            for (int y = 0; y < Height; y++)
            {
                SendData(outputLine);
            }

            SendCommand(StopDataTransmissionCommand);
        }

        #endregion Private Methods

        //########################################################################################

    }
}
