using FlightDashboard.Models;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlightDashboard.Services;

public sealed class AviationDataService
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(12)
    };

    public async Task<FeedSnapshot> GetSnapshotAsync(int radiusNauticalMiles)
    {
        var radius = Math.Clamp(radiusNauticalMiles, 25, 250);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.adsb.lol/v2/lat/39.5/lon/-98.35/dist/{radius}");
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("AirspaceDemo", "1.0"));

        using var response = await Client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var payload = await JsonSerializer.DeserializeAsync<AdsbResponse>(stream);
        if (payload?.Aircraft is null)
        {
            throw new InvalidDataException("The ADS-B.lol response did not contain an aircraft list.");
        }

        var flights = payload.Aircraft
            .Where(aircraft => !string.IsNullOrWhiteSpace(aircraft.Hex))
            .Select(ToFlight)
            .ToArray();

        var timestamp = ParseTimestamp(payload.Timestamp);

        return new FeedSnapshot(flights, timestamp);
    }

    private static DateTimeOffset ParseTimestamp(double? value)
    {
        if (value is not double timestamp || !double.IsFinite(timestamp))
        {
            return DateTimeOffset.UtcNow;
        }

        var milliseconds = timestamp > 10_000_000_000
            ? timestamp
            : timestamp * 1000;
        if (milliseconds < 0 || milliseconds > DateTimeOffset.MaxValue.ToUnixTimeMilliseconds())
        {
            return DateTimeOffset.UtcNow;
        }

        return DateTimeOffset.FromUnixTimeMilliseconds((long)milliseconds);
    }

    private static Flight ToFlight(AdsbAircraft aircraft)
    {
        var altitudeText = aircraft.Altitude.ValueKind == JsonValueKind.String
            ? aircraft.Altitude.GetString()
            : null;
        var onGround = string.Equals(altitudeText, "ground", StringComparison.OrdinalIgnoreCase);
        int? altitude = onGround ? 0 : ReadInteger(aircraft.Altitude);
        var callsign = string.IsNullOrWhiteSpace(aircraft.Flight)
            ? aircraft.Hex?.ToUpperInvariant() ?? string.Empty
            : aircraft.Flight.Trim();

        return new Flight(
            callsign,
            aircraft.Type?.Trim() ?? string.Empty,
            aircraft.Registration?.Trim() ?? string.Empty,
            altitude,
            ReadDouble(aircraft.GroundSpeed),
            ReadDouble(aircraft.Track),
            onGround);
    }

    private static int? ReadInteger(JsonElement value) =>
        value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)
            ? number
            : value.ValueKind == JsonValueKind.String &&
              int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number)
                ? number
                : null;

    private static double? ReadDouble(JsonElement value) =>
        value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number)
            ? number
            : value.ValueKind == JsonValueKind.String &&
              double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number)
                ? number
                : null;

    private sealed class AdsbResponse
    {
        public AdsbResponse()
        {
        }

        [JsonPropertyName("ac")]
        public List<AdsbAircraft>? Aircraft { get; set; }

        [JsonPropertyName("now")]
        public double? Timestamp { get; set; }
    }

    private sealed class AdsbAircraft
    {
        public AdsbAircraft()
        {
        }

        [JsonPropertyName("hex")]
        public string? Hex { get; set; }

        [JsonPropertyName("flight")]
        public string? Flight { get; set; }

        [JsonPropertyName("t")]
        public string? Type { get; set; }

        [JsonPropertyName("r")]
        public string? Registration { get; set; }

        [JsonPropertyName("alt_baro")]
        public JsonElement Altitude { get; set; }

        [JsonPropertyName("gs")]
        public JsonElement GroundSpeed { get; set; }

        [JsonPropertyName("track")]
        public JsonElement Track { get; set; }
    }
}

public sealed record FeedSnapshot(IReadOnlyList<Flight> Flights, DateTimeOffset Timestamp);
