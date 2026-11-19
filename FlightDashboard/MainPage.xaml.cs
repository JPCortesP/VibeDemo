using FlightDashboard.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FlightDashboard;

public sealed partial class MainPage : Page
{
    public MainPageViewModel ViewModel => App.ViewModel;

    public MainPage()
    {
        InitializeComponent();
        ContentFrame.Navigate(typeof(Views.OverviewPage));
        Loaded += MainPage_Loaded;
    }

    private async void MainPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
    }

    private void Navigation_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer?.Tag is not string destination)
        {
            return;
        }

        var pageType = destination switch
        {
            "Overview" => typeof(Views.OverviewPage),
            "Flights" => typeof(Views.FlightsPage),
            "Insights" => typeof(Views.InsightsPage),
            "Settings" => typeof(Views.SettingsPage),
            _ => null
        };

        if (pageType is not null && ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }
}
