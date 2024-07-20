using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BetterGenshinImpact.GameTask.AutoFight.Model;
using BetterGenshinImpact.Model;
using System.Threading.Tasks;
using System.Threading;
using BetterGenshinImpact.Core.Config;
using static BetterGenshinImpact.GameTask.Common.TaskControl;
using BetterGenshinImpact.GameTask.AutoFight.Script;
using BetterGenshinImpact.GameTask.Model.Enum;
using BetterGenshinImpact.Service;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.GameTask.AutoFight;

/// <summary>
/// Боевой макрос в один клик
/// </summary>
public class OneKeyFightTask : Singleton<OneKeyFightTask>
{
    public static readonly string HoldOnMode = "Повторять, пока нажата";
    public static readonly string TickMode = "курок";

    private Dictionary<string, List<CombatCommand>>? _avatarMacros;
    private CancellationTokenSource? _cts = null;
    private Task? _fightTask;

    private bool _isKeyDown = false;
    private int activeMacroPriority = -1;
    private DateTime _lastUpdateTime = DateTime.MinValue;

    private CombatScenes? _currentCombatScenes;

    public void KeyDown()
    {
        if (_isKeyDown || !IsEnabled())
        {
            return;
        }
        _isKeyDown = true;
        if (activeMacroPriority != TaskContext.Instance().Config.MacroConfig.CombatMacroPriority || IsAvatarMacrosEdited())
        {
            activeMacroPriority = TaskContext.Instance().Config.MacroConfig.CombatMacroPriority;
            _avatarMacros = LoadAvatarMacros();
            Logger.LogInformation("Загрузка конфигурации макроса в один клик завершена");
        }

        if (IsHoldOnMode())
        {
            if (_cts == null || _cts.Token.IsCancellationRequested)
            {
                _cts = new CancellationTokenSource();
                _fightTask = FightTask(_cts);
                if (!_fightTask.IsCompleted)
                {
                    _fightTask.Start();
                }
            }
        }
        else if (IsTickMode())
        {
            if (_cts == null || _cts.Token.IsCancellationRequested)
            {
                _cts = new CancellationTokenSource();
                _fightTask = FightTask(_cts);
                if (!_fightTask.IsCompleted)
                {
                    _fightTask.Start();
                }
            }
            else
            {
                _cts.Cancel();
            }
        }
    }

    public void KeyUp()
    {
        _isKeyDown = false;
        if (!IsEnabled())
        {
            return;
        }

        if (IsHoldOnMode())
        {
            _cts?.Cancel();
        }
    }

    // public void Run()
    // {
    //     if (!IsEnabled())
    //     {
    //         return;
    //     }
    //     _avatarMacros ??= LoadAvatarMacros();
    //
    //     if (IsHoldOnMode())
    //     {
    //         if (_fightTask == null || _fightTask.IsCompleted)
    //         {
    //             _fightTask = FightTask(_cts);
    //             _fightTask.Start();
    //         }
    //         Thread.Sleep(100);
    //     }
    //     else if (IsTickMode())
    //     {
    //         if (_cts.Token.IsCancellationRequested)
    //         {
    //             _cts = new CancellationTokenSource();
    //             Task.Run(() => FightTask(_cts));
    //         }
    //         else
    //         {
    //             _cts.Cancel();
    //         }
    //     }
    // }

    /// <summary>
    /// Перебирать боевые макросы
    /// </summary>
    private Task FightTask(CancellationTokenSource cts)
    {
        // Переключить режим скриншота
        var dispatcherCaptureMode = TaskTriggerDispatcher.Instance().GetCacheCaptureMode();
        if (dispatcherCaptureMode != DispatcherCaptureModeEnum.CacheCaptureWithTrigger)
        {
            TaskTriggerDispatcher.Instance().SetCacheCaptureMode(DispatcherCaptureModeEnum.CacheCaptureWithTrigger);
            Sleep(TaskContext.Instance().Config.TriggerInterval * 2, cts); // Ожидание кэшированного изображения
        }

        var imageRegion = GetRectAreaFromDispatcher();
        var combatScenes = new CombatScenes().InitializeTeam(imageRegion);
        if (!combatScenes.CheckTeamInitialized())
        {
            if (_currentCombatScenes == null)
            {
                Logger.LogError("Не удалось определить роль первой команды.");
                return Task.CompletedTask;
            }
            else
            {
                Logger.LogWarning("Распознавание роли в команде не удалось，Использовать последний результат распознавания，Нет никакого воздействия, когда команда не переключается.");
            }
        }
        else
        {
            _currentCombatScenes = combatScenes;
        }
        // Найдите роль, которую хотите сыграть
        var activeAvatar = _currentCombatScenes.Avatars.First(avatar => avatar.IsActive(imageRegion));

        if (_avatarMacros != null && _avatarMacros.TryGetValue(activeAvatar.Name, out var combatCommands))
        {
            return new Task(() =>
            {
                Logger.LogInformation("→ {Name}Выполнить макрос", activeAvatar.Name);
                while (!cts.Token.IsCancellationRequested && IsEnabled())
                {
                    if (IsHoldOnMode() && !_isKeyDown)
                    {
                        break;
                    }

                    // Универсальная боевая стратегия
                    foreach (var command in combatCommands)
                    {
                        command.Execute(activeAvatar);
                    }
                }
                Logger.LogInformation("→ {Name}Остановить макрос", activeAvatar.Name);
            });
        }
        else
        {
            Logger.LogWarning("→ {Name}Конфигурация[{Priority}]Пусто，пожалуйста, сначалаКонфигурацияМакрос в один клик", activeAvatar.Name, activeMacroPriority);
            return Task.CompletedTask;
        }
    }

    public Dictionary<string, List<CombatCommand>> LoadAvatarMacros()
    {
        var jsonPath = Global.Absolute("User/avatar_macro.json");
        var json = File.ReadAllText(jsonPath);
        _lastUpdateTime = File.GetLastWriteTime(jsonPath);
        var avatarMacros = JsonSerializer.Deserialize<List<AvatarMacro>>(json, ConfigService.JsonOptions);
        if (avatarMacros == null)
        {
            return new Dictionary<string, List<CombatCommand>>();
        }
        var result = new Dictionary<string, List<CombatCommand>>();
        foreach (var avatarMacro in avatarMacros)
        {
            var commands = avatarMacro.LoadCommands();
            if (commands != null)
            {
                result.Add(avatarMacro.Name, commands);
            }
        }
        return result;
    }

    public bool IsAvatarMacrosEdited()
    {
        // Определите, было ли оно отредактировано по времени модификации.
        var jsonPath = Global.Absolute("User/avatar_macro.json");
        var lastWriteTime = File.GetLastWriteTime(jsonPath);
        return lastWriteTime > _lastUpdateTime;
    }

    public static bool IsEnabled()
    {
        return TaskContext.Instance().Config.MacroConfig.CombatMacroEnabled;
    }

    public static bool IsHoldOnMode()
    {
        return TaskContext.Instance().Config.MacroConfig.CombatMacroHotkeyMode == HoldOnMode;
    }

    public static bool IsTickMode()
    {
        return TaskContext.Instance().Config.MacroConfig.CombatMacroHotkeyMode == TickMode;
    }
}
