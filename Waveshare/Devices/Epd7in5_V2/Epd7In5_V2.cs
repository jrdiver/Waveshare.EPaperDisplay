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
    private bool _invertPolarity = false;
    public override byte[] DeviceByteColors => _invertPolarity 
        ? [Epd7In5_V2Colors.Black, Epd7In5_V2Colors.White] 
        : [Epd7In5_V2Colors.White, Epd7In5_V2Colors.Black];

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
        DeviceInitialize();
        FillColor(Epd7In5_V2Commands.DataStartTransmission1, ByteColors.White);
        FillColor(Epd7In5_V2Commands.DataStartTransmission2, ByteColors.White);
        TurnOnDisplay();
        PowerOff();
    }

    /// <summary> Clear the Display to Black </summary>
    public override void ClearBlack()
    {
        DeviceInitialize();
        FillColor(Epd7In5_V2Commands.DataStartTransmission2, ByteColors.Black);
        TurnOnDisplay();
        PowerOff();
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
        SendCommand(Epd7In5_V2Commands.VcomAndDataIntervalSetting);
        SendData(0xF7);
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
    public override void DisplayImage(IRawImage rawImage, IRawImage previousRawImage, bool dithering, bool partialRefresh = false, int x = 0, int y = 0)
    {
        if (!partialRefresh)
        {
            DisplayImage(rawImage, dithering, false, x, y);
            return;
        }

        InitializePartial();

        SendCommand(Epd7In5_V2Commands.VcomAndDataIntervalSetting);
        SendData(0xA9);
        SendData(0x07);

        SendCommand(Epd7In5_V2Commands.PartialIn);
        SendCommand(Epd7In5_V2Commands.PartialWindow);

        x = (x / 8) * 8;
        int xEnd = x + rawImage.Width - 1;

        SendData((byte)(x / 256));
        SendData((byte)(x % 256));
        SendData((byte)(xEnd / 256));
        SendData((byte)(xEnd % 256));
        SendData((byte)(y / 256));
        SendData((byte)(y % 256));
        int yEnd = y + rawImage.Height - 1;
        SendData((byte)(yEnd / 256));
        SendData((byte)(yEnd % 256));
        SendData(0x01);

        // Write the previous state to the 'Old' buffer (0x10) to prevent desync ghosting
        WritePartialImageToDevice(previousRawImage, (byte)Epd7In5_V2Commands.DataStartTransmission1, dithering);
        
        // Write the new state to the 'New' buffer (0x13)
        WritePartialImageToDevice(rawImage, StartDataTransmissionCommand, dithering);

        if (StopDataTransmissionCommand < byte.MaxValue)
            SendCommand(StopDataTransmissionCommand);

        TurnOnDisplay();
        SendCommand(Epd7In5_V2Commands.PartialOut);
        PowerOff();
    }

    public override void DisplayImage(IRawImage rawImage, bool dithering, bool partialRefresh = false, int x = 0, int y = 0)
    {
        if (!partialRefresh)
        {
            // Reset and initialize hardware for full refresh to clear partial LUTs
            DeviceInitialize();

            // Seed the 'Old' image buffer (0x10) with inverted data.
            // This allows future Partial Updates to know the current screen state and avoid flashing.
            _invertPolarity = true;
            SendCommand(Epd7In5_V2Commands.DataStartTransmission1);
            if (dithering)
                SendDitheredBitmapToDevice(rawImage.ScanLine, rawImage.Stride, rawImage.Width, rawImage.Height);
            else
                SendBitmapToDevice(rawImage.ScanLine, rawImage.Stride, rawImage.Width, rawImage.Height);
            _invertPolarity = false;
            
            // Now send the normal image to the 'New' buffer (0x13) and refresh
            base.DisplayImage(rawImage, dithering, partialRefresh, x, y);
            PowerOff();
            return;
        }

        InitializePartial();

        SendCommand(Epd7In5_V2Commands.VcomAndDataIntervalSetting);
        SendData(0xA9);
        SendData(0x07);

        SendCommand(Epd7In5_V2Commands.PartialIn);
        SendCommand(Epd7In5_V2Commands.PartialWindow);

        x = (x / 8) * 8;
        int xEnd = x + rawImage.Width - 1;

        SendData((byte)(x / 256));
        SendData((byte)(x % 256));
        SendData((byte)(xEnd / 256));
        SendData((byte)(xEnd % 256));
        SendData((byte)(y / 256));
        SendData((byte)(y % 256));
        int yEnd = y + rawImage.Height - 1;
        SendData((byte)(yEnd / 256));
        SendData((byte)(yEnd % 256));
        SendData(0x01);

        WritePartialImageToDevice(rawImage, StartDataTransmissionCommand, dithering);

        if (StopDataTransmissionCommand < byte.MaxValue)
            SendCommand(StopDataTransmissionCommand);

        TurnOnDisplay();
        SendCommand(Epd7In5_V2Commands.PartialOut);
        PowerOff();
    }

    private void WritePartialImageToDevice(IRawImage rawImage, byte command, bool dithering)
    {
        SendCommand(command);
        
        // The Partial Update LUT on this hardware requires inverted polarity (White=1, Black=0)
        _invertPolarity = true;
        
        if (dithering)
        {
            SendDitheredBitmapToDevice(rawImage.ScanLine, rawImage.Stride, rawImage.Width, rawImage.Height, padToFull: false);
        }
        else
        {
            SendBitmapToDevice(rawImage.ScanLine, rawImage.Stride, rawImage.Width, rawImage.Height, padToFull: false);
        }

        // The controller expects exactly 48000 bytes. Pad the remaining data.
        int totalPixels = Width * Height;
        int pixelsWritten = rawImage.Width * rawImage.Height;
        for (int p = pixelsWritten; p < totalPixels; p++)
            DisplayWriter.WriteBlankPixel();
            
        DisplayWriter.Finish();
        
        _invertPolarity = false;
    }

    public void InitializePartial()
    {
        // We MUST call Reset() here. Toggling the hardware reset pin resets the controller's logic state machine 
        // so it will accept the new LUT configuration (0xE5). Without it, it ignores the partial LUT and flashes!
        // (SRAM containing the old image is preserved during a soft logic reset).
        Reset();
        SendCommand(Epd7In5_V2Commands.PanelSetting);
        SendData(0x1F);
        SendCommand(Epd7In5_V2Commands.PowerOn);
        Thread.Sleep(100);
        DeviceWaitUntilReady();
        SendCommand(Epd7In5_V2Commands.CascadeSetting);
        SendData(0x02);
        SendCommand(Epd7In5_V2Commands.ForceTemperature);
        SendData(0x6E);
    }

    // 42-byte custom 4-Gray waveform LUTs for UC8179 (works on pre-Oct 2023 panels via PanelSetting 0x3F)
    private static readonly byte[] Lut4GrayVcom =
    [
        0x00, 0x0F, 0x0F, 0x00, 0x00, 0x01,
        0x00, 0x0F, 0x01, 0x0F, 0x01, 0x02,
        0x00, 0x0F, 0x0F, 0x00, 0x00, 0x01,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    ];

    // (0,0) -> Pure White (15 frames VDL White in final phase)
    private static readonly byte[] Lut4GrayWW =
    [
        0x10, 0x0F, 0x0F, 0x00, 0x00, 0x01,
        0x84, 0x0F, 0x01, 0x0F, 0x01, 0x02,
        0x20, 0x0F, 0x0F, 0x00, 0x00, 0x01,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    ];

    // (1,0) -> Light Gray (11 frames VDL White + 4 frames VDH Black in final phase)
    private static readonly byte[] Lut4GrayBW =
    [
        0x10, 0x0F, 0x0F, 0x00, 0x00, 0x01,
        0x84, 0x0F, 0x01, 0x0F, 0x01, 0x02,
        0x24, 0x0F, 0x0B, 0x04, 0x00, 0x01,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    ];

    // (0,1) -> Dark Gray (6 frames VDL White + 9 frames VDH Black in final phase)
    private static readonly byte[] Lut4GrayWB =
    [
        0x80, 0x0F, 0x0F, 0x00, 0x00, 0x01,
        0x84, 0x0F, 0x01, 0x0F, 0x01, 0x02,
        0x24, 0x0F, 0x06, 0x09, 0x00, 0x01,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    ];

    // (1,1) -> Pure Black (15 frames VDH Black in final phase)
    private static readonly byte[] Lut4GrayBB =
    [
        0x80, 0x0F, 0x0F, 0x00, 0x00, 0x01,
        0x84, 0x0F, 0x01, 0x0F, 0x01, 0x02,
        0x40, 0x0F, 0x0F, 0x00, 0x00, 0x01,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    ];

    public void Initialize4Gray()
    {
        Reset();

        SendCommand(Epd7In5_V2Commands.PowerSetting);
        SendData(0x17); // Internal power
        SendData(0x17); // VGH=20V, VGL=-20V
        SendData(0x3F); // VSH=15V
        SendData(0x3F); // VSL=-15V
        SendData(0x11); // VSHR

        SendCommand(Epd7In5_V2Commands.VcmDcSetting);
        SendData(0x24);

        SendCommand(Epd7In5_V2Commands.BoosterSoftStart);
        SendData(0x27);
        SendData(0x27);
        SendData(0x2F);
        SendData(0x17);

        SendCommand(Epd7In5_V2Commands.PllControl);
        SendData(0x06);

        SendCommand(Epd7In5_V2Commands.PowerOn);
        Thread.Sleep(100);
        DeviceWaitUntilReady();

        // 0x3F enables custom LUT registers (0x20-0x24) instead of factory OTP ROM
        SendCommand(Epd7In5_V2Commands.PanelSetting);
        SendData(0x3F);

        SendCommand(Epd7In5_V2Commands.TconResolution);
        SendData(0x03); // 800
        SendData(0x20);
        SendData(0x01); // 480
        SendData(0xE0);

        SendCommand(Epd7In5_V2Commands.DualSpi);
        SendData(0x00);

        SendCommand(Epd7In5_V2Commands.VcomAndDataIntervalSetting);
        SendData(0x10);
        SendData(0x00);

        SendCommand(Epd7In5_V2Commands.TconSetting);
        SendData(0x22);

        SendCommand(Epd7In5_V2Commands.GateSourceStartSetting);
        SendData(0x00);
        SendData(0x00);
        SendData(0x00);
        SendData(0x00);

        SendCommand(Epd7In5_V2Commands.LutVcom);
        SendData(Lut4GrayVcom);
        SendCommand(Epd7In5_V2Commands.LutWhiteToWhite);
        SendData(Lut4GrayWW);
        SendCommand(Epd7In5_V2Commands.LutBlackToWhite);
        SendData(Lut4GrayBW);
        SendCommand(Epd7In5_V2Commands.LutWhiteToBlack);
        SendData(Lut4GrayWB);
        SendCommand(Epd7In5_V2Commands.LutBlackToBlack);
        SendData(Lut4GrayBB);
    }

    public override void DisplayImage4Gray(IRawImage rawImage)
    {
        Initialize4Gray();

        int bytesPerRow = Width / 8; // 100 bytes per row
        int totalBytes = bytesPerRow * Height; // 48,000 bytes
        byte[] plane10 = new byte[totalBytes];
        byte[] plane13 = new byte[totalBytes];

        byte[] rowBuffer = new byte[rawImage.Stride];
        int maxY = Math.Min(Height, rawImage.Height);
        int maxX = Math.Min(Width, rawImage.Width);
        int bpp = ColorBytesPerPixel;

        for (int y = 0; y < maxY; y++)
        {
            IntPtr rowPtr = rawImage.ScanLine + (y * rawImage.Stride);
            Marshal.Copy(rowPtr, rowBuffer, 0, rawImage.Stride);

            int rowByteOffset = y * bytesPerRow;
            for (int x = 0; x < maxX; x++)
            {
                byte lum = rowBuffer[x * bpp]; // Pre-quantized grayscale (B == G == R)
                int byteIdx = rowByteOffset + (x >> 3);
                int bitMask = 0x80 >> (x & 7);

                // Bitplane encoding matching UC8179 4-gray LUTs:
                // White (>= 213)      -> (0, 0)
                // Light Gray (>= 128) -> (1, 0)
                // Dark Gray (>= 43)   -> (0, 1)
                // Black (< 43)        -> (1, 1)
                if (lum < 213)
                {
                    if (lum >= 128)
                    {
                        plane10[byteIdx] |= (byte)bitMask;
                    }
                    else if (lum >= 43)
                    {
                        plane13[byteIdx] |= (byte)bitMask;
                    }
                    else
                    {
                        plane10[byteIdx] |= (byte)bitMask;
                        plane13[byteIdx] |= (byte)bitMask;
                    }
                }
            }
        }

        SendCommand(Epd7In5_V2Commands.DataStartTransmission1);
        SendData(plane10);

        SendCommand(Epd7In5_V2Commands.DataStartTransmission2);
        SendData(plane13);

        TurnOnDisplay();

        // Seed 0x10 ('Old' buffer) with ~plane13 so subsequent 1-bit partial clock updates
        // see matching old/new state outside the partial window and don't disturb 4-gray pixels.
        for (int i = 0; i < totalBytes; i++)
            plane10[i] = (byte)~plane13[i];

        SendCommand(Epd7In5_V2Commands.DataStartTransmission1);
        SendData(plane10);

        PowerOff();
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


