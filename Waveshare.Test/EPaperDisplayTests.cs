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

using Waveshare.Devices;
using Waveshare.Interfaces;

namespace Waveshare.Test;

public class EPaperDisplayTests
{
    [Test]
    public void CreateNoneTest()
    {
        using IEPaperDisplaySKBitmap result = EPaperDisplay.CreateSKBitmap(EPaperDisplayType.None);
        Assert.That(result, Is.Null, $"Enum Value {EPaperDisplayType.None} should not return a object");
    }

    [Test]
    public void CreateWaveShare7In5BcTest()
    {
        Mock<IEPaperDisplayHardware> ePaperDisplayHardwareMock = new();
        ePaperDisplayHardwareMock.SetupGet(e => e.BusyPin).Returns(PinValue.High);
        EPaperDisplayRaw.EPaperDisplayHardware = new(() => ePaperDisplayHardwareMock.Object);

        using IEPaperDisplaySKBitmap result = EPaperDisplay.CreateSKBitmap(EPaperDisplayType.WaveShare7In5Bc);
        Assert.That(result, Is.Not.Null, $"Enum Value {EPaperDisplayType.WaveShare7In5Bc} should return a object");
    }

    [Test]
    public void CreateWaveShare7In5_V2Test()
    {
        Mock<IEPaperDisplayHardware> ePaperDisplayHardwareMock = new();
        ePaperDisplayHardwareMock.SetupGet(e => e.BusyPin).Returns(PinValue.High);
        EPaperDisplayRaw.EPaperDisplayHardware = new(() => ePaperDisplayHardwareMock.Object);

        using IEPaperDisplaySKBitmap result = EPaperDisplay.CreateSKBitmap(EPaperDisplayType.WaveShare7In5_V2);
        Assert.That(result, Is.Not.Null, $"Enum Value {EPaperDisplayType.WaveShare7In5_V2} should return a object");
    }

    [Test]
    public void CreateWaveShare7In5b_V2Test()
    {
        Mock<IEPaperDisplayHardware> ePaperDisplayHardwareMock = new();
        ePaperDisplayHardwareMock.SetupGet(e => e.BusyPin).Returns(PinValue.High);
        EPaperDisplayRaw.EPaperDisplayHardware = new(() => ePaperDisplayHardwareMock.Object);

        using IEPaperDisplaySKBitmap result = EPaperDisplay.CreateSKBitmap(EPaperDisplayType.WaveShare7In5b_V2);
        Assert.That(result, Is.Not.Null, $"Enum Value {EPaperDisplayType.WaveShare7In5b_V2} should return a object");
    }

    [Test]
    public void CreateWaveShareEpd5in65fTest()
    {
        Mock<IEPaperDisplayHardware> ePaperDisplayHardwareMock = new();
        ePaperDisplayHardwareMock.SetupGet(e => e.BusyPin).Returns(PinValue.High);
        EPaperDisplayRaw.EPaperDisplayHardware = new(() => ePaperDisplayHardwareMock.Object);

        using IEPaperDisplaySKBitmap result = EPaperDisplay.CreateSKBitmap(EPaperDisplayType.WaveShare5In65f);
        Assert.That(result, Is.Not.Null, $"Enum Value {EPaperDisplayType.WaveShare5In65f} should return a object");
    }

    [Test]
    public void GetEPaperDisplayHardwareTest()
    {
        Lazy<IEPaperDisplayHardware> lazyHardware = EPaperDisplayRaw.EPaperDisplayHardware;
        Assert.That(lazyHardware, Is.Not.Null);

        using IEPaperDisplayHardware result = lazyHardware.Value;
        Assert.That(result, Is.Not.Null, "EPaperDisplayHardware should not return null");

        Mock<IEPaperDisplayHardware> ePaperDisplayHardwareMock = new();
        EPaperDisplayRaw.EPaperDisplayHardware = new(() => ePaperDisplayHardwareMock.Object);

        using IEPaperDisplayHardware result2 = EPaperDisplayRaw.EPaperDisplayHardware.Value;
        Assert.That(result2, Is.Not.Null, "EPaperDisplayHardware should not return null");
    }
}
