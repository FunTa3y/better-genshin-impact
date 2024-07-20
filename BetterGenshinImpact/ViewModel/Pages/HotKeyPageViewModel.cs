using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.GameTask.Macro;
using BetterGenshinImpact.Service.Interface;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using BetterGenshinImpact.Core.Simulator;
using BetterGenshinImpact.GameTask.AutoFight;
using BetterGenshinImpact.GameTask.AutoTrackPath;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.GameTask.Common.BgiVision;
using BetterGenshinImpact.GameTask.Model.Area;
using BetterGenshinImpact.Helpers.Extensions;
using Microsoft.Extensions.Logging;
using HotKeySettingModel = BetterGenshinImpact.Model.HotKeySettingModel;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.Mvvm.Messaging;
using BetterGenshinImpact.GameTask.QucikBuy;
using BetterGenshinImpact.GameTask.QuickSereniteaPot;
using BetterGenshinImpact.Helpers;
using BetterGenshinImpact.Model;
using Vanara.PInvoke;
using BetterGenshinImpact.GameTask.Model.Enum;
using static Vanara.PInvoke.User32;
using System.Runtime.InteropServices;
using System;
using System.Threading.Tasks;
using BetterGenshinImpact.Core.Monitor;
using BetterGenshinImpact.Core.Recorder;

namespace BetterGenshinImpact.ViewModel.Pages;

public partial class HotKeyPageViewModel : ObservableObject, IViewModel
{
    private readonly ILogger<HotKeyPageViewModel> _logger;
    private readonly TaskSettingsPageViewModel _taskSettingsPageViewModel;
    public AllConfig Config { get; set; }

    [ObservableProperty]
    private ObservableCollection<HotKeySettingModel> _hotKeySettingModels = new();

    public HotKeyPageViewModel(IConfigService configService, ILogger<HotKeyPageViewModel> logger, TaskSettingsPageViewModel taskSettingsPageViewModel)
    {
        _logger = logger;
        _taskSettingsPageViewModel = taskSettingsPageViewModel;
        // Получить конфигурацию
        Config = configService.Get();

        // Создайте список конфигурации сочетаний клавиш.
        BuildHotKeySettingModelList();

        foreach (var hotKeyConfig in HotKeySettingModels)
        {
            hotKeyConfig.RegisterHotKey();
            hotKeyConfig.PropertyChanged += (sender, e) =>
            {
                if (sender is HotKeySettingModel model)
                {
                    // Конфигурация обновления отражения

                    // Обновить сочетания клавиш
                    if (e.PropertyName == "HotKey")
                    {
                        Debug.WriteLine($"{model.FunctionName} Сочетание клавиш изменено на {model.HotKey}");
                        var pi = Config.HotKeyConfig.GetType().GetProperty(model.ConfigPropertyName, BindingFlags.Public | BindingFlags.Instance);
                        if (null != pi && pi.CanWrite)
                        {
                            var str = model.HotKey.ToString();
                            if (str == "< None >")
                            {
                                str = "";
                            }

                            pi.SetValue(Config.HotKeyConfig, str, null);
                        }
                    }

                    // Обновить сочетания клавиштип
                    if (e.PropertyName == "HotKeyType")
                    {
                        Debug.WriteLine($"{model.FunctionName} Тип сочетания клавиш изменен на {model.HotKeyType.ToChineseName()}");
                        model.HotKey = HotKey.None;
                        var pi = Config.HotKeyConfig.GetType().GetProperty(model.ConfigPropertyName + "Type", BindingFlags.Public | BindingFlags.Instance);
                        if (null != pi && pi.CanWrite)
                        {
                            pi.SetValue(Config.HotKeyConfig, model.HotKeyType.ToString(), null);
                        }
                    }

                    RemoveDuplicateHotKey(model);
                    model.UnRegisterHotKey();
                    model.RegisterHotKey();
                }
            };
        }
    }

    /// <summary>
    /// Удаление повторяющихся конфигураций сочетаний клавиш
    /// </summary>
    /// <param name="current"></param>
    private void RemoveDuplicateHotKey(HotKeySettingModel current)
    {
        if (current.HotKey.IsEmpty)
        {
            return;
        }

        foreach (var hotKeySettingModel in HotKeySettingModels)
        {
            if (hotKeySettingModel.HotKey.IsEmpty)
            {
                continue;
            }

            if (hotKeySettingModel.ConfigPropertyName != current.ConfigPropertyName && hotKeySettingModel.HotKey == current.HotKey)
            {
                hotKeySettingModel.HotKey = HotKey.None;
            }
        }
    }

    private void BuildHotKeySettingModelList()
    {
        var bgiEnabledHotKeySettingModel = new HotKeySettingModel(
            "начать стоп BetterGI",
            nameof(Config.HotKeyConfig.BgiEnabledHotkey),
            Config.HotKeyConfig.BgiEnabledHotkey,
            Config.HotKeyConfig.BgiEnabledHotkeyType,
            (_, _) => { WeakReferenceMessenger.Default.Send(new PropertyChangedMessage<object>(this, "SwitchTriggerStatus", "", "")); }
        );
        HotKeySettingModels.Add(bgiEnabledHotKeySettingModel);

        var takeScreenshotHotKeySettingModel = new HotKeySettingModel(
            "Скриншоты игры（Разработчик）",
            nameof(Config.HotKeyConfig.TakeScreenshotHotkey),
            Config.HotKeyConfig.TakeScreenshotHotkey,
            Config.HotKeyConfig.TakeScreenshotHotkeyType,
            (_, _) => { TaskTriggerDispatcher.Instance().TakeScreenshot(); }
        );
        HotKeySettingModels.Add(takeScreenshotHotKeySettingModel);

        HotKeySettingModels.Add(new HotKeySettingModel(
            "Переключатель отображения журнала и окна состояния",
            nameof(Config.HotKeyConfig.LogBoxDisplayHotkey),
            Config.HotKeyConfig.LogBoxDisplayHotkey,
            Config.HotKeyConfig.LogBoxDisplayHotkeyType,
            (_, _) =>
            {
                TaskContext.Instance().Config.MaskWindowConfig.ShowLogBox = !TaskContext.Instance().Config.MaskWindowConfig.ShowLogBox;
                // Синхронизировать с окном статуса
                TaskContext.Instance().Config.MaskWindowConfig.ShowStatus = TaskContext.Instance().Config.MaskWindowConfig.ShowLogBox;
            }
        ));

        var autoPickEnabledHotKeySettingModel = new HotKeySettingModel(
            "Автоматический переключатель звукоснимателя",
            nameof(Config.HotKeyConfig.AutoPickEnabledHotkey),
            Config.HotKeyConfig.AutoPickEnabledHotkey,
            Config.HotKeyConfig.AutoPickEnabledHotkeyType,
            (_, _) =>
            {
                TaskContext.Instance().Config.AutoPickConfig.Enabled = !TaskContext.Instance().Config.AutoPickConfig.Enabled;
                _logger.LogInformation("выключатель{Name}Статус[{Enabled}]", "автоматический сбор", ToChinese(TaskContext.Instance().Config.AutoPickConfig.Enabled));
            }
        );
        HotKeySettingModels.Add(autoPickEnabledHotKeySettingModel);

        var autoSkipEnabledHotKeySettingModel = new HotKeySettingModel(
            "Автоматическое переключение сюжета",
            nameof(Config.HotKeyConfig.AutoSkipEnabledHotkey),
            Config.HotKeyConfig.AutoSkipEnabledHotkey,
            Config.HotKeyConfig.AutoSkipEnabledHotkeyType,
            (_, _) =>
            {
                TaskContext.Instance().Config.AutoSkipConfig.Enabled = !TaskContext.Instance().Config.AutoSkipConfig.Enabled;
                _logger.LogInformation("выключатель{Name}Статус[{Enabled}]", "автоматический сюжет", ToChinese(TaskContext.Instance().Config.AutoSkipConfig.Enabled));
            }
        );
        HotKeySettingModels.Add(autoSkipEnabledHotKeySettingModel);

        HotKeySettingModels.Add(new HotKeySettingModel(
            "Автоматическое переключение приглашений",
            nameof(Config.HotKeyConfig.AutoSkipHangoutEnabledHotkey),
            Config.HotKeyConfig.AutoSkipHangoutEnabledHotkey,
            Config.HotKeyConfig.AutoSkipHangoutEnabledHotkeyType,
            (_, _) =>
            {
                TaskContext.Instance().Config.AutoSkipConfig.AutoHangoutEventEnabled = !TaskContext.Instance().Config.AutoSkipConfig.AutoHangoutEventEnabled;
                _logger.LogInformation("выключатель{Name}Статус[{Enabled}]", "Автоматическое приглашение", ToChinese(TaskContext.Instance().Config.AutoSkipConfig.AutoHangoutEventEnabled));
            }
        ));

        var autoFishingEnabledHotKeySettingModel = new HotKeySettingModel(
            "Автоматический переключатель рыбалки",
            nameof(Config.HotKeyConfig.AutoFishingEnabledHotkey),
            Config.HotKeyConfig.AutoFishingEnabledHotkey,
            Config.HotKeyConfig.AutoFishingEnabledHotkeyType,
            (_, _) =>
            {
                TaskContext.Instance().Config.AutoFishingConfig.Enabled = !TaskContext.Instance().Config.AutoFishingConfig.Enabled;
                _logger.LogInformation("выключатель{Name}Статус[{Enabled}]", "автоматическая рыбалка", ToChinese(TaskContext.Instance().Config.AutoFishingConfig.Enabled));
            }
        );
        HotKeySettingModels.Add(autoFishingEnabledHotKeySettingModel);

        var quickTeleportEnabledHotKeySettingModel = new HotKeySettingModel(
            "Быстрый переключатель",
            nameof(Config.HotKeyConfig.QuickTeleportEnabledHotkey),
            Config.HotKeyConfig.QuickTeleportEnabledHotkey,
            Config.HotKeyConfig.QuickTeleportEnabledHotkeyType,
            (_, _) =>
            {
                TaskContext.Instance().Config.QuickTeleportConfig.Enabled = !TaskContext.Instance().Config.QuickTeleportConfig.Enabled;
                _logger.LogInformation("выключатель{Name}Статус[{Enabled}]", "Быстрая доставка", ToChinese(TaskContext.Instance().Config.QuickTeleportConfig.Enabled));
            }
        );
        HotKeySettingModels.Add(quickTeleportEnabledHotKeySettingModel);

        var quickTeleportTickHotKeySettingModel = new HotKeySettingModel(
            "ручной триггерБыстрая доставкаЗапустить сочетания клавиш（Нажмите и удерживайте, чтобы изменения вступили в силу.）",
            nameof(Config.HotKeyConfig.QuickTeleportTickHotkey),
            Config.HotKeyConfig.QuickTeleportTickHotkey,
            Config.HotKeyConfig.QuickTeleportTickHotkeyType,
            (_, _) => { Thread.Sleep(100); },
            true
        );
        HotKeySettingModels.Add(quickTeleportTickHotKeySettingModel);

        var turnAroundHotKeySettingModel = new HotKeySettingModel(
            "Длительное нажатие, чтобы повернуть перспективу - Навилетта кружится по кругу",
            nameof(Config.HotKeyConfig.TurnAroundHotkey),
            Config.HotKeyConfig.TurnAroundHotkey,
            Config.HotKeyConfig.TurnAroundHotkeyType,
            (_, _) => { TurnAroundMacro.Done(); },
            true
        );
        HotKeySettingModels.Add(turnAroundHotKeySettingModel);

        var enhanceArtifactHotKeySettingModel = new HotKeySettingModel(
            "Нажмите, чтобы быстро укрепить святую реликвию",
            nameof(Config.HotKeyConfig.EnhanceArtifactHotkey),
            Config.HotKeyConfig.EnhanceArtifactHotkey,
            Config.HotKeyConfig.EnhanceArtifactHotkeyType,
            (_, _) => { QuickEnhanceArtifactMacro.Done(); },
            true
        );
        HotKeySettingModels.Add(enhanceArtifactHotKeySettingModel);

        HotKeySettingModels.Add(new HotKeySettingModel(
            "Нажмите, чтобы быстро купить товары в магазине",
            nameof(Config.HotKeyConfig.QuickBuyHotkey),
            Config.HotKeyConfig.QuickBuyHotkey,
            Config.HotKeyConfig.QuickBuyHotkeyType,
            (_, _) => { QuickBuyTask.Done(); },
            true
        ));

        HotKeySettingModels.Add(new HotKeySettingModel(
            "Нажмите, чтобы быстро войти и выйти из горшка с песней пыли.",
            nameof(Config.HotKeyConfig.QuickSereniteaPotHotkey),
            Config.HotKeyConfig.QuickSereniteaPotHotkey,
            Config.HotKeyConfig.QuickSereniteaPotHotkeyType,
            (_, _) => { QuickSereniteaPotTask.Done(); }
        ));

        HotKeySettingModels.Add(new HotKeySettingModel(
            "запускать/Остановить автоматический призыв Семи Святых.",
            nameof(Config.HotKeyConfig.AutoGeniusInvokationHotkey),
            Config.HotKeyConfig.AutoGeniusInvokationHotkey,
            Config.HotKeyConfig.AutoGeniusInvokationHotkeyType,
            (_, _) => { _taskSettingsPageViewModel.OnSwitchAutoGeniusInvokation(); }
        ));

        HotKeySettingModels.Add(new HotKeySettingModel(
            "запускать/Остановить автоматическое ведение журнала",
            nameof(Config.HotKeyConfig.AutoWoodHotkey),
            Config.HotKeyConfig.AutoWoodHotkey,
            Config.HotKeyConfig.AutoWoodHotkeyType,
            (_, _) => { _taskSettingsPageViewModel.OnSwitchAutoWood(); }
        ));

        HotKeySettingModels.Add(new HotKeySettingModel(
            "запускать/Остановить автобой",
            nameof(Config.HotKeyConfig.AutoFightHotkey),
            Config.HotKeyConfig.AutoFightHotkey,
            Config.HotKeyConfig.AutoFightHotkeyType,
            (_, _) => { _taskSettingsPageViewModel.OnSwitchAutoFight(); }
        ));

        HotKeySettingModels.Add(new HotKeySettingModel(
            "запускать/Остановить автоматическое секретное царство",
            nameof(Config.HotKeyConfig.AutoDomainHotkey),
            Config.HotKeyConfig.AutoDomainHotkey,
            Config.HotKeyConfig.AutoDomainHotkeyType,
            (_, _) => { _taskSettingsPageViewModel.OnSwitchAutoDomain(); }
        ));

        HotKeySettingModels.Add(new HotKeySettingModel(
            "Быстро нажмите кнопку подтверждения в Genshin Impact.",
            nameof(Config.HotKeyConfig.ClickGenshinConfirmButtonHotkey),
            Config.HotKeyConfig.ClickGenshinConfirmButtonHotkey,
            Config.HotKeyConfig.ClickGenshinConfirmButtonHotkeyType,
            (_, _) =>
            {
                if (Bv.ClickConfirmButton(TaskControl.CaptureToRectArea()))
                {
                    TaskControl.Logger.LogInformation("Запустить быстрый клик в Genshin Impact{Btn}кнопка：успех", "подтверждать");
                }
                else
                {
                    TaskControl.Logger.LogInformation("Запустить быстрый клик в Genshin Impact{Btn}кнопка：не найденокнопкакартина", "подтверждать");
                }
            }
        ));

        HotKeySettingModels.Add(new HotKeySettingModel(
            "Быстро щелкните в Genshin ImpactОтменакнопка",
            nameof(Config.HotKeyConfig.ClickGenshinCancelButtonHotkey),
            Config.HotKeyConfig.ClickGenshinCancelButtonHotkey,
            Config.HotKeyConfig.ClickGenshinCancelButtonHotkeyType,
            (_, _) =>
            {
                if (Bv.ClickCancelButton(TaskControl.CaptureToRectArea()))
                {
                    TaskControl.Logger.LogInformation("Запустить быстрый клик в Genshin Impact{Btn}кнопка：успех", "Отмена");
                }
                else
                {
                    TaskControl.Logger.LogInformation("Запустить быстрый клик в Genshin Impact{Btn}кнопка：не найденокнопкакартина", "Отмена");
                }
            }
        ));

        HotKeySettingModels.Add(new HotKeySettingModel(
            "Сочетания клавиш боевых макросов в один клик",
            nameof(Config.HotKeyConfig.OneKeyFightHotkey),
            Config.HotKeyConfig.OneKeyFightHotkey,
            Config.HotKeyConfig.OneKeyFightHotkeyType,
            null,
            true)
        {
            OnKeyDownAction = (_, _) => { OneKeyFightTask.Instance.KeyDown(); },
            OnKeyUpAction = (_, _) => { OneKeyFightTask.Instance.KeyUp(); }
        });

        var keyMouseRecordRunning = false;
        HotKeySettingModels.Add(new HotKeySettingModel(
            "запускать/Остановить запись клавиатуры и мыши",
            nameof(Config.HotKeyConfig.KeyMouseMacroRecordHotkey),
            Config.HotKeyConfig.KeyMouseMacroRecordHotkey,
            Config.HotKeyConfig.KeyMouseMacroRecordHotkeyType,
            (_, _) =>
            {
                var vm = App.GetService<KeyMouseRecordPageViewModel>();
                if (vm == null)
                {
                    _logger.LogError("невозможно найти KeyMouseRecordPageViewModel Синглтон-объект！");
                    return;
                }
                if (!keyMouseRecordRunning)
                {
                    keyMouseRecordRunning = true;
                    Thread.Sleep(300); // Запретить ввод сочетаний клавиш
                    vm.OnStartRecord();
                }
                else
                {
                    keyMouseRecordRunning = false;
                    vm.OnStopRecord();
                }
            }
        ));

        if (RuntimeHelper.IsDebug)
        {
            HotKeySettingModels.Add(new HotKeySettingModel(
                "запускать/Остановить автоматические аудиоигры с активностью",
                nameof(Config.HotKeyConfig.AutoMusicGameHotkey),
                Config.HotKeyConfig.AutoMusicGameHotkey,
                Config.HotKeyConfig.AutoMusicGameHotkeyType,
                (_, _) => { _taskSettingsPageViewModel.OnSwitchAutoMusicGame(); }
            ));
            HotKeySettingModels.Add(new HotKeySettingModel(
                "（тест）запускать/Остановить автоматическое отслеживание",
                nameof(Config.HotKeyConfig.AutoTrackHotkey),
                Config.HotKeyConfig.AutoTrackHotkey,
                Config.HotKeyConfig.AutoTrackHotkeyType,
                (_, _) => { _taskSettingsPageViewModel.OnSwitchAutoTrack(); }
            ));
            HotKeySettingModels.Add(new HotKeySettingModel(
                "（тест）Запись маршрута карты",
                nameof(Config.HotKeyConfig.MapPosRecordHotkey),
                Config.HotKeyConfig.MapPosRecordHotkey,
                Config.HotKeyConfig.MapPosRecordHotkeyType,
                (_, _) =>
                {
                    PathPointRecorder.Instance.Switch();
                }));
            HotKeySettingModels.Add(new HotKeySettingModel(
                "（тест）Автоматический поиск пути",
                nameof(Config.HotKeyConfig.AutoTrackPathHotkey),
                Config.HotKeyConfig.AutoTrackPathHotkey,
                Config.HotKeyConfig.AutoTrackPathHotkeyType,
                (_, _) => { _taskSettingsPageViewModel.OnSwitchAutoTrackPath(); }
            ));

            var flag = false;
            var m = "";
            HotKeySettingModels.Add(new HotKeySettingModel(
                "（тест）тест",
                nameof(Config.HotKeyConfig.Test1Hotkey),
                Config.HotKeyConfig.Test1Hotkey,
                Config.HotKeyConfig.Test1HotkeyType,
                (_, _) =>
                {
                    // if (postMessageSimulator == null)
                    // {
                    //     postMessageSimulator = Simulation.PostMessage(TaskContext.Instance().GameHandle);
                    // }
                    // User32.GetCursorPos(out var p);
                    // Debug.WriteLine($"положение мыши：{p.X},{p.Y}");
                    // // postMessageSimulator.KeyPressBackground(User32.VK.VK_W);
                    // GameCaptureRegion.GameRegion1080PPosMove(1340, 655);
                    // postMessageSimulator.LeftButtonClickBackground(1340, 655);
                    // Thread.Sleep(5);
                    // DesktopRegion.DesktopRegionMove(p.X, p.Y);

                    // Расположение на большой карте
                    // TaskTriggerDispatcher.Instance().SetCacheCaptureMode(DispatcherCaptureModeEnum.OnlyCacheCapture);
                    // Thread.Sleep(TaskContext.Instance().Config.TriggerInterval * 5); // Ожидание кэшированного изображения
                    // AutoTrackPathTask.GetPositionFromBigMap();

                    // Simulation.SendInput.Mouse.MoveMouseBy(400, 0).Sleep(200)
                    //     .Keyboard.KeyPress(User32.VK.VK_W).Sleep(500);

                    // HWND hWnd = GetForegroundWindow();
                    //
                    // uint threadid = GetWindowThreadProcessId(hWnd, out var _);
                    //
                    // GUITHREADINFO lpgui = new GUITHREADINFO();
                    // lpgui.cbSize = (uint)Marshal.SizeOf(lpgui);
                    //
                    // if (GetGUIThreadInfo(threadid, ref lpgui))
                    // {
                    // if (lpgui.hwndCaret != 0)
                    // {
                    //     _logger.LogInformation("статус ввода");
                    //     return;
                    // }
                    // }
                    // _logger.LogInformation("Нетстатус ввода");
                }
            ));

            HotKeySettingModels.Add(new HotKeySettingModel(
                "（тест）тест2",
                nameof(Config.HotKeyConfig.Test2Hotkey),
                Config.HotKeyConfig.Test2Hotkey,
                Config.HotKeyConfig.Test2HotkeyType,
                (_, _) =>
                {
                    _logger.LogInformation("Начать воспроизведение сценария");
                    Task.Run(async () =>
                    {
                        await KeyMouseMacroPlayer.PlayMacro(m);
                        _logger.LogInformation("Конец сценария воспроизведения");
                    });
                }
            ));
        }
    }

    private string ToChinese(bool enabled)
    {
        return enabled.ToChinese();
    }
}
