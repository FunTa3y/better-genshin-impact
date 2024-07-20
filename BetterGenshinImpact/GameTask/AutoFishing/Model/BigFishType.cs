using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace BetterGenshinImpact.GameTask.AutoFishing.Model;

/// <summary>
/// подражатьJavaРеализован класс перечисления с несколькими атрибутами.
/// Перечень рыб Genshin Impact, классифицированных по морфологическим категориям
/// </summary>
public class BigFishType
{
    public static readonly BigFishType Medaka = new("medaka", "fruit paste bait", "Киллифиш");
    public static readonly BigFishType LargeMedaka = new("large medaka", "fruit paste bait", "большойКиллифиш");
    public static readonly BigFishType Stickleback = new("stickleback", "redrot bait", "колюшка");
    public static readonly BigFishType Koi = new("koi", "fake fly bait", "поддельный дракон");
    public static readonly BigFishType Butterflyfish = new("butterflyfish", "false worm bait", "рыба-бабочка");
    public static readonly BigFishType Pufferfish = new("pufferfish", "fake fly bait", "рыба фугу");
    public static readonly BigFishType FormaloRay = new("formalo ray", "fake fly bait", "Формоза Рэй");
    public static readonly BigFishType DivdaRay = new("divda ray", "fake fly bait", "Дифуда Рэй");
    public static readonly BigFishType Angler = new("angler", "sugardew bait", "Накидка-пуховик");
    public static readonly BigFishType AxeMarlin = new("axe marlin", "sugardew bait", "Марлин");
    public static readonly BigFishType HeartfeatherBass = new("heartfeather bass", "sour bait", "окунь из перьев сердца");
    public static readonly BigFishType MaintenanceMek = new("maintenance mek", "flashing maintenance mek bait", "агентство по техническому обслуживанию");


    public static IEnumerable<BigFishType> Values
    {
        get
        {
            yield return Medaka;
            yield return LargeMedaka;
            yield return Stickleback;
            yield return Koi;
            yield return Butterflyfish;
            yield return Pufferfish;
            yield return FormaloRay;
            yield return DivdaRay;
            yield return Angler;
            yield return AxeMarlin;
            yield return HeartfeatherBass;
            yield return MaintenanceMek;
        }
    }

    public string Name { get; private set; }
    public string BaitName { get; private set; }
    public string ChineseName { get; private set; }

    private BigFishType(string name, string baitName, string chineseName)
    {
        Name = name;
        BaitName = baitName;
        ChineseName = chineseName;
    }

    public static BigFishType FromName(string name)
    {
        foreach (var fishType in Values)
        {
            if (fishType.Name == name)
            {
                return fishType;
            }
        }

        throw new KeyNotFoundException($"BigFishType {name} not found");
    }

    public static int GetIndex(BigFishType e)
    {
        for (int i = 0; i < Values.Count(); i++)
        {
            if (Values.ElementAt(i).Name == e.Name)
            {
                return i;
            }
        }
        throw new KeyNotFoundException($"BigFishType {e.Name} not found index");
    }
}