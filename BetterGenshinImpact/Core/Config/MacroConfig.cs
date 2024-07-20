using BetterGenshinImpact.GameTask.AutoFight;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace BetterGenshinImpact.Core.Config;

[Serializable]
public partial class MacroConfig : ObservableObject
{
    /// <summary>
    ///     Определить объекты
    ///     https://github.com/babalae/better-genshin-impact/issues/9
    /// </summary>
    [ObservableProperty]
    private int _enhanceWaitDelay;

    /// <summary>
    ///     Fинтервал времени взрыва
    /// </summary>
    [ObservableProperty]
    private int _fFireInterval = 100;

    /// <summary>
    ///     НажиматьFИзменятьFлопаться
    /// </summary>
    [ObservableProperty]
    private bool _fPressHoldToContinuationEnabled;

    /// <summary>
    ///     интервал времени круга
    /// </summary>
    [ObservableProperty]
    private int _runaroundInterval = 10;

    /// <summary>
    ///     Двигайте мышкой по кругу вправо
    /// </summary>
    [ObservableProperty]
    private int _runaroundMouseXInterval = 500;

    /// <summary>
    ///     космосинтервал времени взрыва
    /// </summary>
    [ObservableProperty]
    private int _spaceFireInterval = 100;

    /// <summary>
    ///     НажиматькосмосИзменятькосмослопаться
    /// </summary>
    [ObservableProperty]
    private bool _spacePressHoldToContinuationEnabled;

    /// <summary>
    ///     Состояние включения боевого макроса в один клик
    /// </summary>
    [ObservableProperty]
    private bool _combatMacroEnabled;

    /// <summary>
    ///     Режим сочетания клавиш боевого макроса одним щелчком мыши
    /// </summary>
    [ObservableProperty]
    private string _combatMacroHotkeyMode = OneKeyFightTask.HoldOnMode;

    /// <summary>
    ///     Приоритет боевого макроса в один клик
    /// </summary>
    [ObservableProperty]
    private int _combatMacroPriority = 1;
}
