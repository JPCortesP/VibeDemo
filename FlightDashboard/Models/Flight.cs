namespace FlightDashboard.Models;

public sealed record Flight(
    string Callsign,
    string AircraftType,
    string Registration,
    int? AltitudeFeet,
    double? GroundSpeedKnots,
    double? TrackDegrees,
    bool IsOnGround)
{
    public string DisplayCallsign => string.IsNullOrWhiteSpace(Callsign) ? "Unknown callsign" : Callsign;

    public string DisplayType => HasAircraftType ? AircraftType : "Type unavailable";

    public string DisplayRegistration => string.IsNullOrWhiteSpace(Registration) ? "—" : Registration;

    public string AltitudeLabel => IsOnGround
        ? "On ground"
        : AltitudeFeet is int altitude
            ? $"{altitude:N0} ft"
            : "Unknown";

    public string SpeedLabel => GroundSpeedKnots is double speed ? $"{speed:N0} kt" : "—";

    public string StatusLabel => IsOnGround ? "ON GROUND" : IsCruising ? "CRUISING" : "AIRBORNE";

    public bool IsCruising => !IsOnGround && AltitudeFeet is >= 10000;

    public bool HasAircraftType => !string.IsNullOrWhiteSpace(AircraftType);

    public string CallsignPrefix
    {
        get
        {
            var prefix = new string(Callsign.Take(3).Where(char.IsLetter).ToArray()).ToUpperInvariant();
            return prefix.Length == 3 ? prefix : "OTHER";
        }
    }

    public static IReadOnlyList<Flight> CreateDemoSet()
    {
        string[] prefixes = ["UAL", "AAL", "DAL", "SWA", "JBU", "ASA", "FFT", "EJA", "NKS", "SKW"];
        string[] types = ["B738", "A321", "B77W", "A20N", "E175", "B789", "A320", "B38M", "B752", "CRJ9"];
        var flights = new List<Flight>(72);

        for (var index = 0; index < 72; index++)
        {
            var grounded = Random.Shared.Next(0, 14) == 0;
            var prefix = prefixes[Random.Shared.Next(prefixes.Length)];
            var flightNumber = Random.Shared.Next(100, 9999);
            flights.Add(new Flight(
                $"{prefix}{flightNumber}",
                Random.Shared.Next(0, 12) == 0 ? string.Empty : types[Random.Shared.Next(types.Length)],
                $"N{Random.Shared.Next(100, 99999)}",
                grounded ? 0 : Random.Shared.Next(85, 460) * 100,
                grounded ? null : Random.Shared.Next(110, 520),
                grounded ? null : Random.Shared.Next(0, 360),
                grounded));
        }

        return flights;
    }
}
