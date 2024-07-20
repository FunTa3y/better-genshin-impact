using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.GameTask.AutoTrackPath.Model;
using BetterGenshinImpact.GameTask.Model;
using BetterGenshinImpact.Service;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BetterGenshinImpact.GameTask.Common.Element.Assets;

public class MapAssets : BaseAssets<MapAssets>
{
    public Lazy<Mat> MainMap100BlockMat { get; } = new(() => new Mat(Global.Absolute(@"Assets\Map\mainMap100Block.png")));

    // public Lazy<Mat> MainMap1024BlockMat { get; } = new(() => new Mat(@"E:\HuiTask\Улучшенный Genshin Impact\сопоставление карт\Полезный материал\mainMap1024Block.png", ImreadModes.Grayscale));

    public Lazy<Mat> MainMap2048BlockMat { get; } = new(() => new Mat(@"E:\HuiTask\Улучшенный Genshin Impact\сопоставление карт\Полезный материал\mainMap2048Block.png", ImreadModes.Grayscale));

    public Lazy<Mat> MainMap128BlockMat { get; } = new(() => new Mat(@"E:\HuiTask\Улучшенный Genshin Impact\сопоставление карт\Полезный материал\mainMap128Block.png", ImreadModes.Grayscale));
    public Lazy<Mat> MainMap256BlockMat { get; } = new(() => new Mat(@"E:\HuiTask\Улучшенный Genshin Impact\сопоставление карт\Полезный материал\mainMap256Block.png", ImreadModes.Grayscale));

    // Центральное положение каждого региона после нажатия

    // 2048 под блоком，Максимальная площадь, где существуют точки телепорта，Если результат распознавания больше этого，Нужно нажать, чтобы увеличить

    // Информация о пункте пересадки

    public List<GiWorldPosition> TpPositions;

    public readonly Dictionary<string, double[]> CountryPositions = new()
    {
        { "Монд", [-876, 2278] },
        { "Лиюэ", [270, -666] },
        { "Иназума", [-4400, -3050] },
        { "Сумеру", [2877, -374] },
        { "Фонтейн", [4515, 3631] },
    };

    public MapAssets()
    {
        var json = File.ReadAllText(Global.Absolute(@"GameTask\AutoTrackPath\Assets\tp.json"));
        TpPositions = JsonSerializer.Deserialize<List<GiWorldPosition>>(json, ConfigService.JsonOptions) ?? throw new Exception("tp.json deserialize failed");
    }
}
