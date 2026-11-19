using FlightDashboard.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FlightDashboard.Views;

public sealed partial class InsightsPage : Page
{
    public MainPageViewModel ViewModel => App.ViewModel;

    public InsightsPage()
    {
        InitializeComponent();
    }
}
