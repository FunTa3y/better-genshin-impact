using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace BetterGenshinImpact.GameTask.AutoWood;

/// <summary>
/// Конфигурация автоматического журналирования
/// </summary>
[Serializable]
public partial class AutoWoodConfig : ObservableObject
{
    /// <summary>
    /// Дополнительная задержка после использования реквизита（миллисекунда）
    /// </summary>
    [ObservableProperty]
    private int _afterZSleepDelay = 0;

    /// <summary>
    /// Количество древесиныOCRВключить ли
    /// </summary>
    [ObservableProperty]
    private bool _woodCountOcrEnabled = false;

    // /// <summary>
    // /// Нажмите дваждыESCключ，Увидеть причину：
    // /// https://github.com/babalae/better-genshin-impact/issues/235
    // /// </summary>
    // [ObservableProperty]
    // private bool _pressTwoEscEnabled = false;
}
