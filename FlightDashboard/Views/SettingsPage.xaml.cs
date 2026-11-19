using FlightDashboard.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FlightDashboard.Views;

public sealed partial class SettingsPage : Page
{
    public MainPageViewModel ViewModel => App.ViewModel;

    public SettingsPage()
    {
        InitializeComponent();
    }
}
