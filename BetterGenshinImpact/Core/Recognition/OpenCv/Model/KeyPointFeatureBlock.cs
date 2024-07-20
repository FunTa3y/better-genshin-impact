using System.Collections.Generic;
using OpenCvSharp;

namespace BetterGenshinImpact.Core.Recognition.OpenCv.Model;

/// <summary>
/// функциональный блок
/// Разделить объекты по областям изображения
/// </summary>
public class KeyPointFeatureBlock
{
    public List<KeyPoint> KeyPointList { get; set; } = new();

    private KeyPoint[]? keyPointArray;

    public KeyPoint[] KeyPointArray
    {
        get
        {
            keyPointArray ??= [.. KeyPointList];
            return keyPointArray;
        }
    }

    /// <summary>
    /// Конфигурация быстрой передачи KeyPoint[] индекс в
    /// </summary>
    public List<int> KeyPointIndexList { get; set; } = new();

    public Mat? Descriptor;

    public int MergedCenterCellCol = -1;
    public int MergedCenterCellRow = -1;
}
