using System;
using System.Diagnostics;
using BetterGenshinImpact.Core.Recognition.OpenCv;
using BetterGenshinImpact.Core.Recognition.OpenCv.FeatureMatch;
using BetterGenshinImpact.GameTask.Common.Element.Assets;
using BetterGenshinImpact.Model;
using OpenCvSharp;

namespace BetterGenshinImpact.GameTask.Common.Map;

/// <summary>
/// Специально используется для распознавания больших карт.
/// изображение уменьшено8раз
/// </summary>
public class BigMap : Singleton<BigMap>
{
    // Загружайте характерные точки прямо с изображения
    private readonly FeatureMatcher _featureMatcher = new(MapAssets.Instance.MainMap256BlockMat.Value, new FeatureStorage("mainMap256Block"));

    /// <summary>
    /// Получить местоположение на карте на основе сопоставления объектов соответствовать всем
    /// </summary>
    /// <param name="greyMat">Входящие большие изображения карт уменьшаются.8раз</param>
    /// <returns></returns>
    public Rect GetBigMapPositionByFeatureMatch(Mat greyMat)
    {
        try
        {
            greyMat = ResizeHelper.Resize(greyMat, 1d / 4);

            var pArray = _featureMatcher.Match(greyMat);
            if (pArray == null || pArray.Length < 4)
            {
                throw new InvalidOperationException();
            }
            return Cv2.BoundingRect(pArray);
        }
        catch
        {
            Debug.WriteLine("Feature Match Failed");
            return Rect.Empty;
        }
    }
}
