#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// MIT License
// Copyright(c) 2022 Andre Wehrli

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

using Moq;
using NUnit.Framework;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.IO;
using System.Linq;
using Waveshare.Common;
using Waveshare.Devices.Epd5in65F;
using Waveshare.Image;
using Waveshare.Interfaces.Internal;

#endregion Usings

namespace Waveshare.Test.Devices.Epd5in65F
{
    /// <summary>
    /// All UnitTests for the Epd5in65f Device
    /// </summary>
    public class Epd5in65fTests
    {

        //########################################################################################

        #region Fields

        private List<byte> m_DataBuffer;
        private Mock<IEPaperDisplayHardware> m_EPaperDisplayHardwareMock;

        #endregion Fields

        //########################################################################################

        #region Setup

        [SetUp]
        public void Setup()
        {
            m_DataBuffer = new List<byte>();

            m_EPaperDisplayHardwareMock = new Mock<IEPaperDisplayHardware>();
            m_EPaperDisplayHardwareMock.Setup(e => e.BusyPin).Returns(PinValue.High);
            m_EPaperDisplayHardwareMock.Setup(e => e.Write(It.IsAny<byte[]>())).Callback((byte[] b) => m_DataBuffer.AddRange(b));
            m_EPaperDisplayHardwareMock.Setup(e => e.WriteByte(It.IsAny<byte>())).Callback((byte b) => m_DataBuffer.Add(b));
            m_EPaperDisplayHardwareMock.Setup(e => e.Write(It.IsAny<MemoryStream>())).Callback((MemoryStream b) => m_DataBuffer.AddRange(b.ToArray()));
        }

        #endregion Setup

        //########################################################################################

        #region Tests

        [Test]
        public void ConstructorTest()
        {
            using Epd5in65f result = new Epd5in65f();
            result.Initialize(m_EPaperDisplayHardwareMock.Object);
        }

        [Test]
        public void DisposeNoHardwareTest()
        {
            using Epd5in65f result = new Epd5in65f();
            result.Initialize(m_EPaperDisplayHardwareMock.Object);
        }

        [Test]
        public void DoubleDisposeTest()
        {
            Epd5in65f result = new Epd5in65f();
            result.Initialize(m_EPaperDisplayHardwareMock.Object);
            result.Dispose();
            result.Dispose();
        }

        [Test]
        public void FinalizerTest()
        {
            Epd5in65f result = new Epd5in65f();
            result.Initialize(m_EPaperDisplayHardwareMock.Object);

            Assert.That(result, Is.Not.Null, "Object should not be null");

            // ReSharper disable once RedundantAssignment
#pragma warning disable IDE0059 // Unnecessary assignment of a value
            result = null;
#pragma warning restore IDE0059 // Unnecessary assignment of a value

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        [Test]
        public void FinalizerNoHardwareTest()
        {
            Epd5in65f result = new Epd5in65f();

            Assert.That(result, Is.Not.Null, "Object should not be null");

            // ReSharper disable once RedundantAssignment
#pragma warning disable IDE0059 // Unnecessary assignment of a value
            result = null;
#pragma warning restore IDE0059 // Unnecessary assignment of a value

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        [Test]
        public void PowerOnTest()
        {
            using Epd5in65f result = new Epd5in65f();
            result.Initialize(m_EPaperDisplayHardwareMock.Object);

            m_DataBuffer.Clear();

            result.PowerOn();

            List<byte> validBuffer = new List<byte>
            {
                (byte)Epd5in65fCommands.PowerOn,
                (byte)Epd5in65fCommands.GetStatus
            };
            Assert.That(m_DataBuffer, Is.EqualTo(validBuffer), "Command Data Sequence is wrong");
        }


        [Test]
        public void PowerOffTest()
        {
            using Epd5in65f result = new Epd5in65f();
            result.Initialize(m_EPaperDisplayHardwareMock.Object);

            m_DataBuffer.Clear();

            result.PowerOff();

            List<byte> validBuffer = new List<byte>
            {
                (byte)Epd5in65fCommands.PowerOff,
                (byte)Epd5in65fCommands.GetStatus
            };
            Assert.That(m_DataBuffer, Is.EqualTo(validBuffer), "Command Data Sequence is wrong");
        }

        [Test]
        public void SleepTest()
        {
            using Epd5in65f result = new Epd5in65f();
            result.Initialize(m_EPaperDisplayHardwareMock.Object);

            m_DataBuffer.Clear();

            result.Sleep();

            List<byte> validBuffer = new List<byte>
            {
                (byte)Epd5in65fCommands.PowerOff,
                (byte)Epd5in65fCommands.GetStatus,
                (byte)Epd5in65fCommands.DeepSleep,
                0xA5
            };
            Assert.That(m_DataBuffer, Is.EqualTo(validBuffer), "Command Data Sequence is wrong");
        }

        [Test]
        public void ClearTest()
        {
            using Epd5in65f result = new Epd5in65f();
            result.Initialize(m_EPaperDisplayHardwareMock.Object);

            m_DataBuffer.Clear();

            result.Clear();

            const int pixelPerByte = 2;
            int displayBytes = result.Width / pixelPerByte * result.Height;

            const byte white = Epd5in65fColors.White;
            byte twoWhitePixel = result.MergePixelDataInByte(white, white);

            List<byte> validBuffer = new List<byte>
            {
                (byte) Epd5in65fCommands.DataStartTransmission1
            };

            for (int i = 0; i < displayBytes; i++)
            {
                validBuffer.Add(twoWhitePixel);
            }
            validBuffer.Add((byte)Epd5in65fCommands.DataStop);
            validBuffer.Add((byte)Epd5in65fCommands.PowerOn);
            validBuffer.Add((byte)Epd5in65fCommands.GetStatus);
            validBuffer.Add((byte)Epd5in65fCommands.DisplayRefresh);
            validBuffer.Add((byte)Epd5in65fCommands.GetStatus);

            Assert.That(m_DataBuffer, Is.EqualTo(validBuffer), "Command Data Sequence is wrong");
        }

        [Test]
        public void ClearBlackTest()
        {
            using Epd5in65f result = new Epd5in65f();
            result.Initialize(m_EPaperDisplayHardwareMock.Object);

            m_DataBuffer.Clear();

            result.ClearBlack();

            const int pixelPerByte = 2;
            int displayBytes = result.Width / pixelPerByte * result.Height;

            const byte black = Epd5in65fColors.Black;
            byte twoBlackPixel = result.MergePixelDataInByte(black, black);

            List<byte> validBuffer = new List<byte>
            {
                (byte) Epd5in65fCommands.DataStartTransmission1
            };

            for (int i = 0; i < displayBytes; i++)
            {
                validBuffer.Add(twoBlackPixel);
            }
            validBuffer.Add((byte)Epd5in65fCommands.DataStop);
            validBuffer.Add((byte)Epd5in65fCommands.PowerOn);
            validBuffer.Add((byte)Epd5in65fCommands.GetStatus);
            validBuffer.Add((byte)Epd5in65fCommands.DisplayRefresh);
            validBuffer.Add((byte)Epd5in65fCommands.GetStatus);

            Assert.That(m_DataBuffer, Is.EqualTo(validBuffer), "Command Data Sequence is wrong");
        }

        [Test]
        public void DisplayImageTest()
        {
            using Epd5in65f result = new Epd5in65f();
            result.Initialize(m_EPaperDisplayHardwareMock.Object);

            SKBitmap image = CommonTestData.CreateSampleBitmap(result.Width, result.Height);

            List<byte> validBuffer = new List<byte>
            {
                (byte) Epd5in65fCommands.DataStartTransmission1
            };

            m_DataBuffer.Clear();

            validBuffer.AddRange(BitmapToData(image, result.Width, result.Height));
            validBuffer.Add((byte)Epd5in65fCommands.DataStop);
            validBuffer.Add((byte)Epd5in65fCommands.PowerOn);
            validBuffer.Add((byte)Epd5in65fCommands.GetStatus);
            validBuffer.Add((byte)Epd5in65fCommands.DisplayRefresh);
            validBuffer.Add((byte)Epd5in65fCommands.GetStatus);

            m_DataBuffer.Clear();

            SKBitmapLoader bitmapEPaperDisplay = new SKBitmapLoader(result);
            bitmapEPaperDisplay.DisplayImage(image, false);

            Assert.That(m_DataBuffer.Count, Is.EqualTo(validBuffer.Count), "Data Length is wrong");
            Assert.That(m_DataBuffer, Is.EqualTo(validBuffer), "Command Data Sequence is wrong");
        }

        [Test]
        public void DisplayImageSmallTest()
        {
            using Epd5in65f result = new Epd5in65f();
            result.Initialize(m_EPaperDisplayHardwareMock.Object);

            SKBitmap image = CommonTestData.CreateSampleBitmap(result.Width / 2, result.Height / 2);

            List<byte> validBuffer = new List<byte>
            {
                (byte) Epd5in65fCommands.DataStartTransmission1
            };

            m_DataBuffer.Clear();

            validBuffer.AddRange(BitmapToData(image, result.Width, result.Height));
            validBuffer.Add((byte)Epd5in65fCommands.DataStop);
            validBuffer.Add((byte)Epd5in65fCommands.PowerOn);
            validBuffer.Add((byte)Epd5in65fCommands.GetStatus);
            validBuffer.Add((byte)Epd5in65fCommands.DisplayRefresh);
            validBuffer.Add((byte)Epd5in65fCommands.GetStatus);

            m_DataBuffer.Clear();

            SKBitmapLoader bitmapEPaperDisplay = new SKBitmapLoader(result);
            bitmapEPaperDisplay.DisplayImage(image);

            Assert.That(m_DataBuffer.Count, Is.EqualTo(validBuffer.Count), "Data Length is wrong");
            Assert.That(m_DataBuffer, Is.EqualTo(validBuffer), "Command Data Sequence is wrong");
        }

        [Test]
        public void WakeUpTest()
        {
            using Epd5in65f result = new Epd5in65f();
            result.Initialize(m_EPaperDisplayHardwareMock.Object);

            m_DataBuffer.Clear();

            result.WakeUp();

            List<byte> validBuffer = new List<byte>
            {
                (byte)Epd5in65fCommands.PanelSetting,
                0xEF,
                0x08,
                (byte)Epd5in65fCommands.PowerSetting,
                0x37,
                0x00,
                0x23,
                0x23,
                (byte)Epd5in65fCommands.PowerOffSequenceSetting,
                0x00,
                (byte)Epd5in65fCommands.PowerOn,
                0xC7,
                0xC7,
                0x1D,
                (byte)Epd5in65fCommands.PllControl,
                0x3C,
                (byte)Epd5in65fCommands.TemperatureCalibration,
                0x00,
                (byte)Epd5in65fCommands.VcomAndDataIntervalSetting,
                0x37,
                (byte)Epd5in65fCommands.TconSetting,
                0x22,
                (byte)Epd5in65fCommands.TconResolution,
                0x02,
                0x58,
                0x01,
                0xC0,
                (byte)Epd5in65fCommands.FlashMode,
                0xAA,
            };

            Assert.That(m_DataBuffer, Is.EqualTo(validBuffer), "Command Data Sequence is wrong");
        }

        [Test]
        public void TestMergePixelDataInByte()
        {
            using Epd5in65f result = new Epd5in65f();

            Random random = new Random();
            for (int i = 0; i < 200; i++)
            {
                byte b1 = (byte)random.Next(0, 0x0F);
                byte b2 = (byte)random.Next(0, 0x0F);

                byte oldResult = MergePixelDataInByte(b1, b2);
                byte newResult = result.MergePixelDataInByte(b1, b2);

                Assert.That(newResult, Is.EqualTo(oldResult), $"Merged Byte Run {i} is wrong. Expected {oldResult}, Returned {newResult}");
            }
        }

        #endregion Tests

        //########################################################################################

        #region Old working Implementation

        /// <summary>
        /// Original Method: Merge two DataBytes into one Byte
        /// </summary>
        /// <param name="pixel1"></param>
        /// <param name="pixel2"></param>
        /// <returns></returns>
        private static byte MergePixelDataInByte(byte pixel1, byte pixel2)
        {
            byte output = (byte)(pixel1 << 4);
            output |= pixel2;
            return output;
        }

        /// <summary>
        /// Original Method: Convert a Bitmap to a Byte Array
        /// </summary>
        /// <param name="image"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private static byte[] BitmapToData(SKBitmap image, int width, int height)
        {
            const int pixelPerByte = 2;

            List<byte> imageData = new List<byte>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x += pixelPerByte)
                {
                    SKColor pixel1 = GetPixel(image, x, y);
                    byte pixel1Data = ColorToByte(pixel1);

                    SKColor pixel2 = GetPixel(image, x + 1, y);
                    byte pixel2Data = ColorToByte(pixel2);

                    byte mergedData = MergePixelDataInByte(pixel1Data, pixel2Data);
                    imageData.Add(mergedData);
                }
            }

            return imageData.ToArray();
        }

        /// <summary>
        /// Original Method: Get the Pixel at position x, y or return a white pixel if it is out of bounds
        /// </summary>
        /// <param name="image"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private static SKColor GetPixel(SKBitmap image, int x, int y)
        {
            SKColor color = SKColors.White;

            if (x < image.Width && y < image.Height)
            {
                color = image.GetPixel(x, y);
            }

            return color;
        }

        /// <summary>
        /// Original Method: Convert a pixel to a DataByte
        /// </summary>
        /// <param name="pixel"></param>
        /// <returns>Pixel converted to specific byte value for the hardware</returns>
        private static byte ColorToByte(SKColor pixel)
        {
            if (IsMonochrom(pixel))
            {
                return pixel.Red <= 85 ? Epd5in65fColors.Black : Epd5in65fColors.White;
            }

            if (IsColor(pixel, ByteColors.Black))
            {
                return Epd5in65fColors.Black;
            }

            if (IsColor(pixel, ByteColors.White))
            {
                return Epd5in65fColors.White;
            }

            if (IsColor(pixel, ByteColors.Green))
            {
                return Epd5in65fColors.Green;
            }

            if (IsColor(pixel, ByteColors.Blue))
            {
                return Epd5in65fColors.Blue;
            }

            if (IsColor(pixel, ByteColors.Red))
            {
                return Epd5in65fColors.Red;
            }

            if (IsColor(pixel, ByteColors.Yellow))
            {
                return Epd5in65fColors.Yellow;
            }

            if (IsColor(pixel, ByteColors.Orange))
            {
                return Epd5in65fColors.Orange;
            }

            return Epd5in65fColors.Clean;
        }

        /// <summary>
        /// Original Method: Check if a Pixel is Monochrom (white, gray or black)
        /// </summary>
        /// <param name="pixel"></param>
        /// <returns></returns>
        private static bool IsMonochrom(SKColor pixel)
        {
            return pixel.Red == pixel.Green && pixel.Green == pixel.Blue;
        }

        /// <summary>
        /// Check if the Pixel is the Color
        /// </summary>
        /// <param name="pixel"></param>
        /// <param name="rgb"></param>
        /// <returns></returns>
        private static bool IsColor(SKColor pixel, ByteColor rgb)
        {
            return pixel.Red == rgb.R && pixel.Green == rgb.G && pixel.Blue == rgb.B;
        }
        #endregion Old working Implementation

        //########################################################################################

    }
}
