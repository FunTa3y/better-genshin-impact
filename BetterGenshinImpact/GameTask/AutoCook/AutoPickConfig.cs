using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace BetterGenshinImpact.GameTask.AutoCook;

/// <summary>
///Автоматическая настройка приготовления
/// </summary>
[Serializable]
public partial class AutoCookConfig : ObservableObject
{
    /// <summary>
    /// Триггер включен?
    /// </summary>
    [ObservableProperty]
    private bool _enabled = false;
}
