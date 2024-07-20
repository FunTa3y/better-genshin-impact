using BetterGenshinImpact.GameTask.AutoFight.Config;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static BetterGenshinImpact.GameTask.Common.TaskControl;

namespace BetterGenshinImpact.GameTask.AutoFight.Script;

public class CombatScriptParser
{
    public static CombatScriptBag ReadAndParse(string path)
    {
        if (File.Exists(path))
        {
            return new CombatScriptBag(Parse(path));
        }
        else if (Directory.Exists(path))
        {
            var files = Directory.GetFiles(path, "*.txt", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                Logger.LogError("Файл боевого сценария не существует：{Path}", path);
                throw new Exception("Файл боевого сценария не существует");
            }

            var combatScripts = new List<CombatScript>();
            foreach (var file in files)
            {
                try
                {
                    combatScripts.Add(Parse(file));
                }
                catch (Exception e)
                {
                    Logger.LogWarning("Не удалось разобрать файл боевого сценария.：{Path} , {Msg} ", file, e.Message);
                }
            }

            return new CombatScriptBag(combatScripts);
        }
        else
        {
            Logger.LogError("Файл боевого сценария не существует：{Path}", path);
            throw new Exception("Файл боевого сценария не существует");
        }
    }

    public static CombatScript Parse(string path)
    {
        var script = File.ReadAllText(path);
        var lines = script.Split(new[] { "\r\n", "\r", "\n", ";" }, StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>();
        foreach (var line in lines)
        {
            var l = line.Trim()
                .Replace("（", "(")
                .Replace(")", ")")
                .Replace("，", ",");
            if (l.StartsWith("//") || l.StartsWith("#") || string.IsNullOrEmpty(l))
            {
                continue;
            }

            result.Add(l);
        }

        var combatScript = Parse(result);
        combatScript.Path = path;
        combatScript.Name = Path.GetFileNameWithoutExtension(path);
        return combatScript;
    }

    public static CombatScript Parse(List<string> lines)
    {
        List<CombatCommand> combatCommands = new();
        HashSet<string> combatAvatarNames = new();
        foreach (var line in lines)
        {
            var oneLineCombatCommands = ParseLine(line, combatAvatarNames);
            combatCommands.AddRange(oneLineCombatCommands);
        }

        var names = string.Join(",", combatAvatarNames);
        Logger.LogDebug("Анализ боевого сценария завершен.，общий{Cnt}инструкции，включающие роли：{Str}", combatCommands.Count, names);

        return new CombatScript(combatAvatarNames, combatCommands);
    }

    public static List<CombatCommand> ParseLine(string line, HashSet<string> combatAvatarNames)
    {
        var oneLineCombatCommands = new List<CombatCommand>();
        // Разделяйте роли и директивы пробелами Содержимое перед первым пробелом перехватывается как имя персонажа.，Ниже приведены инструкции
        var firstSpaceIndex = line.IndexOf(' ');
        if (firstSpaceIndex < 0)
        {
            Logger.LogError("Ошибка формата боевого сценария，долженРазделяйте роли и директивы пробелами");
            throw new Exception("Ошибка формата боевого сценария，долженРазделяйте роли и директивы пробелами");
        }

        var character = line[..firstSpaceIndex];
        character = DefaultAutoFightConfig.AvatarAliasToStandardName(character);
        var commands = line[(firstSpaceIndex + 1)..];
        oneLineCombatCommands.AddRange(ParseLineCommands(commands, character));
        combatAvatarNames.Add(character);
        return oneLineCombatCommands;
    }

    public static List<CombatCommand> ParseLineCommands(string lineWithoutAvatar, string avatarName)
    {
        var oneLineCombatCommands = new List<CombatCommand>();
        var commandArray = lineWithoutAvatar.Split(",", StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i < commandArray.Length; i++)
        {
            var command = commandArray[i];
            if (string.IsNullOrEmpty(command))
            {
                continue;
            }

            if (command.Contains("(") && !command.Contains(")"))
            {
                var j = i + 1;
                // скобки, разделенные запятыми，Нужно объединить
                while (j < commandArray.Length)
                {
                    command += "," + commandArray[j];
                    if (command.Count("(".Contains) > 1)
                    {
                        Logger.LogError("Ошибка формата боевого сценария，инструкция {Cmd} Скобки не могут быть сопряжены", command);
                        throw new Exception("Ошибка формата боевого сценария，инструкцияСкобки не могут быть сопряжены");
                    }

                    if (command.Contains(")"))
                    {
                        i = j;
                        break;
                    }

                    j++;
                }

                if (!(command.Contains("(") && command.Contains(")")))
                {
                    Logger.LogError("Ошибка формата боевого сценария，инструкция {Cmd} Неполные скобки", command);
                    throw new Exception("Ошибка формата боевого сценария，инструкцияНеполные скобки");
                }
            }

            var combatCommand = new CombatCommand(avatarName, command);
            oneLineCombatCommands.Add(combatCommand);
        }

        return oneLineCombatCommands;
    }
}
