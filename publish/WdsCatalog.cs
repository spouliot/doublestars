using System;
using System.IO;
using System.Text;

namespace Wds;

public class WdsCatalog {
	readonly string[] wds;

	public WdsCatalog (string catalog)
	{
		wds = File.ReadAllLines (catalog);
	}

	public WdsEntry? FindByDiscovererId (string id)
	{
		var key = new StringBuilder (12);
		// discover is one to three characters
		int i = 0;
		key.Append (Char.ToUpperInvariant (id [i++]));
		if (Char.IsLetter (id [1])) {
			key.Append (Char.ToUpperInvariant (id [i++]));
			if (Char.IsLetter (id [2]))
				key.Append (Char.ToUpperInvariant (id [i++]));
			else
				key.Append (' ');
		} else {
			key.Append ("  ");
		}
		// number is one to four characters
		var n = (int) id [i++] - (int) '0';
		while ((i < id.Length) && Char.IsDigit (id [i])) {
			n = n * 10 + (int) id [i++] - (int) '0';
		}
		key.Append ($"{n,4:D0}");
		// components are 0 to 5 characters
		for (int j = i; j < id.Length; j++) {
			key.Append (Char.ToUpperInvariant (id [j]));
		}
		while (key.Length < 12)
			key.Append (' ');
		// look up in wds catalogue
		var key_span = key.ToString ().AsSpan ();
		i = 0;
		foreach (var line in wds) {
			i++;
			if (line.Length < 24)
				continue;
			if (key_span.Equals (line.AsSpan (10, 12), StringComparison.Ordinal))
				return WdsEntry.Parse (line);
		}
		return null;
	}

}
