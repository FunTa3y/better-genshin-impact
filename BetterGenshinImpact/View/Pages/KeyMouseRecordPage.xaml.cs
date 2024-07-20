using BetterGenshinImpact.ViewModel.Pages;
using System.Windows.Controls;

namespace BetterGenshinImpact.View.Pages;

/// <summary>
/// KeyMouseRecordPage.xaml логика взаимодействия
/// </summary>
public partial class KeyMouseRecordPage : Page
{
    private KeyMouseRecordPageViewModel ViewModel { get; }

    public KeyMouseRecordPage(KeyMouseRecordPageViewModel viewModel)
    {
        DataContext = ViewModel = viewModel;
        InitializeComponent();
    }
}
