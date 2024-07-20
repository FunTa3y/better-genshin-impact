using System.Windows.Controls;
using BetterGenshinImpact.ViewModel.Pages;

namespace BetterGenshinImpact.View.Pages
{
    /// <summary>
    /// NotificationSettingsPage.xaml логика взаимодействия
    /// </summary>
    public partial class NotificationSettingsPage : Page
    {
        private NotificationSettingsPageViewModel ViewModel { get; }

        public NotificationSettingsPage(NotificationSettingsPageViewModel viewModel)
        {
            DataContext = ViewModel = viewModel;
            InitializeComponent();
        }
    }
}
