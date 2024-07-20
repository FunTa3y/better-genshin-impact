using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterGenshinImpact.GameTask.AutoFight;

/// <summary>
/// Автоматическая боевая конфигурация
/// </summary>
[Serializable]
public partial class AutoFightConfig : ObservableObject
{
    [ObservableProperty] private string _strategyName = "";

    /// <summary>
    /// Разделение английской запятой Принудительно назначенные командные роли
    /// </summary>
    [ObservableProperty] private string _teamNames = "";
}