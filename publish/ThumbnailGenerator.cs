using System;
using NetVips;
using Spectre.Console;

static class ThumbnailGenerator {

	public static int Height { get; set; } = 128;
	public static int Width { get; set; } = 128;

	static Image Linear (this Image self, double a, double b)
	{
		// Existing API forces the use of arrays and create a non-required VOption instance
		// https://github.com/kleisauke/net-vips/issues/153
		// return image.Linear (new double [] { f }, new double [] { a });
		return (Image) Operation.Call ("linear", null, self, a, b);
	}

	static Image Linear (this Image image, double bglevel, double pklevel, double maxvalue)
	{
		var f = maxvalue / (pklevel - bglevel);
		var a = -(bglevel * f);
		return image.Linear (f, a);
	}

	// based on fits liberator code
	static Image Asinh (this Image image, double bglevel, double pklevel, double maxvalue)
	{
		double f = maxvalue / (pklevel - bglevel);
		double a = -(bglevel * f);
		using var lfa = image.Linear (f, a);
		using var pow2 = lfa.Pow (2.0d);
		using var l11 = pow2.Linear (1.0d, 1.0d);
		using var pow05 = l11.Pow (0.5d);
		using var add = pow05.Add (lfa);
		return add.Log ();
	}

	public static void Generate (string fileName, int x, int y, double pa, string outputFileName)
	{
		try {
			using var im = Image.NewFromFile (fileName);
			// double the crop area so we can rotate the image and crop (again) to the requested size
			using var crop = im.Crop (x - Width, Math.Max (im.Height - y - Height, 0), Width * 2, Height * 2);
			using var flip = crop.FlipVer ();
			using var rotate = (360.0d - pa > 1.0d) ? flip.Rotate (360.0d - pa) : flip;
			using var crop2 = rotate.Crop ((rotate.Width - Width) / 2, (rotate.Height - Height) / 2, Width, Height);

			using var stats = crop2.Stats ();
			double min = stats[0, 0][0];
			double max = stats[1, 0][0];
			double scaled_peak = (max - min) > 10000 ? 10.0d : 1.0d;
			double pklevel = (max - min) / scaled_peak;
			//AnsiConsole.MarkupLine ($"<bold>{name}</bold> : Max {max}, Min: {min}, PeakLevel: {pklevel}");
			double bglevel = 0.0d;
			var im2 = crop2.Asinh (bglevel, pklevel, 10.0d);
			using var scaled = im2.Linear (0.0d, 3.0d, 255.0d);
			var cast = scaled.Cast (Enums.BandFormat.Uchar);
			cast.WriteToFile (outputFileName);
		}
		catch (Exception ex) {
			AnsiConsole.MarkupLine ($"[bold red]Error:[/] Could not process file '{fileName}': ");
			AnsiConsole.WriteException (ex);
		}
	}
}
