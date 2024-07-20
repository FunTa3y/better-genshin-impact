using CommunityToolkit.Mvvm.ComponentModel;
using OpenCvSharp;
using System;
using Point = System.Windows.Point;

namespace BetterGenshinImpact.Core.Config;

/// <summary>
///     Определить объекты
/// </summary>
[Serializable]
public partial class MaskWindowConfig : ObservableObject
{
    /// <summary>
    ///     Заблокировано ли управление（Перетаскивать и перемещать и т. д.）
    /// </summary>
    [ObservableProperty]
    private bool _controlLocked = true;

    /// <summary>
    ///     Свернуть в трей при выходе
    /// </summary>
    [ObservableProperty]
    private bool _directionsEnabled;

    /// <summary>
    ///     Отображать ли результаты распознавания в окне маски
    /// </summary>
    [ObservableProperty]
    private bool _displayRecognitionResultsOnMask = true;

    /// <summary>
    ///     Положение и размер окна журнала
    /// </summary>
    [ObservableProperty]
    private Rect _logBoxLocation;

    /// <summary>
    ///     Включить ли окно маски
    /// </summary>
    [ObservableProperty]
    private bool _maskEnabled = true;

    ///// <summary>
    ///// Показать границу окна маски
    ///// </summary>
    //[ObservableProperty] private bool _showMaskBorder = false;

    /// <summary>
    ///     показать окно журнала
    /// </summary>
    [ObservableProperty]
    private bool _showLogBox = true;

    /// <summary>
    ///     Индикация состояния дисплея
    /// </summary>
    [ObservableProperty]
    private bool _showStatus = true;

    /// <summary>
    ///     UIDМаскирование включено?
    /// </summary>
    [ObservableProperty]
    private bool _uidCoverEnabled;

    /// <summary>
    ///     1080pВнизUIDРасположение и размер крышки
    /// </summary>
    public Rect UidCoverRect { get; set; } = new(1690, 1052, 173, 22);

    /// <summary>
    ///     1080pВнизUIDРасположение и размер крышки
    /// </summary>
    public Rect UidCoverRightBottomRect { get; set; } = new(1920 - 1690, 1080 - 1052, 173, 22);

    public Point EastPoint { get; set; } = new(274, 109);
    public Point SouthPoint { get; set; } = new(150, 233);
    public Point WestPoint { get; set; } = new(32, 109);
    public Point NorthPoint { get; set; } = new(150, -9);

    /// <summary>
    /// показыватьFPS
    /// </summary>
    [ObservableProperty]
    private bool _showFps = false;
}
