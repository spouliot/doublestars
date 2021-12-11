using System.Text;

const int columns = 5;

var home = new StringBuilder ();
home.AppendLine ("# 2021");
home.AppendLine ();
home.Append ('|', columns + 1).AppendLine ();
for (int c = 0; c < columns; c++)
	home.Append ("|:---:");
home.AppendLine ("|");

var path = args [0];
var files = Directory.EnumerateFiles (path, "*.md").ToList ();
files.Sort ();
int i = 0;
var thumnails = new StringBuilder ();
var descriptions = new StringBuilder ();
foreach (var file in files) {
	var name = Path.GetFileNameWithoutExtension (file);
	switch (name) {
	// excluded markdown files
	case "Home":
		break;
	default:
		thumnails.Append ("|![").Append (name).Append ("](").Append (name).Append (".webp)");
		descriptions.Append ("|[").Append (name.ToUpperInvariant ()).Append ("](").Append (name).Append (".md)");
		if (++i % columns == 0) {
			home.Append (thumnails).Append ('|').AppendLine ();
			home.Append (descriptions).Append ('|').AppendLine ();
			thumnails.Clear ();
			descriptions.Clear ();
		}
		break;
	}
	Console.WriteLine (file);
}
if (thumnails.Length > 0) {
	home.Append (thumnails).Append ('|', columns + 1 - (i % columns)).AppendLine ();
	home.Append (descriptions).Append ('|', columns & i).AppendLine ();
}
File.WriteAllText (Path.Combine (path, "Home.md"), home.ToString ());
return 0;