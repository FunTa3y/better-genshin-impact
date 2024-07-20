using System;
using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.GameTask.AutoDomain;
using BetterGenshinImpact.GameTask.AutoFight;
using BetterGenshinImpact.GameTask.AutoGeniusInvokation;
using BetterGenshinImpact.GameTask.AutoSkip.Model;
using BetterGenshinImpact.GameTask.AutoWood;
using BetterGenshinImpact.GameTask.Model;
using BetterGenshinImpact.Service.Interface;
using BetterGenshinImpact.View.Pages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Wpf.Ui;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;
using BetterGenshinImpact.GameTask.AutoMusicGame;
using BetterGenshinImpact.GameTask.AutoTrackPath;

namespace BetterGenshinImpact.ViewModel.Pages;

public partial class TaskSettingsPageViewModel : ObservableObject, INavigationAware, IViewModel
{
    public AllConfig Config { get; set; }

    private readonly INavigationService _navigationService;
    private readonly TaskTriggerDispatcher _taskDispatcher;

    private CancellationTokenSource? _cts;
    private static readonly object _locker = new();

    [ObservableProperty] private string[] _strategyList;
    [ObservableProperty] private string _switchAutoGeniusInvokationButtonText = "запускать";

    [ObservableProperty] private int _autoWoodRoundNum;
    [ObservableProperty] private int _autoWoodDailyMaxCount = 2000;
    [ObservableProperty] private string _switchAutoWoodButtonText = "запускать";

    [ObservableProperty] private string[] _combatStrategyList;
    [ObservableProperty] private int _autoDomainRoundNum;
    [ObservableProperty] private string _switchAutoDomainButtonText = "запускать";
    [ObservableProperty] private string _switchAutoFightButtonText = "запускать";
    [ObservableProperty] private string _switchAutoTrackButtonText = "запускать";
    [ObservableProperty] private string _switchAutoTrackPathButtonText = "запускать";
    [ObservableProperty] private string _switchAutoMusicGameButtonText = "запускать";

    public TaskSettingsPageViewModel(IConfigService configService, INavigationService navigationService, TaskTriggerDispatcher taskTriggerDispatcher)
    {
        Config = configService.Get();
        _navigationService = navigationService;
        _taskDispatcher = taskTriggerDispatcher;

        _strategyList = LoadCustomScript(Global.Absolute(@"User\AutoGeniusInvokation"));

        _combatStrategyList = ["Автоматический выбор по команде", .. LoadCustomScript(Global.Absolute(@"User\AutoFight"))];
    }

    private string[] LoadCustomScript(string folder)
    {
        var files = Directory.GetFiles(folder, "*.*",
            SearchOption.AllDirectories);

        var strategyList = new string[files.Length];
        for (var i = 0; i < files.Length; i++)
        {
            if (files[i].EndsWith(".txt"))
            {
                var strategyName = files[i].Replace(folder, "").Replace(".txt", "");
                if (strategyName.StartsWith(@"\"))
                {
                    strategyName = strategyName[1..];
                }
                strategyList[i] = strategyName;
            }
        }

        return strategyList;
    }

    [RelayCommand]
    private void OnStrategyDropDownOpened(string type)
    {
        switch (type)
        {
            case "Combat":
                CombatStrategyList = ["Автоматический выбор по команде", .. LoadCustomScript(Global.Absolute(@"User\AutoFight"))];
                break;

            case "GeniusInvocation":
                StrategyList = LoadCustomScript(Global.Absolute(@"User\AutoGeniusInvokation"));
                break;
        }
    }

    public void OnNavigatedTo()
    {
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    public void OnGoToHotKeyPage()
    {
        _navigationService.Navigate(typeof(HotKeyPage));
    }

    [RelayCommand]
    public void OnSwitchAutoGeniusInvokation()
    {
        try
        {
            lock (_locker)
            {
                if (SwitchAutoGeniusInvokationButtonText == "запускать")
                {
                    if (string.IsNullOrEmpty(Config.AutoGeniusInvokationConfig.StrategyName))
                    {
                        MessageBox.Show("Пожалуйста, сначала выберите стратегию");
                        return;
                    }

                    var path = Global.Absolute(@"User\AutoGeniusInvokation\" + Config.AutoGeniusInvokationConfig.StrategyName + ".txt");

                    if (!File.Exists(path))
                    {
                        MessageBox.Show("Файл политики не существует");
                        return;
                    }

                    var content = File.ReadAllText(path);
                    _cts?.Cancel();
                    _cts = new CancellationTokenSource();
                    var param = new GeniusInvokationTaskParam(_cts, content);
                    _taskDispatcher.StartIndependentTask(IndependentTaskEnum.AutoGeniusInvokation, param);
                    SwitchAutoGeniusInvokationButtonText = "останавливаться";
                }
                else
                {
                    _cts?.Cancel();
                    SwitchAutoGeniusInvokationButtonText = "запускать";
                }
            }
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    [RelayCommand]
    public void OnGoToAutoGeniusInvokationUrl()
    {
        Process.Start(new ProcessStartInfo("https://bgi.huiyadan.com/doc.html#%E8%87%AA%E5%8A%A8%E4%B8%83%E5%9C%A3%E5%8F%AC%E5%94%A4") { UseShellExecute = true });
    }

    [RelayCommand]
    public void OnSwitchAutoWood()
    {
        try
        {
            lock (_locker)
            {
                if (SwitchAutoWoodButtonText == "запускать")
                {
                    _cts?.Cancel();
                    _cts = new CancellationTokenSource();
                    var param = new WoodTaskParam(_cts, AutoWoodRoundNum, AutoWoodDailyMaxCount);
                    _taskDispatcher.StartIndependentTask(IndependentTaskEnum.AutoWood, param);
                    SwitchAutoWoodButtonText = "останавливаться";
                }
                else
                {
                    _cts?.Cancel();
                    SwitchAutoWoodButtonText = "запускать";
                }
            }
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    [RelayCommand]
    public void OnGoToAutoWoodUrl()
    {
        Process.Start(new ProcessStartInfo("https://bgi.huiyadan.com/feats/felling.html") { UseShellExecute = true });
    }

    [RelayCommand]
    public void OnSwitchAutoFight()
    {
        try
        {
            lock (_locker)
            {
                if (SwitchAutoFightButtonText == "запускать")
                {
                    var path = Global.Absolute(@"User\AutoFight\" + Config.AutoFightConfig.StrategyName + ".txt");
                    if ("Автоматический выбор по команде".Equals(Config.AutoFightConfig.StrategyName))
                    {
                        path = Global.Absolute(@"User\AutoFight\");
                    }
                    if (!File.Exists(path) && !Directory.Exists(path))
                    {
                        MessageBox.Show("Запустить сочетания клавишФайл политики не существует");
                        return;
                    }

                    _cts?.Cancel();
                    _cts = new CancellationTokenSource();
                    var param = new AutoFightParam(_cts, path);
                    _taskDispatcher.StartIndependentTask(IndependentTaskEnum.AutoFight, param);
                    SwitchAutoFightButtonText = "останавливаться";
                }
                else
                {
                    _cts?.Cancel();
                    SwitchAutoFightButtonText = "запускать";
                }
            }
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    [Obsolete]
    private string? ReadFightStrategy(string strategyName)
    {
        if (string.IsNullOrEmpty(strategyName))
        {
            MessageBox.Show("Пожалуйста, сначала выберите стратегию боя");
            return null;
        }

        var path = Global.Absolute(@"User\AutoFight\" + strategyName + ".txt");

        if (!File.Exists(path))
        {
            MessageBox.Show("Запустить сочетания клавишФайл политики не существует");
            return null;
        }

        var content = File.ReadAllText(path);
        return content;
    }

    [RelayCommand]
    public void OnGoToAutoFightUrl()
    {
        Process.Start(new ProcessStartInfo("https://bgi.huiyadan.com/feats/domain.html") { UseShellExecute = true });
    }

    [RelayCommand]
    public void OnSwitchAutoDomain()
    {
        try
        {
            lock (_locker)
            {
                if (SwitchAutoDomainButtonText == "запускать")
                {
                    var path = Global.Absolute(@"User\AutoFight\" + Config.AutoFightConfig.StrategyName + ".txt");
                    if ("Автоматический выбор по команде".Equals(Config.AutoFightConfig.StrategyName))
                    {
                        path = Global.Absolute(@"User\AutoFight\");
                    }
                    if (!File.Exists(path) && !Directory.Exists(path))
                    {
                        MessageBox.Show("Запустить сочетания клавишФайл политики не существует");
                        return;
                    }

                    _cts?.Cancel();
                    _cts = new CancellationTokenSource();
                    var param = new AutoDomainParam(_cts, AutoDomainRoundNum, path);
                    _taskDispatcher.StartIndependentTask(IndependentTaskEnum.AutoDomain, param);
                    SwitchAutoDomainButtonText = "останавливаться";
                }
                else
                {
                    _cts?.Cancel();
                    SwitchAutoDomainButtonText = "запускать";
                }
            }
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    [RelayCommand]
    public void OnGoToAutoDomainUrl()
    {
        Process.Start(new ProcessStartInfo("https://bgi.huiyadan.com/feats/domain.html") { UseShellExecute = true });
    }

    [RelayCommand]
    public void OnSwitchAutoTrack()
    {
        try
        {
            lock (_locker)
            {
                if (SwitchAutoTrackButtonText == "запускать")
                {
                    _cts?.Cancel();
                    _cts = new CancellationTokenSource();
                    var param = new AutoTrackParam(_cts);
                    _taskDispatcher.StartIndependentTask(IndependentTaskEnum.AutoTrack, param);
                    SwitchAutoTrackButtonText = "останавливаться";
                }
                else
                {
                    _cts?.Cancel();
                    SwitchAutoTrackButtonText = "запускать";
                }
            }
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    [RelayCommand]
    public void OnGoToAutoTrackUrl()
    {
        Process.Start(new ProcessStartInfo("https://bgi.huiyadan.com/feats/track.html") { UseShellExecute = true });
    }

    [RelayCommand]
    public void OnSwitchAutoTrackPath()
    {
        try
        {
            lock (_locker)
            {
                if (SwitchAutoTrackPathButtonText == "запускать")
                {
                    _cts?.Cancel();
                    _cts = new CancellationTokenSource();
                    var param = new AutoTrackPathParam(_cts);
                    _taskDispatcher.StartIndependentTask(IndependentTaskEnum.AutoTrackPath, param);
                    SwitchAutoTrackPathButtonText = "останавливаться";
                }
                else
                {
                    _cts?.Cancel();
                    SwitchAutoTrackPathButtonText = "запускать";
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    [RelayCommand]
    public void OnGoToAutoTrackPathUrl()
    {
        Process.Start(new ProcessStartInfo("https://bgi.huiyadan.com/feats/track.html") { UseShellExecute = true });
    }

    [RelayCommand]
    public void OnSwitchAutoMusicGame()
    {
        try
        {
            lock (_locker)
            {
                if (SwitchAutoMusicGameButtonText == "запускать")
                {
                    _cts?.Cancel();
                    _cts = new CancellationTokenSource();
                    var param = new AutoMusicGameParam(_cts);
                    _taskDispatcher.StartIndependentTask(IndependentTaskEnum.AutoMusicGame, param);
                    SwitchAutoMusicGameButtonText = "останавливаться";
                }
                else
                {
                    _cts?.Cancel();
                    SwitchAutoMusicGameButtonText = "запускать";
                }
            }
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    [RelayCommand]
    public void OnGoToAutoMusicGameUrl()
    {
        Process.Start(new ProcessStartInfo("https://bgi.huiyadan.com/feats/music.html") { UseShellExecute = true });
    }

    public static void SetSwitchAutoTrackButtonText(bool running)
    {
        var instance = App.GetService<TaskSettingsPageViewModel>();
        if (instance == null)
        {
            return;
        }

        instance.SwitchAutoTrackButtonText = running ? "останавливаться" : "запускать";
    }

    public static void SetSwitchAutoGeniusInvokationButtonText(bool running)
    {
        var instance = App.GetService<TaskSettingsPageViewModel>();
        if (instance == null)
        {
            return;
        }

        instance.SwitchAutoGeniusInvokationButtonText = running ? "останавливаться" : "запускать";
    }

    public static void SetSwitchAutoWoodButtonText(bool running)
    {
        var instance = App.GetService<TaskSettingsPageViewModel>();
        if (instance == null)
        {
            return;
        }

        instance.SwitchAutoWoodButtonText = running ? "останавливаться" : "запускать";
    }

    public static void SetSwitchAutoDomainButtonText(bool running)
    {
        var instance = App.GetService<TaskSettingsPageViewModel>();
        if (instance == null)
        {
            return;
        }

        instance.SwitchAutoDomainButtonText = running ? "останавливаться" : "запускать";
    }

    public static void SetSwitchAutoFightButtonText(bool running)
    {
        var instance = App.GetService<TaskSettingsPageViewModel>();
        if (instance == null)
        {
            return;
        }

        instance.SwitchAutoFightButtonText = running ? "останавливаться" : "запускать";
    }
}
