#region Copyright
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


// ReSharper disable InconsistentNaming

using System.Runtime.InteropServices;
using Waveshare.Devices.Epd7in5b_V2;

namespace Waveshare.Devices.Epd7in5_V2;

/// <summary>
/// Type: Waveshare 7.5inch e-Paper V2 <br/>
/// Color: Black and White <br/>
/// Display Resolution: 800*480
/// </summary>
internal sealed class Epd7In5_V2 : EPaperDisplayBase
{
    #region Properties

    /// <summary> Pixels per Byte on the Device </summary>
    public override int PixelPerByte => 8;

    /// <summary> Pixel Width of the Display </summary>
    public override int Width => 800;

    /// <summary> Pixel Height of the Display </summary>
    public override int Height => 480;

    /// <summary> Supported Colors of the E-Paper Device </summary>
    public override ByteColor[] SupportedByteColors { get; } = [ByteColors.White, ByteColors.Black];

    /// <summary> Color Bytes of the E-Paper Device corresponding to the supported colors </summary>
    public override byte[] DeviceByteColors { get; } = [Epd7In5_V2Colors.White, Epd7In5_V2Colors.Black];

    /// <summary> Get Status Command </summary>
    protected override byte GetStatusCommand => (byte)Epd7In5_V2Commands.GetStatus;

    /// <summary> Start Data Transmission Command </summary>
    protected override byte StartDataTransmissionCommand => (byte)Epd7In5_V2Commands.DataStartTransmission2;

    /// <summary> Stop Data Transmission Command </summary>
    protected override byte StopDataTransmissionCommand => byte.MaxValue;

    /// <summary> Display DeepSleep Command </summary>
    protected override byte DeepSleepCommand => (byte)Epd7In5_V2Commands.DeepSleep;

    #endregion Properties

    #region Public Methods

    /// <summary> Clear the Display to White </summary>
    public override void Clear()
    {
        FillColor(Epd7In5_V2Commands.DataStartTransmission1, ByteColors.White);
        FillColor(Epd7In5_V2Commands.DataStartTransmission2, ByteColors.White);
        TurnOnDisplay();
    }

    /// <summary> Clear the Display to Black </summary>
    public override void ClearBlack()
    {
        FillColor(Epd7In5_V2Commands.DataStartTransmission2, ByteColors.Black);
        TurnOnDisplay();
    }

    /// <summary> Power the controller on.  Do not use with SleepMode. </summary>
    public override void PowerOn()
    {
        SendCommand(Epd7In5_V2Commands.PowerOn);
        DeviceWaitUntilReady();
    }

    /// <summary>
    /// Power the controller off. <br/>
    /// Do not use with SleepMode.
    /// </summary>
    public override void PowerOff()
    {
        SendCommand(Epd7In5_V2Commands.PowerOff);
        DeviceWaitUntilReady();
    }

    /// <summary> Wait until the display is ready </summary>
    public void DeviceWaitUntilReady()
    {
        WaitUntilReady();
        Thread.Sleep(200);
    }

    /// <summary> Display an Image on the Display </summary>
    /// <param name="rawImage">Bitmap that should be displayed</param>
    /// <param name="dithering">Use Dithering to display the image</param>
    /// <param name="partialRefresh">Enable Refreshing only part of the display.</param>
    /// <param name="x">Leftmost coordinate for partial refresh</param> 
    /// <param name="y">Uppermost coordinate for partial refresh </param>
    public override void DisplayImage(IRawImage rawImage, bool dithering, bool partialRefresh = false, int x = 0, int y = 0)
    {
        if (!partialRefresh)
        {
            base.DisplayImage(rawImage, dithering, partialRefresh, x, y);
            return;
        }
        InitializePartial();

        //Based off the Python Example code from Waveshare
        //Partial Refresh
        SendCommand(Epd7In5_V2Commands.VcomAndDataIntervalSetting);
        SendData(0xA9);
        SendData(0x07);

        //Go into Partial Mode
        SendCommand(Epd7In5_V2Commands.PartialIn);
        SendCommand(Epd7In5_V2Commands.PartialWindow);

        //X Start
        SendData((byte)(x / 256));
        SendData((byte)(x % 256));

        //X End
        int xEnd = x + rawImage.Width - 1;
        SendData((byte)(xEnd / 256));
        SendData((byte)(xEnd % 256));

        //Y Start
        SendData((byte)(y / 256));
        SendData((byte)(y % 256));

        //Y End
        int yEnd = y + rawImage.Height - 1;
        SendData((byte)(yEnd / 256));
        SendData((byte)(yEnd % 256));
        SendData(0x01);

        SendCommand(StartDataTransmissionCommand);
        if (dithering)
            SendDitheredBitmapToDevice(rawImage.ScanLine, rawImage.Stride, rawImage.Width, rawImage.Height);
        else
        {
            int stride = rawImage.Stride;
            IntPtr scanLine = rawImage.ScanLine;
            Console.WriteLine("Displaying Image without Dithering in Partial Refresh Mode");


            byte[] inputLine = new byte[stride];
            ByteColor pixel = new(0, 0, 0);

            for (int y1 = y; y1 < yEnd; y1++, scanLine += stride)
            {
                Marshal.Copy(scanLine, inputLine, 0, inputLine.Length);

                int xPos = 0;
                for (int x1 = x; x1 < xEnd; x1++)
                {
                    pixel.SetBGR(inputLine[xPos++], inputLine[xPos++], inputLine[xPos++], IsPalletMonochrome);
                    if (ColorBytesPerPixel > 3)
                        xPos += ColorBytesPerPixel - 3;
                    DisplayWriter.Write(GetColorIndex(pixel));
                }

                for (int x1 = xEnd; x1 < Width; x1++)
                    DisplayWriter.WriteBlankPixel();
            }

            // Write blank lines if image is smaller than display.
            for (int y1 = yEnd; y1 < Height; y1++)
                DisplayWriter.WriteBlankLine();

            DisplayWriter.Finish();
        }

        if (StopDataTransmissionCommand < byte.MaxValue)
            SendCommand(StopDataTransmissionCommand);

        TurnOnDisplay();
    }

    public void InitializePartial()
    {
        Reset();
        SendCommand(Epd7In5_V2Commands.PanelSetting);
        SendData(0x1F);
        SendCommand(Epd7In5_V2Commands.PowerOn);
        SendData(0x1F);
        Thread.Sleep(100);
        DeviceWaitUntilReady();
        SendCommand(Epd7In5_V2Commands.CascadeSetting);
        SendData(0x02);
        SendCommand(Epd7In5_V2Commands.ForceTemperature);
        SendData(0x6E);
    }

    #endregion Public Methods

    #region Protected Methods

    /// <summary> Device specific Initializer </summary>
    protected override void DeviceInitialize()
    {
        Reset();

        SendCommand(Epd7In5_V2Commands.BoosterSoftStart);
        SendData(0x17);
        SendData(0x17);
        SendData(0x27);
        SendData(0x17);

        SendCommand(Epd7In5_V2Commands.PowerSetting);
        SendData(0x07); // VGH: 20V
        SendData(0x17); // VGL: -20V
        SendData(0x3f); // VDH: 15V
        SendData(0x3f); // VDL: -15V

        SendCommand(Epd7In5_V2Commands.PowerOn);
        Thread.Sleep(100);
        DeviceWaitUntilReady();

        SendCommand(Epd7In5_V2Commands.PanelSetting);
        SendData(0x1F); // KW-3f   KWR-2F	BWROTP 0f	BWOTP 1f

        SendCommand(Epd7In5_V2Commands.TconResolution);
        SendData(0x03); // source 800
        SendData(0x20);
        SendData(0x01); // gate 480
        SendData(0xe0);

        SendCommand(Epd7In5_V2Commands.DualSpi);
        SendData(0x00);

        SendCommand(Epd7In5_V2Commands.VcomAndDataIntervalSetting);
        SendData(0x10);
        SendData(0x07);

        SendCommand(Epd7In5_V2Commands.TconSetting);
        SendData(0x22);
    }

    /// <summary> Turn the Display PowerOn after a Sleep </summary>
    protected override void TurnOnDisplay()
    {
        SendCommand(Epd7In5_V2Commands.DisplayRefresh);
        Thread.Sleep(100);
        DeviceWaitUntilReady();
    }

    /// <summary> Convert a pixel to a DataByte </summary>
    /// <param name="rgb">color bytes</param>
    /// <returns>Pixel converted to specific byte value for the hardware</returns>
    protected override byte ColorToByte(ByteColor rgb) => rgb.R < 128 ? Epd7In5_V2Colors.Black : Epd7In5_V2Colors.White;

    #endregion Protected Methods

    #region Private Methods

    /// <summary> Helper to send a Command based o the Epd7In5_V2Commands </summary>
    /// <param name="command">Command to send</param>
    private void SendCommand(Epd7In5_V2Commands command) => SendCommand((byte)command);

    /// <summary> Fill the scr </summary>
    /// <param name="command">Start Data Transmission Command</param>
    /// <param name="rgb">Color to fill the screen</param>
    private void FillColor(Epd7In5_V2Commands command, ByteColor rgb)
    {
        byte[] outputLine = GetColoredLineOnDevice(rgb);

        SendCommand(command);

        for (int y = 0; y < Height; y++)
            SendData(outputLine);
    }

    #endregion Private Methods
}
