using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using Spectre.Console;
using Wds;

static class Program {

	static WdsCatalog? wds;
	const string wds_file = "wdsweb_summ2.txt";
	const int ExecutionError = 4;

	/// <summary>
	/// Publish markdown and png thumbnails based on the CSV files.
	/// </summary>
	/// <param name="csvPath">Path to the CSV files to process.</param>
	/// <param name="wdsCatalogPath">Path to the 'wdsweb_summ2.txt' catalog file.</param>
	/// <param name="outputPath">Path to the output directory. It will be created if needed.</param>
	static int Main (string csvPath = ".", string wdsCatalogPath = ".", string outputPath = ".")
	{
		var wds_full_path = Path.Combine (wdsCatalogPath, wds_file);
		if (!File.Exists (wds_full_path)) {
			AnsiConsole.MarkupLine ($"[bold red]Error:[/] WDS catalogue '{wds_file}' [underline]not found[/] inside '{wdsCatalogPath}'.");
			return 1;
		}

		if (!Directory.Exists (csvPath)) {
			AnsiConsole.MarkupLine ($"[bold red]Error:[/] Directory '{csvPath}' [underline]not found[/].");
			return 2;
		}

		if (!Directory.Exists (outputPath)) {
			try {
				Directory.CreateDirectory (outputPath);
			} catch (Exception ex) {
				AnsiConsole.MarkupLine ($"[bold red]Error:[/] Could not create directory '{outputPath}': ");
				AnsiConsole.WriteException (ex);
				return 3;
			}
		}

		try {
			wds = new WdsCatalog (wds_full_path);
		} catch (Exception ex) {
			AnsiConsole.Markup ("[bold red]Error:[/] loading WDS catalog: ");
			AnsiConsole.WriteException (ex);
			return ExecutionError;
		}

		try {
			foreach (var file in Directory.EnumerateFiles (csvPath, "*.csv"))
				ProcessCsv (file, outputPath);
		} catch (Exception ex) {
			AnsiConsole.Markup ("[bold red]Error:[/] processing CSV file: ");
			AnsiConsole.WriteException (ex);
			return ExecutionError + 1;
		}

		return 0;
	}

	static (double avg, double sd, double sem) Compute (List<double> values, int decimals)
	{
		var avg = values.Average ();
		// standard deviation of a >sample<
		var sd = Math.Sqrt (values.Sum (v => Math.Pow (v - avg, 2)) / (values.Count - 1));
		// standard error of the mean
		var sem = sd / Math.Sqrt (values.Count);
		return (Math.Round (avg, decimals, MidpointRounding.AwayFromZero),
			Math.Round (sd, decimals, MidpointRounding.AwayFromZero),
			Math.Round (sem, decimals, MidpointRounding.AwayFromZero));
	}

	static void GetFitsHeaders (string file, Dictionary<string,string?> headers)
	{
		// ensure previous results, if any, are removed
		foreach (var entry in headers.Keys) {
			headers [entry] = null;
		}

		var fits = File.OpenText (file);
		var buffer = new char [80];
		var line = new Span<char> (buffer);
		while (fits.Read (line) == 80) {
			var key = line [0..8].Trim ().ToString ();
			if (key == "END")
				break;
			if (!headers.ContainsKey (key))
				continue;
			var value = line [10..].Trim ();
			// handle comments
			for (int i = 0; i < value.Length; i++) {
				if (value [i] == '/')
					value = value [0..i].Trim ();
			}
			headers [key] = value.ToString ();
		}
	}

	// https://stackoverflow.com/questions/5248827/convert-datetime-to-julian-date-in-c-sharp-tooadate-safe
	public static double ToJulianDate (this DateTime date)
	{
		return date.ToOADate () + 2415018.5;
	}

	// the header format is not always identical
	static bool TryParse (string? s, out DateTime d)
	{
		switch (s?.Length) {
		case 25:
			return DateTime.TryParseExact (s, @"\'yyyy-MM-dd\THH:mm:ss.fff\'", null, DateTimeStyles.AssumeUniversal, out d);
		case 24:
			return DateTime.TryParseExact (s, @"\'yyyy-MM-dd\THH:mm:ss.ff\'", null, DateTimeStyles.AssumeUniversal, out d);
		case 23:
			return DateTime.TryParseExact (s, @"\'yyyy-MM-dd\THH:mm:ss.f\'", null, DateTimeStyles.AssumeUniversal, out d);
		case 21:
			return DateTime.TryParseExact (s, @"\'yyyy-MM-dd\THH:mm:ss\'", null, DateTimeStyles.AssumeUniversal, out d);
		default:
			d = DateTime.MinValue;
			return false;
		}
	}

	static void ProcessCsv (string csv, string outputPath)
	{
		var lines = File.ReadLines (csv);
		var name = Path.GetFileNameWithoutExtension (csv);
		var dir = Environment.CurrentDirectory;
		// first line is the header of the columns
		var pos = 0;
		// columns might vary depending on the configuration, find them using the first (header) line
		var col_label  = -1;
		var col_arclen = -1;
		var col_posang = -1;
		var col_x1fits = -1;
		var col_y1fits = -1;
		var arclen = new List<double> ();
		var posang = new List<double> ();
		// fits
		var min_date_obs = DateTime.MaxValue;
		var max_date_obs = DateTime.MinValue;
		var exposure = double.MinValue;
		var xpixsize = double.MinValue;
		var ypixsize = double.MinValue;
		var focal_length = double.MinValue;
		var fits_headers = new Dictionary<string,string?> () {
			{ "DATE-OBS", null },
			{ "EXPTIME", null },
			{ "XPIXSZ", null },
			{ "YPIXSZ", null },
			{ "FOCALLEN", null } ,
			{ "PA", null },
		};
		foreach (var line in lines) {
			var values = line.Split (',');
			if (pos == 0) {
				col_label  = Array.IndexOf (values, "Label");
				col_arclen = Array.IndexOf (values, "ArcLen (sec)");
				col_posang = Array.IndexOf (values, "PosAng (deg)");
				col_x1fits = Array.IndexOf (values, "X1(FITS)");
				col_y1fits = Array.IndexOf (values, "Y1(FITS)");
				pos++;
				continue;
			} else {
				arclen.Add (double.Parse (values [col_arclen]));
				posang.Add (double.Parse (values [col_posang]));
			}
			var cvs_dir = Path.GetDirectoryName (csv);
			var infile = Path.Combine (cvs_dir ?? ".", values [col_label]);
				GetFitsHeaders (infile, fits_headers);
			// generate the thumbnail for the first image only, pos `0` is for the header (not data)
			if (pos == 1) {
				var pa = 360.0d;
				var fh_pa = fits_headers ["PA"];
				if (fh_pa is null) {
					AnsiConsole.WriteLine ($"Could not find 'PA' inside the headers of '{infile}'. A value of `360` will be assumed to create the thumbnail.");
				} else {
					pa = double.Parse (fh_pa);
				}

				if (TryParse (fits_headers ["DATE-OBS"], out var date_obs)) {
					min_date_obs = max_date_obs = date_obs.ToUniversalTime ();
				} else {
					AnsiConsole.WriteLine ($"Could not find 'DATE-OBS' inside the headers of '{infile}'. Entry will be ignored.");
					continue;
				}

				var outfile = Path.GetFullPath (Path.Combine (outputPath, $"{date_obs.Year}-{name}.webp"));
				var x1 = (int) double.Parse (values [col_x1fits]);
				var y1 = (int) double.Parse (values [col_y1fits]);
				ThumbnailGenerator.Generate (infile, x1, y1, pa, outfile);

				if (!double.TryParse (fits_headers ["EXPTIME"], out exposure)) {
					AnsiConsole.WriteLine ($"Could not find 'EXPTIME' inside the headers of '{infile}'. Entry will be ignored.");
					continue;
				}

				if (!double.TryParse (fits_headers ["XPIXSZ"], out xpixsize)) {
					AnsiConsole.WriteLine ($"Could not find 'XPIXSZ' inside the headers of '{infile}'. Entry will be ignored.");
					continue;
				}

				if (!double.TryParse (fits_headers ["YPIXSZ"], out ypixsize)) {
					AnsiConsole.WriteLine ($"Could not find 'YPIXSZ' inside the headers of '{infile}'. Entry will be ignored.");
					continue;
				}

				if (!double.TryParse (fits_headers ["FOCALLEN"], out focal_length)) {
					AnsiConsole.WriteLine ($"Could not find 'FOCALLEN' inside the headers of '{infile}'. Entry will be ignored.");
					continue;
				}
			} else {
				if (!double.TryParse (fits_headers ["EXPTIME"], out var exp)) {
					AnsiConsole.WriteLine ($"Could not find 'EXPTIME' inside the headers of '{infile}'. Entry will be ignored.");
					continue;
				}

				if (TryParse (fits_headers ["DATE-OBS"], out var date_obs)) {
					date_obs = date_obs.ToUniversalTime ();
					if (date_obs < min_date_obs)
						min_date_obs = date_obs;
					if (date_obs > max_date_obs)
						max_date_obs = date_obs;
				} else {
					AnsiConsole.WriteLine ($"Could not find 'DATE-OBS' inside the headers of '{infile}'. Entry will be ignored.");
					continue;
				}
			}
			pos++;
		}

		// min + (max - min) / 2 + (exp / 2)
		var mid_date_obs = min_date_obs.AddSeconds ((max_date_obs - min_date_obs).TotalSeconds / 2 + (exposure / 2));
		var mid_jd = mid_date_obs.ToJulianDate ();
		var mid_j2000 = 2000 + (mid_jd - 2451545.0d) / 365.25d;

		var entry = wds?.FindByDiscovererId (name);
		if (entry is null) {
			AnsiConsole.WriteLine ($"Could not find id '{name}' inside the catalog. Entry will be ignored.");
#if DEBUG
			entry = wds?.FindByDiscovererId (name);
#endif
			return;
		}

		(var arc_avg, var arc_sd, var arc_sem) = Compute (arclen, 3);
		(var pos_avg, var pos_sd, var pos_sem) = Compute (posang, 3);

		var md = new StringBuilder ();
		md.Append ("# ").AppendLine (name.ToUpperInvariant ());
		md.AppendLine ();
		md.AppendLine ("## Thumbnail");
		md.AppendLine ();
		md.AppendLine ($"![thumbnail]({min_date_obs.Year}-{name}.webp)");
		md.AppendLine ();
		md.Append ($"*North is up, East is left. Image is {ThumbnailGenerator.Width}x{ThumbnailGenerator.Height} pixels");
		var asw = (xpixsize / focal_length) * 206.265d;
		var w = ThumbnailGenerator.Width * asw;
		if ((ThumbnailGenerator.Width == ThumbnailGenerator.Height) && (xpixsize == ypixsize)) {
			md.AppendLine ($" with {asw:F2}″/px ({w:F0}″)*");
		} else {
			var ash = (ypixsize / focal_length) * 206.265d;
			var h = ThumbnailGenerator.Height * ash;
			md.AppendLine ($" with {asw:F2}x{ash:F2}″/px ({w:F0}x{h:F0}″)*");
		}
		md.AppendLine ();
		md.AppendLine ("## WDS Catalog Information");
		md.AppendLine ();
		var url_id = WebUtility.UrlEncode (entry.Identifier); // the `+` and `-` needs to be encoded
		md.AppendLine ($"* Identifier: **{entry.Identifier}** [(stelledoppie.it)](https://www.stelledoppie.it/index2.php?cerca_database={url_id}&azione=cerca_testo_database&nofilter=1&section=2)");
		var pc = entry.PreciseCoordinates;
		md.Append ($"* Coordinates: **{pc}** ");
		var dss_url = "https://archive.eso.org/dss/dss/image?equinox=J2000&mime-type=image%2Fgif&statsmode=WEBFORM&" +
			$"ra={pc [0..2]}%3A{pc [2..4]}%3A{pc [4..9]}&dec=" +
			(pc [9] == '+' ? "%2B" : "-") + $"{pc [10..12]}%3A{pc [12..14]}%3A{pc [14..18]}&";
		md.Append ($"[(dss1)]({dss_url}&x=4&y=4&Sky-Survey=DSS1) ");
		md.Append ($"[(dss2-red)]({dss_url}&x=2&y=2&&Sky-Survey=DSS2-red) ");
		md.Append ($"[(dss2-blue)]({dss_url}&x=2&y=2&&Sky-Survey=DSS2-blue) ");
		md.Append ($"[(dss2-infrared)]({dss_url}&x=2&y=2&&Sky-Survey=DSS2-infrared)");
		md.AppendLine ();
		md.AppendLine ($"* Number of observations: **{entry.NumberOfObservations}** (not including this one)");
		md.AppendLine ();
		md.AppendLine ( "| Measurements       | First | Last | This |");
		md.AppendLine ( "|--------------------|:-----:|:----:|:----:|");
		md.AppendLine ($"| Epoch              | {entry.EpochFirst}  | {entry.EpochLast}  | {mid_j2000:F3} |");
		md.AppendLine ($"| Position Angle (θ) | {entry.ThetaFirst}° | {entry.ThetaLast}° | {pos_avg:F3}° ± {pos_sd:F3} |");
		md.AppendLine ($"| Separation (ρ)     | {entry.RhoFirst}″   | {entry.RhoLast}″   | {arc_avg:F3}″ ± {arc_sd:F3} |");
		md.AppendLine ();
		md.AppendLine ( "|                    | Primary | Secondary |");
		md.AppendLine ( "|--------------------|:-------:|:---------:|");
		md.AppendLine ($"| Magnitude          | {entry.MagnitudePrimary}  | {entry.MagnitudeSecondary} |");
		md.AppendLine ($"| Proper Motion RA   | {entry.PrimaryProperMotionRA}″   | {entry.SecondaryProperMotionRA}″ |");
		md.AppendLine ($"| Proper Motion Dec  | {entry.PrimaryProperMotionDec}″   | {entry.SecondaryProperMotionDec}″ |");
		md.AppendLine ();
		md.AppendLine ("## Imaging Session Information");
		md.AppendLine ();
		md.AppendLine ($"* Date: {mid_date_obs:s} (mid exposure)");
		md.AppendLine ($"* Epoch: J{mid_j2000:F3} (mid exposure)");
		md.AppendLine ($"* {arclen.Count} images x {exposure:F1} seconds exposures");
		md.AppendLine ();
		md.AppendLine ("## Measurements");
		md.AppendLine ();
		md.AppendLine ("| Image | Position Angle (θ) | Separation (ρ) |");
		md.AppendLine ("|:-----:|-------------------:|---------------:|");
		for (int i = 1; i <= arclen.Count; i++) {
			md.AppendLine ($"| {i} | { posang [i - 1]:F3}° | {arclen [i - 1]:F3}″ |");
		}

		md.AppendLine ($"| Means | {pos_avg:F3}° | {arc_avg:F3}″ |");
		md.AppendLine ($"| Standard Deviations | {pos_sd:F3}° | {arc_sd:F3}″ |");
		md.AppendLine ($"| Standard Error of the Means | {pos_sem:F3} | {arc_sem:F3} |");

		var fname = Path.Combine (outputPath, $"{min_date_obs.Year}-{name}.md");
		File.WriteAllText (fname, md.ToString ());
	}
}
