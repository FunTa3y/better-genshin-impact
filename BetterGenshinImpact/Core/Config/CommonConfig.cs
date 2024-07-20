using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace BetterGenshinImpact.Core.Config;

/// <summary>
///     Определить объекты
/// </summary>
[Serializable]
public partial class CommonConfig : ObservableObject
{
    /// <summary>
    ///     Включить ли окно маски
    /// </summary>
    [ObservableProperty]
    private bool _screenshotEnabled;

    /// <summary>
    ///     UIDМаскирование включено?
    /// </summary>
    [ObservableProperty]
    private bool _screenshotUidCoverEnabled;

    /// <summary>
    ///     Свернуть в трей при выходе
    /// </summary>
    [ObservableProperty]
    private bool _exitToTray;
}
