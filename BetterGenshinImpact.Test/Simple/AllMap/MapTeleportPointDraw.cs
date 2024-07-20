using BetterGenshinImpact.Service;
using OpenCvSharp;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace BetterGenshinImpact.Test.Simple.AllMap;

public class MapTeleportPointDraw
{
    public static void Draw()
    {
        var pList = LoadTeleportPoint(@"E:\HuiTask\Улучшенный Genshin Impact\сопоставление карт\Точка карты\Json_Integration\точка привязки&идол\");
        var map = new Mat(@"E:\HuiTask\Улучшенный Genshin Impact\сопоставление карт\genshin_map_Отмечено.png");
        DrawTeleportPoint(map, pList.ToArray());
        Cv2.ImWrite(@"E:\HuiTask\Улучшенный Genshin Impact\сопоставление карт\genshin_map_Отмечено_Точка телепортации.png", map);
    }

    public static void DrawTeleportPoint(Mat map, Point[] points)
    {
        foreach (var point in points)
        {
            Cv2.Circle(map, new Point(point.X, point.Y), 10, Scalar.Red, 2);
        }
    }

    public static List<Point> LoadTeleportPoint(string folderPath)
    {
        string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
        List<Point> points = new();
        foreach (var file in files)
        {
            var gamePoint = JsonSerializer.Deserialize<GamePoint>(File.ReadAllText(file), ConfigService.JsonOptions);
            if (gamePoint == null)
            {
                Debug.WriteLine($"{file} json is null");
                continue;
            }

            points.Add(Transform(gamePoint.Position));
        }

        return points;
    }

    /// <summary>
    /// Преобразование системы координат
    /// </summary>
    /// <param name="position">[a,b,c]</param>
    /// <returns></returns>
    public static Point Transform(decimal[] position)
    {
        // Округлить до четного
        var a = (int)Math.Round(position[0]); // начальство
        var c = (int)Math.Round(position[2]); // Левый

        // Конвертировать1024координаты блока，Положительная ось системы координат большой карты направленаЛевыйначальствонаправленный
        // Пишите сюда больше всегоЛевыйначальствоУгловойкоординаты блока(m,n)/(начальство,Левый),Крайний срок4.5Версия，большинствоЛевыйначальствоУгловойкоординаты блокада(5,7)

        int m = 5, n = 7;
        return new Point((n + 1) * 1024 - c, (m + 1) * 1024 - a);
    }

    class GamePoint
    {
        public string Description { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal[] Position { get; set; } = new decimal[3];
    }
}
