using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlightDashboard.Models;
using FlightDashboard.Services;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace FlightDashboard.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly AviationDataService _dataService = new();
    private bool _initialized;
    private int _refreshVersion;

    public ObservableCollection<Flight> FilteredFlights { get; } = [];
    public ObservableCollection<DistributionItem> OperatorBreakdown { get; } = [];
    public ObservableCollection<DistributionItem> TypeBreakdown { get; } = [];
    public ObservableCollection<DistributionItem> AltitudeBreakdown { get; } = [];

    [ObservableProperty]
    public partial bool IsDemoMode { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial int FlightCount { get; set; }

    [ObservableProperty]
    public partial int CruisingCount { get; set; }

    [ObservableProperty]
    public partial int GroundCount { get; set; }

    [ObservableProperty]
    public partial int OperatorCount { get; set; }

    [ObservableProperty]
    public partial int TypedAircraftCount { get; set; }

    [ObservableProperty]
    public partial double SearchRadius { get; set; } = 250;

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedFilter { get; set; } = "All traffic";

    [ObservableProperty]
    public partial string FeedState { get; set; } = "STARTING";

    [ObservableProperty]
    public partial string UpdatedLabel { get; set; } = "Preparing a sample snapshot";

    [ObservableProperty]
    public partial string FeedWarningTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FeedWarningMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasFeedWarning { get; set; }

    [ObservableProperty]
    public partial bool IsDemoData { get; set; } = true;

    public int TypeCoveragePercent => FlightCount == 0
        ? 0
        : (int)Math.Round(TypedAircraftCount * 100.0 / FlightCount);

    public string TypeCoveragePercentLabel => $"{TypeCoveragePercent}%";

    public string TypeCoverageLabel => $"{TypedAircraftCount:N0} of {FlightCount:N0} aircraft identified";

    public bool IsFilteredListEmpty => FilteredFlights.Count == 0;

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        ShowDemoSnapshot();
        if (!IsDemoMode)
        {
            await RefreshAsync();
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var version = Interlocked.Increment(ref _refreshVersion);
        if (IsDemoMode)
        {
            ShowDemoSnapshot();
            return;
        }

        IsBusy = true;
        FeedState = IsDemoData ? "SAMPLE DATA · CONNECTING" : "LIVE FEED · REFRESHING";
        ClearFeedWarning();
        try
        {
            var snapshot = await _dataService.GetSnapshotAsync((int)SearchRadius);
            if (version != Volatile.Read(ref _refreshVersion) || IsDemoMode)
            {
                return;
            }

            if (snapshot.Flights.Count == 0)
            {
                ShowDemoSnapshot();
                FeedState = "SAMPLE DATA · NO AIRCRAFT IN RANGE";
                ShowFeedWarning(
                    "No live aircraft in range",
                    "The feed responded, but returned no aircraft inside the selected radius. The visible flights are synthetic demo data.");
                return;
            }

            ReplaceFlights(snapshot.Flights);
            IsDemoData = false;
            FeedState = "LIVE FEED · ADS-B.LOL";
            UpdatedLabel = $"Snapshot time · {snapshot.Timestamp.ToLocalTime():t} local";
            OnPropertyChanged(nameof(TypeCoverageLabel));
            ClearFeedWarning();
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            JsonException or
            InvalidDataException)
        {
            if (version != Volatile.Read(ref _refreshVersion) || IsDemoMode)
            {
                return;
            }

            if (IsDemoData)
            {
                ShowDemoSnapshot();
                FeedState = "SAMPLE DATA · FEED UNAVAILABLE";
            }
            else
            {
                FeedState = "LIVE FEED · LAST SNAPSHOT";
                UpdatedLabel += " · refresh failed";
            }

            ShowFeedWarning(
                "Could not refresh the live feed",
                "The ADS-B.lol snapshot could not be reached. " +
                (IsDemoData
                    ? "The displayed aircraft are randomized demo data, not live positions."
                    : "The last successful live snapshot is retained and may be stale."));
        }
        finally
        {
            if (version == Volatile.Read(ref _refreshVersion))
            {
                IsBusy = false;
            }
        }
    }

    partial void OnIsDemoModeChanged(bool value)
    {
        Interlocked.Increment(ref _refreshVersion);
        IsBusy = false;
        if (value)
        {
            ShowDemoSnapshot();
            ClearFeedWarning();
        }
        else if (_initialized)
        {
            _ = RefreshAsync();
        }
    }

    partial void OnSearchRadiusChanged(double value)
    {
        var boundedRadius = double.IsFinite(value)
            ? Math.Round(Math.Clamp(value, 25, 250))
            : 250;
        if (value != boundedRadius)
        {
            SearchRadius = boundedRadius;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    partial void OnSelectedFilterChanged(string value) => ApplyFilters();

    private void ShowDemoSnapshot()
    {
        ReplaceFlights(Flight.CreateDemoSet());
        IsDemoData = true;
        UpdatedLabel = $"Sample generated · {DateTime.Now:t} local";
        FeedState = IsDemoMode ? "DEMO MODE · SYNTHETIC DATA" : "SAMPLE DATA · CONNECTING";
    }

    private void ReplaceFlights(IEnumerable<Flight> flights)
    {
        var items = flights.ToArray();
        FilteredFlights.Clear();
        _allFlights.Clear();
        foreach (var flight in items)
        {
            _allFlights.Add(flight);
        }

        FlightCount = items.Length;
        CruisingCount = items.Count(flight => flight.IsCruising);
        GroundCount = items.Count(flight => flight.IsOnGround);
        OperatorCount = items.Select(flight => flight.CallsignPrefix).Distinct().Count();
        TypedAircraftCount = items.Count(flight => flight.HasAircraftType);
        UpdateDistributions(items);
        ApplyFilters();
        OnPropertyChanged(nameof(TypeCoveragePercent));
        OnPropertyChanged(nameof(TypeCoveragePercentLabel));
        OnPropertyChanged(nameof(TypeCoverageLabel));
    }

    private readonly List<Flight> _allFlights = [];

    private void ApplyFilters()
    {
        var query = SearchText.Trim();
        var selectedFilter = SelectedFilter;
        var matchingFlights = _allFlights.Where(flight =>
            (selectedFilter switch
            {
                "Cruising" => flight.IsCruising,
                "On ground" => flight.IsOnGround,
                _ => true
            }) &&
            (query.Length == 0 ||
             flight.Callsign.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             flight.AircraftType.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             flight.Registration.Contains(query, StringComparison.OrdinalIgnoreCase)));

        FilteredFlights.Clear();
        foreach (var flight in matchingFlights)
        {
            FilteredFlights.Add(flight);
        }

        OnPropertyChanged(nameof(IsFilteredListEmpty));
    }

    private void UpdateDistributions(IReadOnlyCollection<Flight> flights)
    {
        ReplaceDistribution(
            OperatorBreakdown,
            flights.GroupBy(flight => flight.CallsignPrefix)
                .OrderByDescending(group => group.Count())
                .Take(6)
                .Select(group => (group.Key, group.Count())),
            flights.Count);

        ReplaceDistribution(
            TypeBreakdown,
            flights.Where(flight => flight.HasAircraftType)
                .GroupBy(flight => flight.AircraftType)
                .OrderByDescending(group => group.Count())
                .Take(6)
                .Select(group => (group.Key, group.Count())),
            flights.Count);

        var bands = new[]
        {
            ("Below 10,000 ft", flights.Count(flight => !flight.IsOnGround && flight.AltitudeFeet is < 10000)),
            ("10,000-29,999 ft", flights.Count(flight => flight.AltitudeFeet is >= 10000 and < 30000)),
            ("30,000-39,999 ft", flights.Count(flight => flight.AltitudeFeet is >= 30000 and < 40000)),
            ("40,000 ft and above", flights.Count(flight => flight.AltitudeFeet is >= 40000))
        };
        ReplaceDistribution(AltitudeBreakdown, bands.Where(band => band.Item2 > 0), flights.Count);
    }

    private static void ReplaceDistribution(
        ObservableCollection<DistributionItem> target,
        IEnumerable<(string Label, int Count)> groups,
        int total)
    {
        target.Clear();
        foreach (var (label, count) in groups)
        {
            target.Add(new DistributionItem(
                label,
                count,
                total == 0 ? 0 : count * 100.0 / total));
        }
    }

    private void ShowFeedWarning(string title, string message)
    {
        FeedWarningTitle = title;
        FeedWarningMessage = message;
        HasFeedWarning = true;
    }

    private void ClearFeedWarning()
    {
        FeedWarningTitle = string.Empty;
        FeedWarningMessage = string.Empty;
        HasFeedWarning = false;
    }
}
