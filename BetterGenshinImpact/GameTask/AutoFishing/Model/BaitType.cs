using System.Collections.Generic;

namespace BetterGenshinImpact.GameTask.AutoFishing.Model;

public class BaitType
{

    public static readonly BaitType FruitPasteBait = new("fruit paste bait", "Наживка с фруктовой начинкой");
    public static readonly BaitType RedrotBait = new("redrot bait", "приманка из красного риса");
    public static readonly BaitType FalseWormBait = new("false worm bait", "червячная приманка");
    public static readonly BaitType FakeFlyBait = new("fake fly bait", "наживка");
    public static readonly BaitType SugardewBait = new("sugardew bait", "манная приманка");
    public static readonly BaitType SourBait = new("sour bait", "Наживка из кислого апельсина");
    public static readonly BaitType FlashingMaintenanceMekBait = new("flashing maintenance mek bait", "Механизм обслуживания стробоскопической приманки");

    public static IEnumerable<BaitType> Values
    {
        get
        {
            yield return FruitPasteBait;
            yield return RedrotBait;
            yield return FalseWormBait;
            yield return FakeFlyBait;
            yield return SugardewBait;
            yield return SourBait;
            yield return FlashingMaintenanceMekBait;
        }
    }
    public string Name { get; private set; }
    public string ChineseName { get; private set; }

    private BaitType(string name, string chineseName)
    {
        Name = name;
        ChineseName = chineseName;
    }

    public static BaitType FromName(string name)
    {
        foreach (var type in Values)
        {
            if (type.Name == name)
            {
                return type;
            }
        }

        throw new KeyNotFoundException($"BaitType {name} not found");
    }
}