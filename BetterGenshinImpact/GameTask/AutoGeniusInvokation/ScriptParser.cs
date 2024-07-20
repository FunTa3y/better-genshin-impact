using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using BetterGenshinImpact.GameTask.AutoFight.Config;
using BetterGenshinImpact.GameTask.AutoGeniusInvokation.Config;
using BetterGenshinImpact.GameTask.AutoGeniusInvokation.Model;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.GameTask.AutoGeniusInvokation;

public class ScriptParser
{
    private static readonly ILogger<ScriptParser> MyLogger = App.GetLogger<ScriptParser>();

    public static Duel Parse(string script)
    {
        var lines = script.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var result = new List<string>();
        foreach (var line in lines)
        {
            string l = line.Trim();
            result.Add(l);
        }

        return Parse(result);
    }

    public static Duel Parse(List<string> lines)
    {
        Duel duel = new Duel();
        string stage = "";

        int i = 0;
        try
        {
            for (i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.Contains(":"))
                {
                    stage = line;
                    continue;
                }

                if (line == "---" || line.StartsWith("//") || string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (stage == "определение роли:")
                {
                    var character = ParseCharacter(line);
                    duel.Characters[character.Index] = character;
                }
                else if (stage == "Определение стратегии:")
                {
                    MyAssert(duel.Characters[3] != null, "роль не определена");

                    string[] actionParts = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    MyAssert(actionParts.Length == 3, "Ошибка анализа команды действия в стратегии");
                    MyAssert(actionParts[1] == "использовать", "Ошибка анализа команды действия в стратегии");

                    var actionCommand = new ActionCommand();
                    var action = actionParts[1].ChineseToActionEnum();
                    actionCommand.Action = action;

                    int j = 1;
                    for (j = 1; j <= 3; j++)
                    {
                        var character = duel.Characters[j];
                        if (character != null && character.Name == actionParts[0])
                        {
                            actionCommand.Character = character;
                            break;
                        }
                    }

                    MyAssert(j <= 3, "Ошибка анализа команды действия в стратегии：РольИмя не может быть получено изопределение ролисоответствует в");

                    int skillNum = int.Parse(Regex.Replace(actionParts[2], @"[^0-9]+", ""));
                    MyAssert(skillNum < 5, "Ошибка анализа команды действия в стратегии：Неправильный номер навыка");
                    actionCommand.TargetIndex = skillNum;
                    duel.ActionCommandQueue.Add(actionCommand);
                }
                else
                {
                    throw new System.Exception($"Определить роли в команде：{stage}");
                }
            }

            MyAssert(duel.Characters[3] != null, "роль не определена，Пожалуйста, подтвердите, соответствует ли текстовый формат политикиUTF-8");
        }
        catch (System.Exception ex)
        {
            MyLogger.LogError($"Ошибка скрипта разбора，Номер строки：{i + 1}，сообщение об ошибке：{ex}");
            MessageBox.Show($"Ошибка скрипта разбора，Номер строки：{i + 1}，сообщение об ошибке：{ex}", "Не удалось выполнить синтаксический анализ политики.", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
        }

        return duel;
    }

    /// <summary>
    /// Пример парсинга
    /// Роль1=Цинцин|гром{Навык3потреблять=1громигральная кость+2произвольный,Навык2потреблять=3громигральная кость,Навык1потреблять=4громигральная кость}
    /// Роль2=громбог|гром{Навык3потреблять=1громигральная кость+2произвольный,Навык2потреблять=3громигральная кость,Навык1потреблять=4громигральная кость}
    /// Роль3=Ганьюй|лед{Навык4потреблять=1ледигральная кость+2произвольный,Навык3потреблять=1ледигральная кость,Навык2потреблять=5ледигральная кость,Навык1потреблять=3ледигральная кость}
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    public static Character ParseCharacter(string line)
    {
        var character = new Character();

        var characterAndSkill = line.Split('{');

        var parts = characterAndSkill[0].Split('=');
        character.Index = int.Parse(Regex.Replace(parts[0], @"[^0-9]+", ""));
        MyAssert(character.Index >= 1 && character.Index <= 3, "РольСерийный номер должен быть в1-3между");

        if (parts[1].Contains("|"))
        {
            var nameAndElement = parts[1].Split('|');
            character.Name = nameAndElement[0];
            character.Element = nameAndElement[1].Substring(0, 1).ChineseToElementalType();

            // Навык
            string skillStr = characterAndSkill[1].Replace("}", "");
            var skillParts = skillStr.Split(',');
            var skills = new Skill[skillParts.Length + 1];
            for (int i = 0; i < skillParts.Length; i++)
            {
                var skill = ParseSkill(skillParts[i]);
                skills[skill.Index] = skill;
            }

            character.Skills = skills.ToArray();
        }
        else
        {
            // Никакой настройки напрямуюиспользоватьраспределение по умолчанию
            character.Name = parts[1];
            var standardName = DefaultTcgConfig.CharacterCardMap.Keys.FirstOrDefault(x => x.Equals(character.Name));
            if (string.IsNullOrEmpty(standardName))
            {
                standardName = DefaultAutoFightConfig.AvatarAliasToStandardName(character.Name);
            }

            if (DefaultTcgConfig.CharacterCardMap.TryGetValue(standardName, out var characterCard))
            {
                CharacterCard.CopyCardProperty(character, characterCard);
            }
            else
            {
                throw new System.Exception($"Роль【{standardName}】В настоящее время не существует конфигурации определения карты по умолчанию.，Пожалуйста, сделай это самопределение роли");
            }
        }

        return character;
    }

    /// <summary>
    /// Навык3потреблять=1громигральная кость+2произвольный
    /// Навык2потреблять=3громигральная кость
    /// Навык1потреблять=4громигральная кость
    /// </summary>
    /// <param name="oneSkillStr"></param>
    /// <returns></returns>
    public static Skill ParseSkill(string oneSkillStr)
    {
        var skill = new Skill();
        var parts = oneSkillStr.Split('=');
        skill.Index = short.Parse(Regex.Replace(parts[0], @"[^0-9]+", ""));
        MyAssert(skill.Index >= 1 && skill.Index <= 5, "НавыкСерийный номер должен быть в1-5между");
        var costStr = parts[1];
        var costParts = costStr.Split('+');
        skill.SpecificElementCost = int.Parse(costParts[0].Substring(0, 1));
        skill.Type = costParts[0].Substring(1, 1).ChineseToElementalType();
        // Разношерстные кости+Без прав
        if (costParts.Length > 1)
        {
            skill.AnyElementCost = int.Parse(costParts[1].Substring(0, 1));
        }

        skill.AllCost = skill.SpecificElementCost + skill.AnyElementCost;
        return skill;
    }

    private static void MyAssert(bool b, string msg)
    {
        if (!b)
        {
            throw new System.Exception(msg);
        }
    }
}