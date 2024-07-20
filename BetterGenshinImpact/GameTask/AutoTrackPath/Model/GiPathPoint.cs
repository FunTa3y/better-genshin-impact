using System;
using BetterGenshinImpact.Core.Recognition.OpenCv;
using BetterGenshinImpact.GameTask.Common.Map;
using OpenCvSharp;

namespace BetterGenshinImpact.GameTask.AutoTrackPath.Model;

/// <summary>
/// точка маршрута
/// Координаты должны быть в игровой системе координат.
/// </summary>
[Serializable]
public class GiPathPoint
{
    public Point Pt { get; set; }

    public Rect MatchRect { get; set; }

    public int Index { get; set; }

    public DateTime Time { get; set; }

    public string Type { get; set; } = GiPathPointType.Normal.ToString();

    public static GiPathPoint BuildFrom(Rect matchRect, int index)
    {
        var pt = MapCoordinate.Main2048ToGame(matchRect.GetCenterPoint());
        return new GiPathPoint
        {
            Pt = pt,
            MatchRect = matchRect,
            Index = index,
            Time = DateTime.Now
        };
    }

    public static bool IsKeyPoint(GiPathPoint giPathPoint)
    {
        if (giPathPoint.Type == GiPathPointType.KeyPoint.ToString()
            || giPathPoint.Type == GiPathPointType.Fighting.ToString()
            || giPathPoint.Type == GiPathPointType.Collection.ToString())
        {
            return true;
        }
        return false;
    }
}

public enum GiPathPointType
{
    Normal, // Обычный
    KeyPoint, // ключевой момент
    Fighting, // боевая точка
    Collection, // Пункт сбора
}
