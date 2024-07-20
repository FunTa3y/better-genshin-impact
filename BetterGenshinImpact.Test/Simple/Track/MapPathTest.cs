using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Recognition.OpenCv;
using BetterGenshinImpact.GameTask.AutoTrackPath.Model;
using BetterGenshinImpact.GameTask.Common.Element.Assets;
using BetterGenshinImpact.Service;
using OpenCvSharp;
using System.IO;
using System.Text.Json;

namespace BetterGenshinImpact.Test.Simple.Track;

internal class MapPathTest
{
    public static void Test()
    {
        var wayJson = File.ReadAllText(Global.Absolute(@"log\way\yl3.json"));
        var way = JsonSerializer.Deserialize<GiPath>(wayJson, ConfigService.JsonOptions) ?? throw new Exception("way json deserialize failed");

        var points = way.WayPointList.Select(giPathPoint => giPathPoint.MatchRect.GetCenterPoint()).ToList();

        var pointsRect = Cv2.BoundingRect(points);
        var allMap = new Mat(@"E:\HuiTask\Улучшенный Genshin Impact\сопоставление карт\Полезный материал\mainMap2048Block.png");

        // Провод в порядке，Обведите каждую точку
        for (var i = 0; i < points.Count - 1; i++)
        {
            Cv2.Line(allMap, points[i], points[i + 1], Scalar.Red, 1);
            Cv2.Circle(allMap, points[i], 3, Scalar.Red, -1);
        }

        var map = allMap[new Rect(pointsRect.X - 100, pointsRect.Y - 100, pointsRect.Width + 200, pointsRect.Height + 200)];
        Cv2.ImShow("map", map);
    }
}
