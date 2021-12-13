using System.Text;

const int columns = 5;

var path = args [0];
var home = new StringBuilder ();

for (int year = DateTime.UtcNow.Year; year >= 2021; year--) {
	home.Append ("# ").Append (year).AppendLine ();
	home.AppendLine ();
	home.Append ('|', columns + 1).AppendLine ();
	for (int c = 0; c < columns; c++)
		home.Append ("|:---:");
	home.AppendLine ("|");

	var files = Directory.EnumerateFiles (path, $"{year}-*.md").ToList ();
	files.Sort ();
	int i = 0;
	var thumnails = new StringBuilder ();
	var descriptions = new StringBuilder ();
	foreach (var file in files) {
		var name = Path.GetFileNameWithoutExtension (file);
		thumnails.Append ("|![").Append (name [5..]).Append ("](").Append (name).Append (".webp)");
		descriptions.Append ("|[").Append (name [5..].ToUpperInvariant ()).Append ("](").Append (name).Append (')');
		if (++i % columns == 0) {
			home.Append (thumnails).Append ('|').AppendLine ();
			home.Append (descriptions).Append ('|').AppendLine ();
			thumnails.Clear ();
			descriptions.Clear ();
		}
	}
	if (thumnails.Length > 0) {
		home.Append (thumnails).Append ('|', columns + 1 - (i % columns)).AppendLine ();
		home.Append (descriptions).Append ('|', columns & i).AppendLine ();
	}
	home.AppendLine ();
}
File.WriteAllText (Path.Combine (path, "Home.md"), home.ToString ());
return 0;