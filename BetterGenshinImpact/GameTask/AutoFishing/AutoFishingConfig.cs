using OpenCvSharp;
using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterGenshinImpact.GameTask.AutoFishing;

/// <summary>
/// Автоматическая настройка рыбалки
/// </summary>
[Serializable]
public partial class AutoFishingConfig : ObservableObject
{
    /// <summary>
    /// Триггер включен?
    /// После включения：
    /// 1. Автоматически определять, следует ли входить в состояние рыбалки
    /// 2. Автоматическая подъемная штанга
    /// 3. Автоматическая тяга
    /// </summary>
    [ObservableProperty] private bool _enabled = false;

    ///// <summary>
    ///// Область распознавания текста на рыболовном крючке
    ///// Временно бесполезен
    ///// </summary>
    //[ObservableProperty] private Rect _fishHookedRecognitionArea = Rect.Empty;

    /// <summary>
    /// Включено ли автоматическое бросание шеста
    /// </summary>
    [ObservableProperty] private bool _autoThrowRodEnabled = false;

    /// <summary>
    /// Автоматическое метание удилища без тайм-аута крюка(Второй)
    /// </summary>
    [ObservableProperty] private int _autoThrowRodTimeOut = 10;
}