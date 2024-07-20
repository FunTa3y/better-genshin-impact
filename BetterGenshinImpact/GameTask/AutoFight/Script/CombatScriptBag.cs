using System;
using BetterGenshinImpact.GameTask.AutoFight.Model;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using static BetterGenshinImpact.GameTask.Common.TaskControl;

namespace BetterGenshinImpact.GameTask.AutoFight.Script;

public class CombatScriptBag(List<CombatScript> combatScripts)
{
    private List<CombatScript> CombatScripts { get; set; } = combatScripts;

    public CombatScriptBag(CombatScript combatScript) : this([combatScript])
    {
    }

    public List<CombatCommand> FindCombatScript(Avatar[] avatars)
    {
        foreach (var combatScript in CombatScripts)
        {
            var matchCount = 0;
            foreach (var avatar in avatars)
            {
                if (combatScript.AvatarNames.Contains(avatar.Name))
                {
                    matchCount++;
                }

                if (matchCount == avatars.Length)
                {
                    Logger.LogInformation("Соответствие сценарию боя：{Name}", combatScript.Name);
                    return combatScript.CombatCommands;
                }
            }

            combatScript.MatchCount = matchCount;
        }

        // Подходящего сценария боя не найдено
        // Сортировать по количеству совпадений в порядке убывания.
        CombatScripts.Sort((a, b) => b.MatchCount.CompareTo(a.MatchCount));
        if (CombatScripts[0].MatchCount == 0)
        {
            throw new Exception("Боевые сценарии не найдены.");
        }

        Logger.LogWarning("Не полностью соответствует команде из четырех человек，Используйте команду с лучшим матчем：{Name}", CombatScripts[0].Name);
        return CombatScripts[0].CombatCommands;
    }
}
