using BetterGenshinImpact.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace BetterGenshinImpact.Core.Config;

/// <summary>
///     Определить объекты быстрая клавиша и быстрая клавишаType
/// </summary>
[Serializable]
public partial class HotKeyConfig : ObservableObject
{
    [ObservableProperty]
    private string _autoTrackHotkey = "";

    [ObservableProperty]
    private string _autoTrackHotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    [ObservableProperty]
    private string _autoDomainHotkey = "";

    [ObservableProperty]
    private string _autoDomainHotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    [ObservableProperty]
    private string _autoFightHotkey = "";

    [ObservableProperty]
    private string _autoFightHotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    [ObservableProperty]
    private string _autoFishingEnabledHotkey = "";

    [ObservableProperty]
    private string _autoFishingEnabledHotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    [ObservableProperty]
    private string _autoGeniusInvokationHotkey = "";

    [ObservableProperty]
    private string _autoGeniusInvokationHotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    [ObservableProperty]
    private string _autoPickEnabledHotkey = "";

    [ObservableProperty]
    private string _autoPickEnabledHotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    [ObservableProperty]
    private string _autoSkipEnabledHotkey = "";

    [ObservableProperty]
    private string _autoSkipEnabledHotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    [ObservableProperty]
    private string _autoSkipHangoutEnabledHotkey = "";

    [ObservableProperty]
    private string _autoSkipHangoutEnabledHotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    [ObservableProperty]
    private string _autoWoodHotkey = "";

    [ObservableProperty]
    private string _autoWoodHotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    [ObservableProperty]
    private string _bgiEnabledHotkey = "F11";

    [ObservableProperty]
    private string _bgiEnabledHotkeyType = HotKeyTypeEnum.GlobalRegister.ToString();

    [ObservableProperty]
    private string _enhanceArtifactHotkey = "";

    [ObservableProperty]
    private string _enhanceArtifactHotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    [ObservableProperty]
    private string _quickBuyHotkey = "";

    [ObservableProperty]
    private string _quickBuyHotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    [ObservableProperty]
    private string _quickSereniteaPotHotkey = "";

    [ObservableProperty]
    private string _quickSereniteaPotHotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    [ObservableProperty]
    private string _quickTeleportEnabledHotkey = "";

    [ObservableProperty]
    private string _quickTeleportEnabledHotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    // Триггер быстрой передачи
    [ObservableProperty]
    private string _quickTeleportTickHotkey = "";

    [ObservableProperty]
    private string _quickTeleportTickHotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    // Скриншот
    [ObservableProperty]
    private string _takeScreenshotHotkey = "";

    [ObservableProperty]
    private string _takeScreenshotHotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    [ObservableProperty]
    private string _turnAroundHotkey = "";

    [ObservableProperty]
    private string _turnAroundHotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    // Нажмите кнопку подтверждения
    [ObservableProperty]
    private string _clickGenshinConfirmButtonHotkey = "";

    [ObservableProperty]
    private string _clickGenshinConfirmButtonHotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    // Нажмите кнопку отмены
    [ObservableProperty]
    private string _clickGenshinCancelButtonHotkey = "";

    [ObservableProperty]
    private string _clickGenshinCancelButtonHotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    // Боевой макрос в один клик
    [ObservableProperty]
    private string _oneKeyFightHotkey = "";

    [ObservableProperty]
    private string _oneKeyFightHotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    // Начнется запись маршрута на карте./останавливаться
    [ObservableProperty]
    private string _mapPosRecordHotkey = "";

    [ObservableProperty]
    private string _mapPosRecordHotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    // Начинается аудиотур по мероприятиям/останавливаться
    [ObservableProperty]
    private string _autoMusicGameHotkey = "";

    [ObservableProperty]
    private string _autoMusicGameHotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    // Автоматический поиск пути
    [ObservableProperty]
    private string _autoTrackPathHotkey = "";

    [ObservableProperty]
    private string _autoTrackPathHotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    // тест
    [ObservableProperty]
    private string _test1Hotkey = "";

    [ObservableProperty]
    private string _test1HotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    // тест2
    [ObservableProperty]
    private string _test2Hotkey = "";

    [ObservableProperty]
    private string _test2HotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    // космосиэтот
    [ObservableProperty]
    private string _logBoxDisplayHotkey = "";

    [ObservableProperty]
    private string _logBoxDisplayHotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();

    // Запись клавиатуры и мыши/останавливаться
    [ObservableProperty]
    private string _keyMouseMacroRecordHotkey = "";

    [ObservableProperty]
    private string _keyMouseMacroRecordHotkeyType = HotKeyTypeEnum.KeyboardMonitor.ToString();
}
