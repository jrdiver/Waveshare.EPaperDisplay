# Waveshare.EPaperDisplay
Library for .Net 8+ to control a Waveshare E-Paper Display.

This fork of the library has been updated to work on newer Raspbian versios that were after the SPI udpates that prevented the old way from working.  

Currently supported Models:
- Waveshare 7.5inch e-Paper (B)
- Waveshare 7.5inch e-Paper V2
- Waveshare 7.5inch e-Paper V2 (B)
- Waveshare 5.65inch e-Paper (F)

## Based on:

Specification from:
- https://www.waveshare.com/w/upload/2/29/7.5inch_e-paper-b-Specification.pdf
- https://www.waveshare.com/w/upload/6/60/7.5inch_e-Paper_V2_Specification.pdf
- https://www.waveshare.com/w/upload/4/44/7.5inch_e-Paper_B_V2_Specification.pdf
- https://www.waveshare.com/w/upload/7/7a/5.65inch_e-Paper_(F)_Sepecification.pdf

C Example Code from:
https://github.com/waveshare/e-Paper/tree/master/RaspberryPi_JetsonNano/c

## NuGet Package:

https://www.nuget.org/packages/eXoCooLd.Waveshare.EPaperDisplay/

## Usage for a public static method:

```C#
public static void Main()
{
	const string fileName = "yourImage.bmp";
	using var bitmap = new Bitmap(Image.FromFile(fileName, true));

	using var ePaperDisplay = EPaperDisplay.Create(EPaperDisplayType.WaveShare7In5Bc);
  
	ePaperDisplay.Clear();
	ePaperDisplay.WaitUntilReady();
	ePaperDisplay.DisplayImage(bitmap);
}
```

## Example running on Raspberry Pi 3

![Screenshot](workingWithRaspberryPi.jpg)

## Notes on Setup
This fork of this library was tested on:
- Raspbery Pi 2
- Raspbian 13 Trixie
- SPI Enabled in raspi-config
- "libgpiod-dev" installed via apt
- 7.5inch_e-Paper_V2 panel

## License
[MIT](LICENSE)
