using System;
using System.Collections.Generic;

namespace BetterGenshinImpact.GameTask.AutoFight.Config;

[Serializable]
public class CombatAvatar
{

    /// <summary>
    /// Уникально идентифицирует
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Имя персонажа на китайском языке
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Английское имя персонажа
    /// </summary>
    public string NameEn { get; set; } = string.Empty;

    /// <summary>
    /// Тип оружия
    /// </summary>
    public string Weapon { get; set; } = string.Empty;

    /// <summary>
    /// Элементарные боевые навыкиCD
    /// </summary>
    public double SkillCd { get; set; }

    /// <summary>
    /// НажиматьЭлементарные боевые навыкиCD
    /// </summary>
    public double SkillHoldCd { get; set; }

    /// <summary>
    /// Элементальный взрывCD
    /// </summary>
    public double BurstCd { get; set; }

    /// <summary>
    /// Псевдоним
    /// </summary>
    public List<string> Alias { get; set; } = new();

}