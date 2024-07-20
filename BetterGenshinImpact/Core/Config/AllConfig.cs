using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.GameTask.AutoDomain;
using BetterGenshinImpact.GameTask.AutoFight;
using BetterGenshinImpact.GameTask.AutoFishing;
using BetterGenshinImpact.GameTask.AutoGeniusInvokation;
using BetterGenshinImpact.GameTask.AutoPick;
using BetterGenshinImpact.GameTask.AutoSkip;
using BetterGenshinImpact.GameTask.AutoWood;
using BetterGenshinImpact.GameTask.QuickTeleport;
using BetterGenshinImpact.Service.Notification;
using CommunityToolkit.Mvvm.ComponentModel;
using Fischless.GameCapture;
using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using BetterGenshinImpact.GameTask.AutoCook;

namespace BetterGenshinImpact.Core.Config;

/// <summary>
///     Определить объекты
/// </summary>
[Serializable]
public partial class AllConfig : ObservableObject
{
    /// <summary>
    ///     Как захватить окна
    /// </summary>
    [ObservableProperty]
    private string _captureMode = CaptureModes.BitBlt.ToString();

    /// <summary>
    ///     Подробный журнал ошибок
    /// </summary>
    [ObservableProperty]
    private bool _detailedErrorLogs;

    /// <summary>
    ///     Не отображать последнюю версию приглашения новой версии
    /// </summary>
    [ObservableProperty]
    private string _notShowNewVersionNoticeEndVersion = "";

    /// <summary>
    ///     Частота срабатывания триггера(ms)
    /// </summary>
    [ObservableProperty]
    private int _triggerInterval = 50;

    /// <summary>
    ///     WGCИспользуйте кэширование растровых изображений
    ///     При высокой частоте кадров，Может вызвать задержку
    ///     У Юньюань Шена может быть черный экран
    /// </summary>
    [ObservableProperty]
    private bool _wgcUseBitmapCache = true;

    /// <summary>
    /// Оборудование, используемое для рассуждения
    /// </summary>
    [ObservableProperty]
    private string _inferenceDevice = "CPU";

    /// <summary>
    ///     Конфигурация окна маски
    /// </summary>
    public MaskWindowConfig MaskWindowConfig { get; set; } = new();

    /// <summary>
    ///     Общая конфигурация
    /// </summary>
    public CommonConfig CommonConfig { get; set; } = new();

    /// <summary>
    ///     Конфигурация запуска Genshin Impact
    /// </summary>
    public GenshinStartConfig GenshinStartConfig { get; set; } = new();

    /// <summary>
    ///     Автоматическая настройка пикапа
    /// </summary>
    public AutoPickConfig AutoPickConfig { get; set; } = new();

    /// <summary>
    ///     Автоматическая конфигурация графика
    /// </summary>
    public AutoSkipConfig AutoSkipConfig { get; set; } = new();

    /// <summary>
    ///     Автоматическая настройка рыбалки
    /// </summary>
    public AutoFishingConfig AutoFishingConfig { get; set; } = new();

    /// <summary>
    ///     Быстрая передача конфигурации
    /// </summary>
    public QuickTeleportConfig QuickTeleportConfig { get; set; } = new();

    /// <summary>
    ///     Автоматическая настройка приготовления
    /// </summary>
    public AutoCookConfig AutoCookConfig { get; set; } = new();

    /// <summary>
    ///     Автоматическая настройка игры в карты
    /// </summary>
    public AutoGeniusInvokationConfig AutoGeniusInvokationConfig { get; set; } = new();

    /// <summary>
    ///     Конфигурация автоматического журналирования
    /// </summary>
    public AutoWoodConfig AutoWoodConfig { get; set; } = new();

    /// <summary>
    ///     Автоматическая боевая конфигурация
    /// </summary>
    public AutoFightConfig AutoFightConfig { get; set; } = new();

    /// <summary>
    ///     Автоматическая настройка секретной области
    /// </summary>
    public AutoDomainConfig AutoDomainConfig { get; set; } = new();

    /// <summary>
    ///     Конфигурация класса скрипта
    /// </summary>
    public MacroConfig MacroConfig { get; set; } = new();

    /// <summary>
    ///     Настройка сочетания клавиш
    /// </summary>
    public HotKeyConfig HotKeyConfig { get; set; } = new();

    /// <summary>
    ///     Конфигурация уведомлений
    /// </summary>
    public NotificationConfig NotificationConfig { get; set; } = new();

    [JsonIgnore]
    public Action? OnAnyChangedAction { get; set; }

    public void InitEvent()
    {
        PropertyChanged += OnAnyPropertyChanged;
        MaskWindowConfig.PropertyChanged += OnAnyPropertyChanged;
        CommonConfig.PropertyChanged += OnAnyPropertyChanged;
        GenshinStartConfig.PropertyChanged += OnAnyPropertyChanged;
        NotificationConfig.PropertyChanged += OnAnyPropertyChanged;
        NotificationConfig.PropertyChanged += OnNotificationPropertyChanged;

        AutoPickConfig.PropertyChanged += OnAnyPropertyChanged;
        AutoSkipConfig.PropertyChanged += OnAnyPropertyChanged;
        AutoFishingConfig.PropertyChanged += OnAnyPropertyChanged;
        QuickTeleportConfig.PropertyChanged += OnAnyPropertyChanged;
        AutoCookConfig.PropertyChanged += OnAnyPropertyChanged;
        MacroConfig.PropertyChanged += OnAnyPropertyChanged;
        HotKeyConfig.PropertyChanged += OnAnyPropertyChanged;
        AutoWoodConfig.PropertyChanged += OnAnyPropertyChanged;
        AutoFightConfig.PropertyChanged += OnAnyPropertyChanged;
        AutoDomainConfig.PropertyChanged += OnAnyPropertyChanged;
    }

    public void OnAnyPropertyChanged(object? sender, EventArgs args)
    {
        GameTaskManager.RefreshTriggerConfigs();
        OnAnyChangedAction?.Invoke();
    }

    public void OnNotificationPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        NotificationService.Instance().RefreshNotifiers();
    }
}
