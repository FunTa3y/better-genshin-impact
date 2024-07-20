using BetterGenshinImpact.GameTask.AutoFight.Assets;
using BetterGenshinImpact.GameTask.AutoFight.Model;
using BetterGenshinImpact.GameTask.AutoFight.Script;
using BetterGenshinImpact.GameTask.AutoGeniusInvokation.Exception;
using BetterGenshinImpact.GameTask.Model.Enum;
using BetterGenshinImpact.View.Drawable;
using BetterGenshinImpact.ViewModel.Pages;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using static BetterGenshinImpact.GameTask.Common.TaskControl;

namespace BetterGenshinImpact.GameTask.AutoFight;

public class AutoFightTask
{
    private readonly AutoFightParam _taskParam;

    private readonly CombatScriptBag _combatScriptBag;

    public AutoFightTask(AutoFightParam taskParam)
    {
        _taskParam = taskParam;
        _combatScriptBag = CombatScriptParser.ReadAndParse(_taskParam.CombatStrategyPath);
    }

    public async void Start()
    {
        var hasLock = false;
        try
        {
            AutoFightAssets.DestroyInstance();
            hasLock = await TaskSemaphore.WaitAsync(0);
            if (!hasLock)
            {
                Logger.LogError("Не удалось запустить функцию автоматического боя.：В настоящее время выполняются независимые задачи，Пожалуйста, не повторяйте задания！");
                return;
            }

            Init();
            var combatScenes = new CombatScenes().InitializeTeam(GetRectAreaFromDispatcher());
            if (!combatScenes.CheckTeamInitialized())
            {
                throw new Exception("Не удалось определить роль в команде.");
            }
            var combatCommands = _combatScriptBag.FindCombatScript(combatScenes.Avatars);

            combatScenes.BeforeTask(_taskParam.Cts);

            // боевые действия
            await Task.Run(() =>
            {
                try
                {
                    while (!_taskParam.Cts.Token.IsCancellationRequested)
                    {
                        // Универсальная боевая стратегия
                        foreach (var command in combatCommands)
                        {
                            command.Execute(combatScenes);
                        }
                    }
                }
                catch (NormalEndException)
                {
                    Logger.LogInformation("боевые действияЗаканчивать");
                }
                catch (Exception e)
                {
                    Logger.LogWarning(e.Message);
                    throw;
                }
            });
        }
        catch (NormalEndException)
        {
            Logger.LogInformation("Вручную прервать автоматический бой");
        }
        catch (Exception e)
        {
            Logger.LogError(e.Message);
            Logger.LogDebug(e.StackTrace);
        }
        finally
        {
            VisionContext.Instance().DrawContent.ClearAll();
            TaskTriggerDispatcher.Instance().SetCacheCaptureMode(DispatcherCaptureModeEnum.NormalTrigger);
            TaskSettingsPageViewModel.SetSwitchAutoFightButtonText(false);
            Logger.LogInformation("→ {Text}", "Автоматическое завершение боя");

            if (hasLock)
            {
                TaskSemaphore.Release();
            }
        }
    }

    private void Init()
    {
        LogScreenResolution();
        Logger.LogInformation("→ {Text}", "Авто бой，запускать！");
        SystemControl.ActivateWindow();
        TaskTriggerDispatcher.Instance().SetCacheCaptureMode(DispatcherCaptureModeEnum.CacheCaptureWithTrigger);
        Sleep(TaskContext.Instance().Config.TriggerInterval * 5, _taskParam.Cts); // Ожидание кэшированного изображения
    }

    private void LogScreenResolution()
    {
        var gameScreenSize = SystemControl.GetGameScreenRect(TaskContext.Instance().GameHandle);
        if (gameScreenSize.Width * 9 != gameScreenSize.Height * 16)
        {
            Logger.LogWarning("Разрешение окна игры не 16:9 ！Текущее разрешение {Width}x{Height} , Нет 16:9 Игры с разными разрешениями могут работать некорректноАвто бойФункция !", gameScreenSize.Width, gameScreenSize.Height);
        }
    }
}
