using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace BetterGenshinImpact.Core.Config;

[Serializable]
public partial class RecordConfig : ObservableObject
{
    /// <summary>
    /// Определить объекты1Заблокировано ли управление，нуждатьсяMouseMoveByрасстояние
    /// ОбъектЗаблокировано ли управлениеОбъект
    /// </summary>
    [ObservableProperty]
    private double _angle2MouseMoveByX = 1.0;

    /// <summary>
    /// Определить объекты1Заблокировано ли управление，нуждатьсяDirectInputдвижущаяся единица
    /// </summary>
    [ObservableProperty]
    private double _angle2DirectInputX = 1.0;
}
