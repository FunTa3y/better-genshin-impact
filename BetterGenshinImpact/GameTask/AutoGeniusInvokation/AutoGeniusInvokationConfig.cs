using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using BetterGenshinImpact.Core.Config;

namespace BetterGenshinImpact.GameTask.AutoGeniusInvokation;

/// <summary>
/// Автоматическая настройка игры в карты
/// </summary>
[Serializable]
public partial class AutoGeniusInvokationConfig : ObservableObject
{
    [ObservableProperty] private string _strategyName = "1.Мона сахарное пианино";

    [ObservableProperty] private int _sleepDelay = 0;

    public List<Rect> DefaultCharacterCardRects { get; set; } = new()
    {
        new(667, 632, 165, 282),
        new(877, 632, 165, 282),
        new(1088, 632, 165, 282)
    };


    /// <summary>
    /// Область распознавания текста с номерами игральных костей
    /// </summary>
    public Rect MyDiceCountRect { get; } = new(68, 642, 25, 31); // 42,47

    ///// <summary>
    ///// Область карты персонажа расширяется влево.，ВключатьHPобласть
    ///// </summary>
    //public int CharacterCardLeftExtend { get; } = 20;

    ///// <summary>
    ///// карты персонажейобластьУвеличить расстояние вправо，Включатьперезарядкаобласть
    ///// </summary>
    //public int CharacterCardRightExtend { get; } = 14;

    /// <summary>
    /// идти воеватькарты персонажейобластьразница расстояний вверх или вниз
    /// </summary>
    public int ActiveCharacterCardSpace { get; set; } = 41;

    /// <summary>
    /// HPобласть существовать карты персонажейобласть относительное положение
    /// </summary>
    public Rect CharacterCardExtendHpRect { get; } = new(-20, 0, 60, 55);
}