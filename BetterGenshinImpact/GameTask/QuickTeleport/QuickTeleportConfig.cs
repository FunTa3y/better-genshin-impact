using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace BetterGenshinImpact.GameTask.QuickTeleport;

/// <summary>
/// Быстрая передача конфигурации
/// </summary>
[Serializable]
public partial class QuickTeleportConfig : ObservableObject
{
    /// <summary>
    /// Быстрая доставка включена?
    /// </summary>
    [ObservableProperty] private bool _enabled = false;

    /// <summary>
    /// Время между нажатиями на телепорт списка кандидатов(ms)
    /// </summary>
    [ObservableProperty] private int _teleportListClickDelay = 200;

    /// <summary>
    /// Время ожидания всплывающего окна передачи справа(ms)
    /// 0.24 После версии，Это значение может быть установлено на 0，Потому что на изучение картинок уходит больше времени。0.24 До версии，Рекомендуется установить его на 100
    /// </summary>
    [ObservableProperty] private int _waitTeleportPanelDelay = 50;

    /// <summary>
    /// Отправить с помощью сочетаний клавиш
    /// </summary>
    [ObservableProperty] private bool _hotkeyTpEnabled = false;
}