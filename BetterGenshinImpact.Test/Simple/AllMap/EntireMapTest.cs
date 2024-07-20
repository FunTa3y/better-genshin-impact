using BetterGenshinImpact.Core.Recognition.OpenCv.FeatureMatch;
using BetterGenshinImpact.GameTask.Common.Element.Assets;
using BetterGenshinImpact.Helpers;
using OpenCvSharp;
using OpenCvSharp.Detail;
using System.Diagnostics;
using System.Windows.Forms;

namespace BetterGenshinImpact.Test.Simple.AllMap;

public class EntireMapTest
{
    public static void Test()
    {
        SpeedTimer speedTimer = new();
        var mainMap1024BlockMat = new Mat(@"E:\HuiTask\Улучшенный Genshin Impact\сопоставление карт\Полезный материал\mainMap1024Block.png", ImreadModes.Grayscale);
        var surfMatcher = new FeatureMatcher(mainMap1024BlockMat);
        var queryMat = new Mat(@"E:\HuiTask\Улучшенный Genshin Impact\сопоставление карт\Сравнивать\Начать автоматическую рыбалку\Clip_20240323_183119.png", ImreadModes.Grayscale);

        speedTimer.Record("Инициализация функций");

        var pArray = surfMatcher.Match(queryMat);
        speedTimer.Record("соответствовать1");
        if (pArray != null)
        {
            var rect = Cv2.BoundingRect(pArray);
            Debug.WriteLine($"Matched rect 1: {rect}");
            Cv2.Rectangle(mainMap1024BlockMat, rect, Scalar.Red, 2);
            // Cv2.ImWrite(@"E:\HuiTask\Улучшенный Genshin Impact\сопоставление карт\b1.png", mainMap1024BlockMat);

            var pArray2 = surfMatcher.Match(queryMat, rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
            speedTimer.Record("соответствовать2");
            if (pArray2 != null)
            {
                var rect2 = Cv2.BoundingRect(pArray2);
                Debug.WriteLine($"Matched rect 2: {rect2}");
                Cv2.Rectangle(mainMap1024BlockMat, rect2, Scalar.Yellow, 1);
                // Cv2.ImWrite(@"E:\HuiTask\Улучшенный Genshin Impact\сопоставление карт\b2.png", mainMap1024BlockMat);
            }
            else
            {
                Debug.WriteLine("No match 2");
            }
        }
        else
        {
            Debug.WriteLine("No match 1");
        }
        speedTimer.DebugPrint();
    }

    public static void Storage()
    {
        var featureMatcher = new FeatureMatcher(MapAssets.Instance.MainMap2048BlockMat.Value, new FeatureStorage("mainMap2048Block"));
        MessageBox.Show("Генерация характерных точек завершена");
    }
}
