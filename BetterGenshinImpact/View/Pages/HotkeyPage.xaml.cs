using BetterGenshinImpact.ViewModel.Pages;
using System.Windows.Controls;

namespace BetterGenshinImpact.View.Pages;

/// <summary>
/// TaskSettingsPage.xaml логика взаимодействия
/// </summary>
public partial class HotKeyPage : Page
{
    private HotKeyPageViewModel ViewModel { get; }

    public HotKeyPage(HotKeyPageViewModel viewModel)
    {
        DataContext = ViewModel = viewModel;
        InitializeComponent();
    }
}
