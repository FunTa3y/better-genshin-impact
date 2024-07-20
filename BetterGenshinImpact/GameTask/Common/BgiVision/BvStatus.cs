using BetterGenshinImpact.GameTask.Common.Element.Assets;
using BetterGenshinImpact.GameTask.Model.Area;
using BetterGenshinImpact.GameTask.QuickTeleport.Assets;
using System;

namespace BetterGenshinImpact.GameTask.Common.BgiVision;

/// <summary>
/// подражатьOpenCvстатический класс
/// Используется для различных операций по идентификации и контролю Genshin Impact.
///
/// В основном это необходимо для определения некоторых состояний в игре.
/// </summary>
public static partial class Bv
{
    public static string WhichGameUi()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Это в основном интерфейсе?
    /// </summary>
    /// <param name="captureRa"></param>
    /// <returns></returns>
    public static bool IsInMainUi(ImageRegion captureRa)
    {
        return captureRa.Find(ElementAssets.Instance.PaimonMenuRo).IsExist();
    }

    /// <summary>
    /// Это в интерфейсе большой карты?
    /// </summary>
    /// <param name="captureRa"></param>
    /// <returns></returns>
    public static bool IsInBigMapUi(ImageRegion captureRa)
    {
        return captureRa.Find(QuickTeleportAssets.Instance.MapScaleButtonRo).IsExist();
    }

    /// <summary>
    /// Интерфейс большой карты находится под землей?
    /// Значок подземелья может быть неправильно распознан при наведении на него указателя мыши или во время анимации переключения.
    /// </summary>
    /// <param name="captureRa"></param>
    /// <returns></returns>
    public static bool BigMapIsUnderground(ImageRegion captureRa)
    {
        return captureRa.Find(QuickTeleportAssets.Instance.MapUndergroundSwitchButtonRo).IsExist();
    }

    public static MotionStatus GetMotionStatus(ImageRegion captureRa)
    {
        var spaceExist = captureRa.Find(ElementAssets.Instance.SpaceKey).IsExist();
        var xExist = captureRa.Find(ElementAssets.Instance.XKey).IsExist();
        if (spaceExist)
        {
            return xExist ? MotionStatus.Climb : MotionStatus.Fly;
        }
        else
        {
            return MotionStatus.Normal;
        }
    }
}

public enum MotionStatus
{
    Normal, // нормальный
    Fly, // полет
    Climb, // взбираться
}
