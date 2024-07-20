using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace BetterGenshinImpact.GameTask.AutoDomain;

[Serializable]
public partial class AutoDomainConfig : ObservableObject
{

    /// <summary>
    /// После битвы подождите несколько секунд, прежде чем начинать поиски окаменевшего древнего дерева.，Второй
    /// </summary>
    [ObservableProperty] private double _fightEndDelay = 5;

    /// <summary>
    /// Когда ищешь древние деревья，Имя триггера，Это включено
    /// </summary>
    [ObservableProperty] private bool _shortMovement = false;

    /// <summary>
    /// Когда ищешь древние деревья，Имя триггера，Это включено
    /// </summary>
    [ObservableProperty] private bool _walkToF = false;

    /// <summary>
    /// Когда ищешь древние деревья，Имя триггерараз
    /// </summary>
    [ObservableProperty] private int _leftRightMoveTimes = 3;
}