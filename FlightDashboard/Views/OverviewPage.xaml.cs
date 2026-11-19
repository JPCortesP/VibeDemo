using FlightDashboard.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FlightDashboard.Views;

public sealed partial class OverviewPage : Page
{
    public MainPageViewModel ViewModel => App.ViewModel;

    public OverviewPage()
    {
        InitializeComponent();
    }
}
