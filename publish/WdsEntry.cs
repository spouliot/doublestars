namespace Wds;

public class WdsEntry {

	public string Identifier { get; internal set; }

	public string DiscovererId { get; internal set; }

	public string Components { get; internal set; }

	public int EpochFirst { get; internal set; }

	public int EpochLast { get; internal set; }

	public int NumberOfObservations { get; internal set; }

	public int ThetaFirst { get; internal set; }

	public int ThetaLast { get; internal set; }

	public double RhoFirst { get; internal set; }

	public double RhoLast { get; internal set; }

	public double MagnitudePrimary { get; internal set; }

	public double MagnitudeSecondary { get; internal set; }

	public string PreciseCoordinates { get; internal set; }

	public string SpectralType { get; internal set; }

	public string PrimaryProperMotionRA { get; internal set; }

	public string PrimaryProperMotionDec { get; internal set; }

	public string SecondaryProperMotionRA { get; internal set; }

	public string SecondaryProperMotionDec { get; internal set; }

	// no need (yet) for Durchmusterung Number and Notes fields

	internal WdsEntry (string line)
	{
		Identifier = line [..10];
		DiscovererId = line.Substring (10, 7);
		Components = line.Substring (17, 5);
		EpochFirst = int.Parse (line.Substring (23, 4));
		EpochLast = int.Parse (line.Substring (28, 4));
		NumberOfObservations = int.Parse (line.Substring (33, 4));
		ThetaFirst = int.Parse (line.Substring (38, 3));
		ThetaLast = int.Parse (line.Substring (42, 3));
		RhoFirst = double.Parse (line.Substring (46, 5));
		RhoLast = double.Parse (line.Substring (52, 5));
		MagnitudePrimary = double.Parse (line.Substring (58, 5));
		MagnitudeSecondary = double.Parse (line.Substring (64, 5));
		SpectralType = line.Substring (70, 9);
		PrimaryProperMotionRA = line.Substring (80, 4);
		PrimaryProperMotionDec = line.Substring (84, 4);
		SecondaryProperMotionRA = line.Substring (89, 4);
		SecondaryProperMotionDec = line.Substring (93, 4);
		PreciseCoordinates = line.Substring (112, 18);
	}

	static public WdsEntry Parse (string line)
	{
		return new WdsEntry (line);
	}
}
