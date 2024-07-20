using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace BetterGenshinImpact.Core.Config;

/// <summary>
///     Универсальный
/// </summary>
[Serializable]
public partial class GenshinStartConfig : ObservableObject
{
    /// <summary>
    ///     Автоматически выбирать ежемесячную карту
    /// </summary>
    [ObservableProperty]
    private bool _autoClickBlessingOfTheWelkinMoonEnabled;

    /// <summary>
    ///     Автоматический вход в игру（Открой дверь）
    /// </summary>
    [ObservableProperty]
    private bool _autoEnterGameEnabled = true;

    /// <summary>
    ///     Параметры запуска Genshin Impact
    /// </summary>
    [ObservableProperty]
    private string _genshinStartArgs = "";

    /// <summary>
    ///     Путь установки Genshin Impact
    /// </summary>
    [ObservableProperty]
    private string _installPath = "";

    /// <summary>
    ///     Связь для активации изначального тела Бога
    /// </summary>
    [ObservableProperty]
    private bool _linkedStartEnabled = true;
}
