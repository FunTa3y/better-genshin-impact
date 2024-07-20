﻿using BetterGenshinImpact.Core.Recognition.OpenCv.FeatureMatch;
using BetterGenshinImpact.GameTask.Common.Element.Assets;
using BetterGenshinImpact.Helpers;
using OpenCvSharp;
using OpenCvSharp.Detail;
using System.Diagnostics;
using System.Windows.Forms;
using BetterGenshinImpact.Core.Recognition.OpenCv;

namespace BetterGenshinImpact.Test.Simple.AllMap;

public class BigMapMatchTest
{
    public static void Test()
    {
        SpeedTimer speedTimer = new();
        // var mainMap100BlockMat = new Mat(@"D:\HuiPrograming\Projects\CSharp\MiHoYo\BetterGenshinImpact\BetterGenshinImpact\Assets\Map\mainMap100Block.png", ImreadModes.Grayscale);

        var map2048 = MapAssets.Instance.MainMap2048BlockMat.Value;
        var mainMap100BlockMat = ResizeHelper.Resize(map2048, 1d / (4 * 2));
        Cv2.ImWrite(@"E:\HuiTask\Улучшенный Genshin Impact\сопоставление карт\Полезный материал\mainMap128Block.png", mainMap100BlockMat);

        var surfMatcher = new FeatureMatcher(mainMap100BlockMat);
        var queryMat = new Mat(@"E:\HuiTask\Улучшенный Genshin Impact\сопоставление карт\Сравнивать\Clip_20240321_000329.png", ImreadModes.Grayscale);

        speedTimer.Record("Инициализация функций");

        queryMat = ResizeHelper.Resize(queryMat, 1d / 4);
        Cv2.ImShow("queryMat", queryMat);

        var pArray = surfMatcher.Match(queryMat);
        speedTimer.Record("соответствовать1");
        if (pArray != null)
        {
            var rect = Cv2.BoundingRect(pArray);
            Debug.WriteLine($"Matched rect 1: {rect}");
            Cv2.Rectangle(mainMap100BlockMat, rect, Scalar.Red, 2);
            Cv2.ImShow(@"b1", mainMap100BlockMat);
        }
        else
        {
            Debug.WriteLine("No match 1");
        }
        speedTimer.DebugPrint();
    }
}
