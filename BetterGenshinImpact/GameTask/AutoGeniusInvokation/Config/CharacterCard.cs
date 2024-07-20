using System;
using System.Collections.Generic;
using BetterGenshinImpact.GameTask.AutoGeniusInvokation.Model;
using BetterGenshinImpact.GameTask.Common;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.GameTask.AutoGeniusInvokation.Config;

[Serializable]
public class CostItem
{
    /// <summary>
    /// толькоid
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Введите имя
    /// </summary>
    public string NameEn { get; set; } = string.Empty;

    /// <summary>
    /// unaligned_element	бесцветные элементы
    /// energy	перезарядка
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Сколько потреблять
    /// </summary>
    public int Count { get; set; }
}

[Serializable]
public class SkillsItem
{
    /// <summary>
    /// 
    /// </summary>
    public string NameEn { get; set; } = string.Empty;

    /// <summary>
    /// Техника съемки струящегося неба
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    public List<string> SkillTag { get; set; } = new();

    /// <summary>
    /// 
    /// </summary>
    public List<CostItem> Cost { get; set; } = new();
}

[Serializable]
public class CharacterCard
{
    /// <summary>
    /// толькоid
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string NameEn { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Ганьюй
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    public int Hp { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public int Energy { get; set; }

    /// <summary>
    /// ледяной элемент
    /// </summary>
    public string Element { get; set; } = string.Empty;

    /// <summary>
    /// поклон
    /// </summary>
    public string Weapon { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    public List<SkillsItem> Skills { get; set; } = new();

    public static void CopyCardProperty(Character source, CharacterCard characterCard)
    {
        try
        {
            source.Element = characterCard.Element.Replace("элемент", "").ChineseToElementalType();
            source.Hp = characterCard.Hp;
            source.Skills = new Skill[characterCard.Skills.Count + 1];

            short skillIndex = 0;
            for (var i = characterCard.Skills.Count - 1; i >= 0; i--)
            {
                var skillsItem = characterCard.Skills[i];
                if (skillsItem.SkillTag.Contains("Пассивный навык"))
                {
                    continue;
                }

                skillIndex++;

                source.Skills[skillIndex] = GetSkill(skillsItem);
                source.Skills[skillIndex].Index = skillIndex;
            }
        }
        catch (System.Exception e)
        {
            TaskControl.Logger.LogError($"Роль【{characterCard.Name}】Не удалось выполнить анализ конфигурации карты.：{e.Message}");
            throw new System.Exception($"Роль【{characterCard.Name}】Не удалось выполнить анализ конфигурации карты.：{e.Message}。Пожалуйста, сделай это самРольидентифицировать", e);
        }
    }

    public static Skill GetSkill(SkillsItem skillsItem)
    {
        Skill skill = new()
        {
            Name = skillsItem.Name
        };
        var specificElementNum = 0;
        foreach (var cost in skillsItem.Cost)
        {
            if (cost.NameEn == "unaligned_element")
            {
                skill.AnyElementCost = cost.Count;
            }
            else if (cost.NameEn == "energy")
            {
                continue;
            }
            else
            {
                skill.SpecificElementCost = cost.Count;
                skill.Type = cost.NameEn.ToElementalType();
                specificElementNum++;
            }
        }

        if (specificElementNum != 1)
        {
            throw new System.Exception($"Навык[{skillsItem.Name}]по умолчаниюНавыкданныеНавыкНе удалось выполнить синтаксический анализ");
        }

        skill.AllCost = skill.SpecificElementCost + skill.AnyElementCost;
        return skill;
    }
}