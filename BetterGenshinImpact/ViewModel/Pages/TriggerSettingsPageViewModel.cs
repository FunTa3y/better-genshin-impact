using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.GameTask.AutoPick;
using BetterGenshinImpact.GameTask.AutoSkip.Assets;
using BetterGenshinImpact.Service.Interface;
using BetterGenshinImpact.View.Pages;
using BetterGenshinImpact.View.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Diagnostics;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace BetterGenshinImpact.ViewModel.Pages;

public partial class TriggerSettingsPageViewModel : ObservableObject, INavigationAware, IViewModel
{
    [ObservableProperty]
    private string[] _clickChatOptionNames = ["Отдайте предпочтение первому варианту", "Случайный выбор вариантов", "Отдайте предпочтение последнему варианту", "Не выбирать вариант"];

    [ObservableProperty]
    private string[] _pickOcrEngineNames = [PickOcrEngineEnum.Paddle.ToString(), PickOcrEngineEnum.Yap.ToString()];

    [ObservableProperty]
    private string[] _defaultPickButtonNames = ["F", "E"];

    public AllConfig Config { get; set; }

    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private List<string> _hangoutBranches;

    public TriggerSettingsPageViewModel(IConfigService configService, INavigationService navigationService)
    {
        Config = configService.Get();
        _navigationService = navigationService;
        _hangoutBranches = HangoutConfig.Instance.HangoutOptionsTitleList;
    }

    public void OnNavigatedTo()
    {
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    private void OnEditBlacklist()
    {
        JsonMonoDialog.Show(@"User\pick_black_lists.json");
    }

    [RelayCommand]
    private void OnEditWhitelist()
    {
        JsonMonoDialog.Show(@"User\pick_white_lists.json");
    }

    [RelayCommand]
    private void OnOpenReExploreCharacterBox(object sender)
    {
        var str = PromptDialog.Prompt("Пожалуйста, используйте имя персонажа, отображаемое в интерфейсе отправки.，Разделение английской запятой，Приоритет уменьшается слева направо.。\nПример：Фишер,Беннетт,ночная орхидея,Шен Хэ,Куки Синобу",
            "Конфигурация приоритета роли диспетчера", Config.AutoSkipConfig.AutoReExploreCharacter);
        Config.AutoSkipConfig.AutoReExploreCharacter = str.Replace("，", ",").Replace(" ", "");
    }

    [RelayCommand]
    public void OnGoToQGroupUrl()
    {
        Process.Start(new ProcessStartInfo("http://qm.qq.com/cgi-bin/qm/qr?_wv=1027&k=mL1O7atys6Prlu5LBVqmDlfOrzyPMLN4&authKey=jSI2WuZyUjmpIUIAsBAf5g0r5QeSu9K6Un%2BRuSsQ8fQGYwGYwRVioFfJyYnQqvbf&noverify=0&group_code=863012276") { UseShellExecute = true });
    }

    [RelayCommand]
    public void OnGoToHotKeyPage()
    {
        _navigationService.Navigate(typeof(HotKeyPage));
    }
}
