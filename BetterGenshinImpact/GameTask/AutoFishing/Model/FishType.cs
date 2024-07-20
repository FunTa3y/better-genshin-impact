using System;
using System.Collections.Generic;

namespace BetterGenshinImpact.GameTask.AutoFishing.Model;

/// <summary>
/// подражатьJavaРеализован класс перечисления с несколькими атрибутами.
/// </summary>
[Obsolete]
public class FishType
{
    public static readonly FishType AizenMedaka = new("aizen medaka", "fruit paste bait", "Айдзоме Киллифиш");
    public static readonly FishType Crystalfish = new("crystalfish", "fruit paste bait", "хрустальный банкет");
    public static readonly FishType Dawncatcher = new("dawncatcher", "fruit paste bait", "Цинь Сяке");
    public static readonly FishType GlazeMedaka = new("glaze medaka", "fruit paste bait", "Глазурованный Киллифиш");
    public static readonly FishType Medaka = new("medaka", "fruit paste bait", "Киллифиш");
    public static readonly FishType SweetFlowerMedaka = new("sweet-flower medaka", "fruit paste bait", "сладкийКиллифиш");
    public static readonly FishType AkaiMaou = new("akai maou", "redrot bait", "Красный дьявол");
    public static readonly FishType Betta = new("betta", "redrot bait", "Борьба с колюшкой");
    public static readonly FishType LungedStickleback = new("lunged stickleback", "redrot bait", "рыба-выпад");
    public static readonly FishType Snowstrider = new("snowstrider", "redrot bait", "Снежный король");
    public static readonly FishType VenomspineFish = new("venomspine fish", "redrot bait", "осетровая рыба");
    public static readonly FishType AbidingAngelfish = new("abiding angelfish", "false worm bait", "Бессмертный");
    public static readonly FishType BrownShirakodai = new("brown shirakodai", "false worm bait", "Рыба-бабочка Райди коричневая");
    public static readonly FishType PurpleShirakodai = new("purple shirakodai", "false worm bait", "Рыба-бабочка Rhyogala purpurea");
    public static readonly FishType RaimeiAngelfish = new("raimei angelfish", "false worm bait", "Лэй Минсянь");
    public static readonly FishType TeaColoredShirakodai = new("tea-colored shirakodai", "false worm bait", "Риотип-рыба-бабочка");
    public static readonly FishType BitterPufferfish = new("bitter pufferfish", "fake fly bait", "Горький иглобрюх");
    public static readonly FishType DivdaRay = new("divda ray", "fake fly bait", "Дифуда Рэй");
    public static readonly FishType FormaloRay = new("formalo ray", "fake fly bait", "Формоза Рэй");
    public static readonly FishType GoldenKoi = new("golden koi", "fake fly bait", "Золотой красный ложный дракон");
    public static readonly FishType Pufferfish = new("pufferfish", "fake fly bait", "рыба фугу");
    public static readonly FishType RustyKoi = new("rusty koi", "fake fly bait", "Цян ложный дракон");
    public static readonly FishType HalcyonJadeAxeMarlin = new("halcyon jade axe marlin", "sugardew bait", "Джейд Марлин");
    public static readonly FishType LazuriteAxeMarlin = new("lazurite axe marlin", "sugardew bait", "Ляпис Марлин");
    public static readonly FishType PeachOfTheDeepWaves = new("peach of the deep waves", "sugardew bait", "Шен Бо Нитао");
    public static readonly FishType SandstormAngler = new("sandstorm angler", "sugardew bait", "Песчаная рыба фугу");
    public static readonly FishType StreamingAxeMarlin = new("streaming axe marlin", "sugardew bait", "Хайтао Марлин");
    public static readonly FishType SunsetCloudAngler = new("sunset cloud angler", "sugardew bait", "Мыс иглобрюх");
    public static readonly FishType TrueFruitAngler = new("true fruit angler", "sugardew bait", "Настоящая фруктовая рыба фугу");
    public static readonly FishType BlazingHeartfeatherBass = new("blazing heartfeather bass", "sour bait", "Запеченный окунь в форме сердца");
    public static readonly FishType RipplingHeartfeatherBass = new("rippling heartfeather bass", "sour bait", "Бобо Харт Бас");
    public static readonly FishType MaintenanceMekInitialConfiguration = new("maintenance mek- initial configuration", "flashing maintenance mek bait", "агентство по техническому обслуживанию·начальный тип способности");
    public static readonly FishType MaintenanceMekPlatinumCollection = new("maintenance mek- platinum collection", "flashing maintenance mek bait", "агентство по техническому обслуживанию·Платиновый Классик");
    public static readonly FishType MaintenanceMekSituationController = new("maintenance mek- situation controller", "flashing maintenance mek bait", "агентство по техническому обслуживанию·Контроллер ситуации");
    public static readonly FishType MaintenanceMekWaterBodyCleaner = new("maintenance mek- water body cleaner", "flashing maintenance mek bait", "агентство по техническому обслуживанию·очиститель воды");
    public static readonly FishType MaintenanceMekWaterGoldLeader = new ("maintenance mek- gold leader", "flashing maintenance mek bait", "агентство по техническому обслуживанию·Тип лидера Чэнцзинь");


    public static IEnumerable<FishType> Values
    {
        get
        {
            yield return AizenMedaka;
            yield return Crystalfish;
            yield return Dawncatcher;
            yield return GlazeMedaka;
            yield return Medaka;
            yield return SweetFlowerMedaka;
            yield return AkaiMaou;
            yield return Betta;
            yield return LungedStickleback;
            yield return Snowstrider;
            yield return VenomspineFish;
            yield return AbidingAngelfish;
            yield return BrownShirakodai;
            yield return PurpleShirakodai;
            yield return RaimeiAngelfish;
            yield return TeaColoredShirakodai;
            yield return BitterPufferfish;
            yield return DivdaRay;
            yield return FormaloRay;
            yield return GoldenKoi;
            yield return Pufferfish;
            yield return RustyKoi;
            yield return HalcyonJadeAxeMarlin;
            yield return LazuriteAxeMarlin;
            yield return PeachOfTheDeepWaves;
            yield return SandstormAngler;
            yield return StreamingAxeMarlin;
            yield return SunsetCloudAngler;
            yield return TrueFruitAngler;
            yield return BlazingHeartfeatherBass;
            yield return RipplingHeartfeatherBass;
            yield return MaintenanceMekInitialConfiguration;
            yield return MaintenanceMekPlatinumCollection;
            yield return MaintenanceMekSituationController;
            yield return MaintenanceMekWaterBodyCleaner;
            yield return MaintenanceMekWaterGoldLeader;
        }
    }

    public string Name { get; private set; }
    public string BaitName { get; private set; }
    public string ChineseName { get; private set; }

    private FishType(string name, string baitName, string chineseName)
    {
        Name = name;
        BaitName = baitName;
        ChineseName = chineseName;
    }

    public static FishType FromName(string name)
    {
        foreach (var fishType in Values)
        {
            if (fishType.Name == name)
            {
                return fishType;
            }
        }

        throw new KeyNotFoundException($"FishType {name} not found");
    }
}