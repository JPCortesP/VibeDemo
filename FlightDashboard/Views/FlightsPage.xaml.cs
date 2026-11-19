using FlightDashboard.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FlightDashboard.Views;

public sealed partial class FlightsPage : Page
{
    public MainPageViewModel ViewModel => App.ViewModel;

    public FlightsPage()
    {
        InitializeComponent();
    }
}
