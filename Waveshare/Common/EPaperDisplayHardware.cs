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

using System.Device.Gpio;
using System.Device.Spi;

namespace Waveshare.Common;

/// <summary> E-Paper Display Hardware on the SPI Bus </summary>
internal sealed class EPaperDisplayHardware : IEPaperDisplayHardware
{
    #region Constants

    /// <summary> GPIO Reset Pin Number </summary>
    private const int GpioResetPin = 17;

    /// <summary> GPIO SPI DC Pin Number </summary>
    private const int GpioSpiDcPin = 25;

    /// <summary> GPIO SPI CS Pin Number </summary>
    private const int GpioSpiCsPin = 0;

    /// <summary> GPIO Busy Pin Number </summary>
    private const int GpioBusyPin = 24;

    /// <summary> GPIO Power Pin Number (Waveshare HAT 3.3V Power Enable) </summary>
    private const int GpioPwrPin = 18;

    #endregion Constants

    #region Properties

    /// <summary> SPI Bus Device </summary>
    internal SpiDevice? SpiDevice { get; private set; }

    /// <summary> GPIO Controller Device </summary>
    internal GpioController? GpioController { get; private set; }

    /// <summary> GPIO Reset Pin </summary>
    public PinValue ResetPin
    {
        get => GpioController?.Read(GpioResetPin) ?? -1;
        set => GpioController?.Write(GpioResetPin, value);
    }

    /// <summary> GPIO SPI DC Pin </summary>
    public PinValue SpiDcPin
    {
        get => GpioController?.Read(GpioSpiDcPin) ?? -1;
        set => GpioController?.Write(GpioSpiDcPin, value);
    }

    /// <summary> GPIO Busy Pin </summary>
    public PinValue BusyPin
    {
        get => GpioController?.Read(GpioBusyPin) ?? -1;
        set => GpioController?.Write(GpioBusyPin, value);
    }

    #endregion Properties

    #region Constructor / Dispose / Finalizer

    /// <summary> Default Constructor </summary>
    public EPaperDisplayHardware() : this(CreateSpiDevice(), CreateGpioController())
    {

    }

    /// <summary> Internal Constructor with Hardware Interfaces (SPI and GPIO) </summary>
    /// <param name="spiDevice"></param>
    /// <param name="gpioController"></param>
    internal EPaperDisplayHardware(SpiDevice spiDevice, GpioController? gpioController)
    {
        GpioController = gpioController;

        GpioController?.OpenPin(GpioResetPin);
        GpioController?.OpenPin(GpioSpiDcPin);
        GpioController?.OpenPin(GpioBusyPin);
        GpioController?.OpenPin(GpioPwrPin);

        GpioController?.SetPinMode(GpioResetPin, PinMode.Output);
        GpioController?.SetPinMode(GpioSpiDcPin, PinMode.Output);
        GpioController?.SetPinMode(GpioBusyPin, PinMode.Input);
        GpioController?.SetPinMode(GpioPwrPin, PinMode.Output);

        // Turn on Waveshare HAT 3.3V power switch (GPIO 18 HIGH) and allow rail to stabilize
        GpioController?.Write(GpioPwrPin, PinValue.High);
        GpioController?.Write(GpioResetPin, PinValue.High);
        GpioController?.Write(GpioSpiDcPin, PinValue.Low);
        Thread.Sleep(100);

        Console.WriteLine($"[EPaperDisplayHardware] Initialized GPIO (PWR=18:High, RST=17:High, DC=25:Low, BUSY=24:{BusyPin}) and SPI0.0 (2MHz).");

        SpiDevice = spiDevice;
    }

    /// <summary> Dispose the SPI and GPIO Devices </summary>
    private void Dispose(bool disposing)
    {
        if (!disposing) return;

        GpioController?.Write(GpioSpiDcPin, PinValue.Low);
        GpioController?.Write(GpioResetPin, PinValue.Low);
        GpioController?.Write(GpioPwrPin, PinValue.Low);

        GpioController?.Dispose();
        GpioController = null;

        SpiDevice?.Dispose();
        SpiDevice = null;
    }

    /// <summary> Dispose </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary> Finalizer </summary>
    ~EPaperDisplayHardware() => Dispose(false);

    #endregion Constructor / Dispose / Finalizer

    #region Public Methods

    /// <summary> Write stream to the SPI device </summary>
    /// <param name="stream">The stream that contains the data to be written to the SPI device</param>
    public void Write(MemoryStream stream)
    {
        byte[] buffer = new byte[Math.Min(4096, stream.Length)];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        while (bytesRead == buffer.Length)
        {
            SpiDevice?.Write(buffer);
            bytesRead = stream.Read(buffer, 0, buffer.Length);
        }

        if (bytesRead <= 0 || bytesRead >= buffer.Length) return;

        Array.Resize(ref buffer, bytesRead);
        SpiDevice?.Write(buffer);
    }

    /// <summary> Write data to the SPI device (chunked to 4096 bytes for Linux spidev compatibility) </summary>
    /// <param name="buffer">The buffer that contains the data to be written to the SPI device</param>
    public void Write(byte[] buffer)
    {
        if (SpiDevice is null) return;
        const int maxChunkSize = 4096;
        ReadOnlySpan<byte> span = buffer;
        for (int offset = 0; offset < span.Length; offset += maxChunkSize)
        {
            int length = Math.Min(maxChunkSize, span.Length - offset);
            SpiDevice.Write(span.Slice(offset, length));
        }
    }

    /// <summary> Write a byte to the SPI device </summary>
    /// <param name="value">The byte to be written to the SPI device</param>
    public void WriteByte(byte value) => SpiDevice?.WriteByte(value);

    #endregion Public Methods

    //########################################################################################

    #region Private Methods

    /// <summary>Create the GPIO Controller </summary>
    /// <returns></returns>
    private static GpioController CreateGpioController()
    {
        try
        {
            return new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EPaperDisplayHardware] Default GpioController failed ({ex.Message}); falling back to LibGpiodDriver(0) on /dev/gpiochip0.");
            return new(new System.Device.Gpio.Drivers.LibGpiodDriver(0));
        }
    }

    /// <summary> Create the SPI Device </summary>
    /// <returns></returns>
    private static SpiDevice CreateSpiDevice()
    {
        SpiConnectionSettings spiConnectionSettings = new(0, GpioSpiCsPin)
        {
            ClockFrequency=2000000,
            Mode = SpiMode.Mode0,
        };
        return SpiDevice.Create(spiConnectionSettings);
    }

    #endregion Private Methods
}
