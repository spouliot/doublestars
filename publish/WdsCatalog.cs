using System;
using System.IO;
using System.Text;

namespace Wds;

public class WdsCatalog {
	readonly string [] wds;

	public WdsCatalog (string catalog)
	{
		wds = File.ReadAllLines (catalog);
	}

	public WdsEntry? FindByDiscovererId (string id)
	{
		var key = new StringBuilder (12);
		// format must be identical to the catalog, we cannot guess spaces
		key.Append (id.ToUpperInvariant ());
		// components are optional
		while (key.Length < 12)
			key.Append (' ');
		// look up in wds catalogue
		var key_span = key.ToString ().AsSpan ();
		foreach (var line in wds) {
			if (line.Length < 24)
				continue;
			if (key_span.Equals (line.AsSpan (10, 12), StringComparison.Ordinal))
				return WdsEntry.Parse (line);
		}
		return null;
	}

}
