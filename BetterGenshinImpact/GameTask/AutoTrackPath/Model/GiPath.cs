using System;
using System.Collections.Generic;
using System.Diagnostics;
using BetterGenshinImpact.Helpers;
using OpenCvSharp;

namespace BetterGenshinImpact.GameTask.AutoTrackPath.Model;

[Serializable]
public class GiPath
{
    public List<GiPathPoint> WayPointList { get; set; } = new();

    public void AddPoint(Rect matchRect)
    {
        // Соотношение сторон больше, чем 1.5 Прямоугольник не добавляется
        var r = matchRect.Width / (double)matchRect.Height;
        if (r is > 1.5 or < 0.66)
        {
            Debug.WriteLine($"Непрерывная работа не должна: {r}");
            return;
        }

        // Расстояние от предыдущей точки меньше 10 Прямоугольник не добавляется
        var giPathPoint = GiPathPoint.BuildFrom(matchRect, WayPointList.Count);
        if (WayPointList.Count > 0)
        {
            var lastPoint = WayPointList[^1];
            var distance = MathHelper.Distance(giPathPoint.Pt, lastPoint.Pt);
            if (distance == 0 || distance > 50)
            {
                Debug.WriteLine($"Слишком близко или слишком далеко от предыдущей точки: {distance}，сдаться");
                return;
            }
        }

        WayPointList.Add(giPathPoint);
    }
}
